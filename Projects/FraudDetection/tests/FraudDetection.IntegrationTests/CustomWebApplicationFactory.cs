using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces the SQL Server database with
/// an in-memory SQLite database for integration testing.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to "Testing" so the Program.cs migration/seeding doesn't run
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FraudDetectionDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            // Add SQLite in-memory database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<FraudDetectionDbContext>(options =>
                options.UseSqlite(_connection));

            // Ensure the database schema is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
            context.Database.EnsureCreated();

            // Seed initial fraud rules for integration tests
            if (!context.FraudRules.Any())
            {
                context.FraudRules.AddRange(
                    new FraudRule(
                        FraudRuleId.New(), "HighAmount", 50, FraudRuleAction.Review),
                    new FraudRule(
                        FraudRuleId.New(), "Velocity", 70, FraudRuleAction.Reject),
                    new FraudRule(
                        FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject),
                    new FraudRule(
                        FraudRuleId.New(), "HighRiskCountry", 30, FraudRuleAction.Review)
                );
                context.SaveChanges();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
