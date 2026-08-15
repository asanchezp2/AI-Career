using FraudDetection.Application.Exceptions;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FraudDetection.IntegrationTests.Persistence;

public class EfTransactionRepositoryTests
{
    private static readonly Guid SourceAccountId = Guid.NewGuid();
    private static readonly Guid TargetAccountId = Guid.NewGuid();
    private static readonly DateTime TodayMorning = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

    private static Transaction CreateTransaction(
        decimal value,
        DateTime? createdAt = null,
        Guid? id = null,
        Guid? sourceAccountId = null) =>
        new(
            id ?? Guid.NewGuid(),
            sourceAccountId ?? SourceAccountId,
            TargetAccountId,
            1,
            value,
            createdAt ?? TodayMorning);

    [Fact]
    public async Task AddAndGetById_RoundTrips()
    {
        using var database = new SqliteTestDatabase();
        var transaction = CreateTransaction(150m);
        var repository = new EfTransactionRepository(database.CreateContext());

        await repository.AddAsync(transaction);

        var loaded = await repository.GetByIdAsync(transaction.TransactionExternalId);
        Assert.NotNull(loaded);
        Assert.Equal(transaction.TransactionExternalId, loaded!.TransactionExternalId);
        Assert.Equal(150m, loaded.Value);
        Assert.Equal(SourceAccountId, loaded.SourceAccountId);
        Assert.Equal(TargetAccountId, loaded.TargetAccountId);
        Assert.Equal(1, loaded.TransferTypeId);
        Assert.Equal(TransactionStatus.Pending, loaded.Status);
        Assert.Null(loaded.RejectionReason);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        using var database = new SqliteTestDatabase();
        var repository = new EfTransactionRepository(database.CreateContext());

        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    [Fact]
    public async Task Add_DuplicateId_ThrowsTransactionConflictException()
    {
        using var database = new SqliteTestDatabase();
        var transaction = CreateTransaction(100m);
        await new EfTransactionRepository(database.CreateContext()).AddAsync(transaction);

        // A second repository/context performs the duplicate insert — a fresh
        // context cannot observe the tracked instance from the first insert.
        await Assert.ThrowsAsync<TransactionConflictException>(
            () => new EfTransactionRepository(database.CreateContext()).AddAsync(transaction));
    }

    [Fact]
    public async Task GetDailyAccumulated_SameAccountSameDay_SumsValues()
    {
        using var database = new SqliteTestDatabase();
        var repository = new EfTransactionRepository(database.CreateContext());
        await repository.AddAsync(CreateTransaction(100m, TodayMorning));
        await repository.AddAsync(CreateTransaction(250.50m, TodayMorning.AddHours(3)));
        await repository.AddAsync(CreateTransaction(150m, TodayMorning.AddHours(8)));

        var total = await repository.GetDailyAccumulatedAsync(SourceAccountId, new DateOnly(2026, 3, 10));

        Assert.Equal(500.50m, total);
    }

    [Fact]
    public async Task GetDailyAccumulated_DifferentAccounts_AreIsolated()
    {
        using var database = new SqliteTestDatabase();
        var otherAccount = Guid.NewGuid();
        var repository = new EfTransactionRepository(database.CreateContext());
        await repository.AddAsync(CreateTransaction(100m, TodayMorning));
        await repository.AddAsync(CreateTransaction(900m, TodayMorning, sourceAccountId: otherAccount));

        var total = await repository.GetDailyAccumulatedAsync(SourceAccountId, new DateOnly(2026, 3, 10));

        Assert.Equal(100m, total);
    }

    [Fact]
    public async Task GetDailyAccumulated_DifferentDays_AreIsolated()
    {
        using var database = new SqliteTestDatabase();
        var day = new DateOnly(2026, 3, 10);
        var repository = new EfTransactionRepository(database.CreateContext());
        await repository.AddAsync(CreateTransaction(100m, day.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc)));
        await repository.AddAsync(CreateTransaction(500m, day.AddDays(-1).ToDateTime(new TimeOnly(23, 30), DateTimeKind.Utc)));

        var todayTotal = await repository.GetDailyAccumulatedAsync(SourceAccountId, day);
        var yesterdayTotal = await repository.GetDailyAccumulatedAsync(SourceAccountId, day.AddDays(-1));

        Assert.Equal(100m, todayTotal);
        Assert.Equal(500m, yesterdayTotal);
    }

