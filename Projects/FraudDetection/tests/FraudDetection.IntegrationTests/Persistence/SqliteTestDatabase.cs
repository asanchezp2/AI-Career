using FraudDetection.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.IntegrationTests.Persistence;

/// <summary>
/// An in-memory SQLite database shared by the repository tests. A single open
/// in-memory connection is kept alive for the lifetime of the helper so that
/// every DbContext created through <see cref="CreateContext"/> observes the
/// same schema and committed data (an in-memory SQLite store is per-connection,
/// so several connections would each get a fresh empty database).
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a fresh DbContext backed by the shared in-memory connection.
    /// Callers own the returned context and should dispose it.
    /// </summary>
    public FraudDetectionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FraudDetectionDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new FraudDetectionDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}