using FraudDetection.Application.Features.Transactions.EvaluateTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudDetection.UnitTests.Features.Transactions.EvaluateTransaction;

public class EvaluateTransactionHandlerTests
{
    private static readonly DateTime FixedCreatedAt = new(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc);

    private static Transaction CreatePendingTransaction(decimal value, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, value, FixedCreatedAt);

    private static EvaluateTransactionHandler CreateHandler(
        FakeTransactionRepository repository,
        FakeEventPublisher? publisher = null) =>
        new(repository, new FraudRuleEngine(), NullLogger<EvaluateTransactionHandler>.Instance);

    [Fact]
    public async Task Handle_TransactionNotFound_ReturnsNullAndDoesNotUpdate()
    {
        var repository = new FakeTransactionRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new EvaluateTransactionCommand { TransactionExternalId = Guid.NewGuid() });

        Assert.Null(result);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
        Assert.DoesNotContain("GetDailyAccumulatedAsync", repository.OperationLog);
    }

    [Fact]
    public async Task Handle_AlreadyEvaluatedTransaction_ReplaysCurrentState()
    {
        // At-least-once delivery may redeliver a processed message; the handler
        // must be idempotent — an already-evaluated transaction is replayed
        // without re-running the rules or persisting again (ADR-058).
        var transaction = CreatePendingTransaction(100m);
        transaction.Approve();
        var repository = new FakeTransactionRepository();
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = transaction.TransactionExternalId });

        Assert.NotNull(result);
        Assert.Equal(transaction.TransactionExternalId, result.TransactionExternalId);
        Assert.Equal(TransactionStatus.Approved, result.Status);
        Assert.Null(result.RejectionReason);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
        Assert.DoesNotContain("GetDailyAccumulatedAsync", repository.OperationLog);
    }

    [Fact]
    public async Task Handle_HighValueTransaction_RejectsAndPersists()
    {
        var transaction = CreatePendingTransaction(2500m);
        var repository = new FakeTransactionRepository { DailyAccumulated = 1000m };
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = transaction.TransactionExternalId });

        Assert.Equal(TransactionStatus.Rejected, result!.Status);
        Assert.Equal(RejectionReason.HighValue, result.RejectionReason);
        Assert.Contains("UpdateAsync", repository.OperationLog);
        Assert.Equal(TransactionStatus.Rejected, Assert.Single(repository.Stored).Status);
        Assert.Equal(RejectionReason.HighValue, Assert.Single(repository.Stored).RejectionReason);
    }

    [Fact]
    public async Task Handle_DailyAccumulatedExceeded_RejectsAndPersists()
    {
        var transaction = CreatePendingTransaction(100m);
        var repository = new FakeTransactionRepository { DailyAccumulated = 25000m };
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = transaction.TransactionExternalId });

        Assert.Equal(TransactionStatus.Rejected, result!.Status);
        Assert.Equal(RejectionReason.DailyAccumulated, result.RejectionReason);
        Assert.Contains("UpdateAsync", repository.OperationLog);
    }

    [Fact]
    public async Task Handle_UnderBothLimits_ApprovesAndPersists()
    {
        var transaction = CreatePendingTransaction(100m);
        var repository = new FakeTransactionRepository { DailyAccumulated = 1000m };
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = transaction.TransactionExternalId });

        Assert.Equal(TransactionStatus.Approved, result!.Status);
        Assert.Null(result.RejectionReason);
        Assert.Contains("UpdateAsync", repository.OperationLog);
        Assert.Equal(TransactionStatus.Approved, Assert.Single(repository.Stored).Status);
        Assert.Null(Assert.Single(repository.Stored).RejectionReason);
    }

    [Fact]
    public async Task Handle_AccumulatedIncludingCurrentTransactionExceedsLimit_Rejects()
    {
        // The repository accumulates the day INCLUDING the evaluated transaction,
        // which is already persisted as Pending (ADR-057). The fake models that:
        // 200 (current) + 19950.10 (earlier same-day) = 20050.10 → rule fires.
        var transaction = CreatePendingTransaction(200m);
        var repository = new FakeTransactionRepository { DailyAccumulated = 20050.10m };
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = transaction.TransactionExternalId });

        Assert.Equal(TransactionStatus.Rejected, result!.Status);
        Assert.Equal(RejectionReason.DailyAccumulated, result.RejectionReason);
    }

    [Fact]
    public async Task Handle_GetDailyAccumulated_UsesTransactionDayBoundary()
    {
        var transaction = CreatePendingTransaction(100m);
        DateOnly? queriedDay = null;
        var repository = new FakeTransactionRepository { DailyAccumulated = 1000m };
        repository.Seed(transaction);
        repository.OnGetDailyAccumulated = (sourceAccountId, day) => queriedDay = day;
        var handler = CreateHandler(repository);

        await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = transaction.TransactionExternalId });

        Assert.Equal(DateOnly.FromDateTime(FixedCreatedAt), queriedDay);
    }
}