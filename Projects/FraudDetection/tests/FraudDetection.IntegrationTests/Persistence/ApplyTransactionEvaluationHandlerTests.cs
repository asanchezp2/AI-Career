using FraudDetection.Application.Events;
using FraudDetection.Application.Features.Transactions.ApplyTransactionEvaluation;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudDetection.IntegrationTests.Persistence;

public class ApplyTransactionEvaluationHandlerTests
{
    [Theory]
    [InlineData(TransactionStatus.Approved, null)]
    [InlineData(TransactionStatus.Rejected, RejectionReason.HighValue)]
    [InlineData(TransactionStatus.Rejected, RejectionReason.DailyAccumulated)]
    public async Task Handle_ResponsePersistsTheTerminalState(
        TransactionStatus status,
        RejectionReason? reason)
    {
        using var database = new SqliteTestDatabase();
        var transaction = new Transaction(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 150m,
            new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc));
        await using (var seedContext = database.CreateContext())
            await new EfTransactionRepository(seedContext).AddAsync(transaction);

        // A real API request resolves a fresh scoped DbContext. Keep the test
        // aligned with that lifecycle instead of tracking the seeded instance.
        await using var applyContext = database.CreateContext();
        var repository = new EfTransactionRepository(applyContext);
        var handler = new ApplyTransactionEvaluationHandler(
            repository,
            NullLogger<ApplyTransactionEvaluationHandler>.Instance);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            status,
            reason));

        Assert.True(result.IsSuccess, result.Error);
        var persisted = await new EfTransactionRepository(database.CreateContext())
            .GetByIdAsync(transaction.TransactionExternalId);
        Assert.NotNull(persisted);
        Assert.Equal(status, persisted!.Status);
        Assert.Equal(reason, persisted.RejectionReason);
    }

    [Fact]
    public async Task Handle_DuplicateResponse_DoesNotRewriteTerminalState()
    {
        using var database = new SqliteTestDatabase();
        var transaction = new Transaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 150m);
        await using (var seedContext = database.CreateContext())
            await new EfTransactionRepository(seedContext).AddAsync(transaction);

        await using var applyContext = database.CreateContext();
        var repository = new EfTransactionRepository(applyContext);
        var handler = new ApplyTransactionEvaluationHandler(
            repository,
            NullLogger<ApplyTransactionEvaluationHandler>.Instance);
        var response = new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            TransactionStatus.Approved,
            null);

        Assert.True((await handler.Handle(response)).IsSuccess);
        Assert.True((await handler.Handle(response)).IsSuccess);

        var persisted = await new EfTransactionRepository(database.CreateContext())
            .GetByIdAsync(transaction.TransactionExternalId);
        Assert.Equal(TransactionStatus.Approved, persisted!.Status);
    }
}
