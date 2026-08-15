using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FraudDetection.IntegrationTests.Persistence;

/// <summary>
/// Applies the production migration (InitialCreate) to a fresh temp-file SQLite
/// database and asserts the resulting schema: the Transactions table, its
/// composite index, and the EF migrations history row.
/// </summary>
public class MigrationsTests
{
    private static readonly string DatabasePath =
        Path.Combine(Path.GetTempPath(), $"FraudDetectionMigrations-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task MigrateAsync_AppliesInitialCreateSchema()
    {
        try
        {
            var options = new DbContextOptionsBuilder<FraudDetectionDbContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;

            using (var context = new FraudDetectionDbContext(options))
            {
                await context.Database.MigrateAsync();
            }

            using (var context = new FraudDetectionDbContext(options))
            {
                var tables = await QueryNameColumnAsync(context, "type='table'");
                Assert.Contains("Transactions", tables);
                Assert.Contains("__EFMigrationsHistory", tables);

                var indexes = await QueryNameColumnAsync(context, "type='index'");
                Assert.Contains("IX_Transactions_SourceAccountId_CreatedAt", indexes);

                var applied = await context.Database.GetAppliedMigrationsAsync();
                Assert.Contains("20260813020511_InitialCreate", applied);
            }
        }
        finally
        {
            foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
            {
                try
                {
                    File.Delete(DatabasePath + suffix);
                }
                catch (Exception)
                {
                    // Best-effort cleanup of the temp database.
                }
            }
        }
    }

    private static async Task<List<string>> QueryNameColumnAsync(
        FraudDetectionDbContext context,
        string whereClause)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM sqlite_master WHERE {whereClause}";

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        return names;
    }
}