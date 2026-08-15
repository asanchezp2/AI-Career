using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FraudDetection.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core CLI tools (dotnet ef) to create a
/// DbContext WITHOUT booting the API host. This keeps migration commands
/// independent of the application's Program.cs (and its auto-migrate/DI
/// pipeline) — a standard production pattern for scripted schema operations.
///
/// The connection string is read from the ConnectionStrings__DefaultConnection
/// environment variable; the fallback is a design-time-only placeholder —
/// migrations that do not touch the database (e.g. migrations add --no-connect)
/// never use it. This factory is never used at runtime.
/// </summary>
public sealed class FraudDetectionDbContextFactory : IDesignTimeDbContextFactory<FraudDetectionDbContext>
{
    /// <inheritdoc />
    public FraudDetectionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=FraudDetectionDb;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<FraudDetectionDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FraudDetectionDbContext(options);
    }
}