    [Fact]
    public async Task GetDailyAccumulated_IncludesTheTransactionBeingEvaluated()
    {
        // The pending transaction is persisted BEFORE the anti-fraud evaluation
        // runs, so the day's sum includes it — the rule sees 20000.10 > 20000
        // when the combined day reaches the limit (ADR-057).
        using var database = new SqliteTestDatabase();
        var repository = new EfTransactionRepository(database.CreateContext());
        await repository.AddAsync(CreateTransaction(19900m, TodayMorning));
        await repository.AddAsync(CreateTransaction(100.10m, TodayMorning.AddHours(6), sourceAccountId: SourceAccountId));

        var total = await repository.GetDailyAccumulatedAsync(SourceAccountId, new DateOnly(2026, 3, 10));

        Assert.Equal(20000.10m, total);
    }

    [Fact]
    public async Task GetDailyAccumulated_NoTransactions_ReturnsZero()
    {
        using var database = new SqliteTestDatabase();
        var repository = new EfTransactionRepository(database.CreateContext());

        var total = await repository.GetDailyAccumulatedAsync(Guid.NewGuid(), new DateOnly(2026, 3, 10));

        Assert.Equal(0m, total);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusTransition()
    {
        using var database = new SqliteTestDatabase();
        var transaction = CreateTransaction(120m);
        await new EfTransactionRepository(database.CreateContext()).AddAsync(transaction);

        // Reproduce the evaluation flow: load (AsNoTracking), apply the domain
        // transition, persist through UpdateAsync.
        var repository = new EfTransactionRepository(database.CreateContext());
        var loaded = await repository.GetByIdAsync(transaction.TransactionExternalId);
        Assert.True(loaded!.Approve().IsSuccess);
        await repository.UpdateAsync(loaded);

        var reloaded = await repository.GetByIdAsync(transaction.TransactionExternalId);
        Assert.Equal(TransactionStatus.Approved, reloaded!.Status);
        Assert.Null(reloaded.RejectionReason);
    }

    [Fact]
    public async Task PersistedStatusAndReason_AreLowercaseStrings()
    {
        using var database = new SqliteTestDatabase();
        var transaction = CreateTransaction(120m);
        await new EfTransactionRepository(database.CreateContext()).AddAsync(transaction);

        Assert.Equal(
            "pending",
            await ReadRawColumnAsync(database, "Status", transaction.TransactionExternalId));

        var repository = new EfTransactionRepository(database.CreateContext());
        var loaded = await repository.GetByIdAsync(transaction.TransactionExternalId);
        Assert.True(loaded!.Reject(RejectionReason.HighValue).IsSuccess);
        await repository.UpdateAsync(loaded);

        Assert.Equal(
            "rejected",
            await ReadRawColumnAsync(database, "Status", transaction.TransactionExternalId));
        Assert.Equal(
            "highvalue",
            await ReadRawColumnAsync(database, "RejectionReason", transaction.TransactionExternalId));
    }

    private static async Task<string?> ReadRawColumnAsync(
        SqliteTestDatabase database,
        string column,
        Guid transactionExternalId)
    {
        using var context = database.CreateContext();
        var connection = context.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT \"{column}\" FROM \"Transactions\" WHERE \"TransactionExternalId\" = @id";
        command.Parameters.Add(new SqliteParameter("@id", transactionExternalId));

        return await command.ExecuteScalarAsync() as string;
    }
}