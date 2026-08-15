using System.Reflection;
using System.Threading.RateLimiting;
using Confluent.Kafka;
using FraudDetection.Api.Endpoints;
using FraudDetection.Api.Health;
using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Configuration;
using FraudDetection.Application.Features.Transactions.CreateTransaction;
using FraudDetection.Infrastructure.Configuration;
using FraudDetection.Infrastructure.Messaging;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fraud Detection API",
        Version = "v1",
        Description = "API for creating financial transactions and querying their state. " +
                      "Every created transaction is validated asynchronously by an " +
                      "anti-fraud microservice (Kafka); the API never evaluates fraud " +
                      "rules synchronously — transactions are created as Pending and " +
                      "transition to Approved or Rejected."
    });

    // Enable XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Persistence
builder.Services.AddDbContext<FraudDetectionDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration — Kafka messaging (producer side; the consumer lives in the
// FraudDetection.Worker project). Bound from the "Kafka" section, validated at
// startup (ValidateOnStart) so a misconfigured deployment fails fast.
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddOptions<KafkaOptions>()
    .ValidateOnStart();
builder.Services.ConfigureOptions<KafkaOptionsValidator>();

// Configuration — rate limiting for the transaction creation endpoint, bound
// from the "RateLimit" section. Validated at startup; the limiter itself is
// registered below with AddRateLimiter (built-in System.Threading.RateLimiting).
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.AddOptions<RateLimitOptions>()
    .ValidateOnStart();
builder.Services.ConfigureOptions<RateLimitOptionsValidator>();

// Rate limiting — fixed window over a single global partition (no per-IP
// partitioning, which avoids proxy/IPv6 edge cases). Applied only to the
// transaction creation endpoint via RequireRateLimiting("create-transaction").
// Config-driven values; on rejection the OnRejected callback returns an
// RFC 7807 ProblemDetails (consistent with ExceptionHandlingMiddleware) plus a
// Retry-After header.
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Detail = "The request was rejected because the rate limit was exceeded. Please retry later."
        };

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };

    rateLimiterOptions.AddPolicy("create-transaction", context =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ =>
        {
            var options = context.RequestServices
                .GetRequiredService<IOptions<RateLimitOptions>>().Value;
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            };
        }));
});

// Health checks — real dependency probes for readiness (SQL Server + Kafka),
// per ADR-059. Both checks are registered with the "ready" tag: the
// /health/ready endpoint selects them via Predicate, while /health/live
// selects NO checks at all (a liveness probe must never depend on
// infrastructure it is meant to protect).
//
// The Kafka check receives a concrete ProducerConfig at registration time
// (the AddKafka extension has no IServiceProvider overload); values are read
// from the same "Kafka" section the publisher binds, with the same defaults
// as KafkaOptions. Missing/invalid values surface as a failing check (503 on
// /health/ready) rather than a crash — the KafkaOptions ValidateOnStart
// covers the messaging path.
var kafkaOptions = builder.Configuration
    .GetSection(KafkaOptions.SectionName)
    .Get<KafkaOptions>() ?? new KafkaOptions();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        serviceProvider => serviceProvider
            .GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."),
        name: HealthCheckNames.SqlServer,
        tags: new[] { HealthCheckTags.Ready },
        timeout: TimeSpan.FromSeconds(5))
    .AddKafka(
        new ProducerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            // Bound the produce-during-check so a black-holed broker cannot
            // hang a probe beyond the per-check timeout below.
            MessageTimeoutMs = 5000
        },
        topic: kafkaOptions.Topics.TransactionCreated,
        name: HealthCheckNames.Kafka,
        tags: new[] { HealthCheckTags.Ready },
        timeout: TimeSpan.FromSeconds(5));

// Messaging — the API only PRODUCES TransactionCreated events; the consumer
// (anti-fraud evaluation) runs in the separate FraudDetection.Worker project.
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

// Application Services
builder.Services.AddScoped<CreateTransactionHandler>();
builder.Services.AddScoped<CreateTransactionValidator>();

// Infrastructure — Persistence
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();

var app = builder.Build();

// Global exception handling middleware — catches unhandled exceptions
app.UseMiddleware<FraudDetection.Api.Middleware.ExceptionHandlingMiddleware>();

// Security headers — applied to all responses
app.UseMiddleware<FraudDetection.Api.Middleware.SecurityHeadersMiddleware>();

// HSTS — enforce HTTPS in non-development environments
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Auto-apply pending migrations.
// Runs in Development, or in any environment when AutoMigrate=true
// (used by docker-compose so containers start with a ready schema).
// The database schema is the same one the Worker shares (ADR-054).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
    await context.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
//
// Swagger is enabled in ALL environments (no IsDevelopment condition) — this
// is a public portfolio repository with no sensitive data, and recruiters
// open /swagger directly against the docker-compose container. A real
// production system with sensitive data would keep Swagger development-only
// or behind authentication (see ADR-059 for the trade-off).
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Rate limiting middleware — enforces the "create-transaction" policy on
// endpoints that opt in via RequireRateLimiting.
app.UseRateLimiter();

// Map API endpoints
app.MapTransactions();
app.MapVersion();

// Health check endpoints — see ADR-059.
//
// /health/live is the LIVENESS probe: the process is up and serving requests.
// Its predicate selects NO checks (Predicate => false), so it never evaluates
// SQL Server or Kafka and always returns 200 while the process runs — a
// dependency outage must not mask a live process. Orchestrators (Dockerfile
// HEALTHCHECK, docker-compose healthcheck) use it to decide restarts.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
})
.WithName("HealthCheckLive")
.WithOpenApi();

// /health/ready is the READINESS probe: it evaluates the real dependencies
// (SQL Server + Kafka) and returns 200 only when ALL of them report Healthy;
// 503 otherwise, with a per-dependency breakdown (name/status/durationMs/
// description) produced by the custom ResponseWriter. Caching is disabled so
// every probe re-evaluates; each registered check carries its own 5s timeout
// (HealthCheckOptions has no endpoint-level timeout in .NET 8).
var readinessOptions = new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    AllowCachingResponses = false
};

app.MapHealthChecks("/health/ready", readinessOptions)
    .WithName("HealthCheckReady")
    .WithOpenApi();

// /health — alias of /health/ready, preserved for backwards compatibility
// with the docs, scripts and the pre-existing Dockerfile HEALTHCHECK. The
// response format is now the /health/ready JSON (the old hand-rolled
// {status,timestamp} contract is superseded — see ADR-059).
app.MapHealthChecks("/health", readinessOptions)
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

/// <summary>
/// Exposes the Program class for integration testing.
/// </summary>
public partial class Program { }