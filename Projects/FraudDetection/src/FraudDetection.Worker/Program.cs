using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.EvaluateTransaction;
using FraudDetection.Domain.Services;
using FraudDetection.Infrastructure.Configuration;
using FraudDetection.Infrastructure.Messaging;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.Infrastructure.Persistence.Repositories;
using FraudDetection.Worker.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Persistence — the Worker shares the FraudDetection database with the API
// (pragmatic single-deployment choice, documented in ADR-054). It reads
// pending transactions, applies the evaluation, and persists the new status.
builder.Services.AddDbContext<FraudDetectionDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Kafka options — bound from the "Kafka" section, validated at startup
// (fail fast on a misconfigured deployment instead of a worker that silently
// consumes nothing).
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddOptions<KafkaOptions>()
    .ValidateOnStart();
builder.Services.ConfigureOptions<KafkaOptionsValidator>();

// Messaging — same publisher adapter as the API (producer side); the consumer
// loop lives in TransactionEvaluationWorker.
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

// Domain services (stateless — safe as singleton)
builder.Services.AddSingleton<FraudRuleEngine>();

// Application services — scoped because EvaluateTransactionHandler depends on
// the scoped DbContext via ITransactionRepository; the worker resolves the
// handler inside a fresh scope per message.
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
builder.Services.AddScoped<EvaluateTransactionHandler>();

// The anti-fraud consumer (BackgroundService).
builder.Services.AddHostedService<TransactionEvaluationWorker>();

var autoMigrate = builder.Configuration.GetValue<bool>("AutoMigrate");
var host = builder.Build();

// Auto-apply pending migrations when configured (docker-compose dev/portfolio
// choice — the API applies the same behavior; see ADR-054).
if (autoMigrate)
{
    using var scope = host.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
    await context.Database.MigrateAsync();
}

await host.RunAsync();