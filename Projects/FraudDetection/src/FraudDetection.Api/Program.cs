using FraudDetection.Api.Endpoints;
using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Services;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Include XML comments for endpoint documentation
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
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

// Infrastructure
// Currently using InMemory provider. Switch to DbFraudRuleProvider when the database is ready:
// builder.Services.AddScoped<IFraudRuleProvider, DbFraudRuleProvider>();
builder.Services.AddSingleton<IFraudRuleProvider, InMemoryFraudRuleProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map API endpoints
app.MapAnalyzeTransaction();

app.Run();

/// <summary>
/// Exposes the Program class for integration testing.
/// </summary>
public partial class Program { }
