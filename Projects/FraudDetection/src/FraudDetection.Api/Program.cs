using System.Reflection;
using FraudDetection.Api.Endpoints;
using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.Infrastructure.Persistence.Repositories;
using FraudDetection.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fraud Detection API",
        Version = "v1",
        Description = "API for analyzing financial transactions for potential fraud. " +
                      "Evaluates transactions against configurable rules (high amount, " +
                      "velocity, blacklist, geographic risk) and returns Approved, " +
                      "UnderReview, or Rejected status."
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

// Domain Services (stateless — safe as singleton)
builder.Services.AddSingleton<FraudRuleEngine>();

// Application Services
builder.Services.AddScoped<AnalyzeTransactionHandler>();
builder.Services.AddScoped<AnalyzeTransactionValidator>();

// Infrastructure — Persistence
builder.Services.AddScoped<IFraudRuleProvider, DbFraudRuleProvider>();
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();

var app = builder.Build();

// Global exception handling middleware — catches unhandled exceptions
app.UseMiddleware<FraudDetection.Api.Middleware.ExceptionHandlingMiddleware>();

// Auto-apply pending migrations and seed development data
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
    await context.Database.MigrateAsync();

    // Seed initial fraud rules if none exist
    if (!context.FraudRules.Any())
    {
        context.FraudRules.AddRange(
            new FraudRule(FraudRuleId.New(), "HighAmount", 50, FraudRuleAction.Review),
            new FraudRule(FraudRuleId.New(), "Velocity", 70, FraudRuleAction.Reject),
            new FraudRule(FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject),
            new FraudRule(FraudRuleId.New(), "HighRiskCountry", 30, FraudRuleAction.Review)
        );
        context.SaveChanges();
    }

    // Log the demo blacklisted customer ID for testing the Blacklist rule
    var blacklistedCustomerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    Console.WriteLine($"[Demo] Blacklisted Customer ID for testing: {blacklistedCustomerId}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map API endpoints
app.MapAnalyzeTransaction();

// Health check endpoint
app.MapGet("/health", async (FraudDetectionDbContext context, ILogger<Program> logger) =>
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed");
        return Results.StatusCode(500);
    }
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();

/// <summary>
/// Exposes the Program class for integration testing.
/// </summary>
public partial class Program { }
