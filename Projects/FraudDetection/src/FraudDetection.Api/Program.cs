using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Services;
using FraudDetection.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Domain Services
builder.Services.AddSingleton<FraudRuleEngine>();

// Application Services
builder.Services.AddScoped<AnalyzeTransactionHandler>();

// Infrastructure
builder.Services.AddSingleton<IFraudRuleProvider, InMemoryFraudRuleProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
