using FraudDetection.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Helpers for integration tests to reach the SQLite test database that a
/// <see cref="CustomWebApplicationFactory"/> configured, so tests can assert
/// the persisted state directly (rows, status transitions) through a fresh
/// scoped DbContext.
/// </summary>
public static class CustomWebApplicationFactoryExtensions
{
    /// <summary>
    /// Creates a scoped DbContext backed by the factory's SQLite database.
    /// Callers own the returned context and should dispose it.
    /// </summary>
    public static FraudDetectionDbContext CreateDbContext(this CustomWebApplicationFactory factory)
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
    }
}