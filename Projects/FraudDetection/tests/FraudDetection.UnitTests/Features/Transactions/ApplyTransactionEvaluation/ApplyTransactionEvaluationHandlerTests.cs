using FraudDetection.Application.Events;
using FraudDetection.Application.Features.Transactions.ApplyTransactionEvaluation;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudDetection.UnitTests.Features.Transactions.ApplyTransactionEvaluation;

public class ApplyTransactionEvaluationHandlerTests
{
    private static Transaction CreatePendingTransaction(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

    private static ApplyTransactionEvaluationHandler CreateHandler(FakeTransactionRepository repository) =>
        new(repository, NullLogger<ApplyTransactionEvaluationHandler>.Instance);

    [Fact]
    public async Task Handle_ApprovedResponse_PersistsApprovedStatus()
    {
        var transaction = CreatePendingTransaction();
        var repository = new FakeTransactionRepository();
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            TransactionStatus.Approved,
            null));

        Assert.True(result.IsSuccess);
        Assert.Contains("UpdateAsync", repository.OperationLog);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
        Assert.Null(transaction.RejectionReason);
    }

    [Fact]
    public async Task Handle_RejectedResponse_PersistsReasonAndStatus()
    {
        var transaction = CreatePendingTransaction();
        var repository = new FakeTransactionRepository();
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            TransactionStatus.Rejected,
            RejectionReason.HighValue));

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
        Assert.Equal(RejectionReason.HighValue, transaction.RejectionReason);
    }

    [Fact]
    public async Task Handle_DuplicateSameResponse_IsIdempotent()
    {
        var transaction = CreatePendingTransaction();
        transaction.Approve();
        var repository = new FakeTransactionRepository();
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            TransactionStatus.Approved,
            null));

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
    }

    [Fact]
    public async Task Handle_ConflictingResponseForTerminalTransaction_FailsWithoutOverwrite()
    {
        var transaction = CreatePendingTransaction();
        transaction.Approve();
        var repository = new FakeTransactionRepository();
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            TransactionStatus.Rejected,
            RejectionReason.HighValue));

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
    }

    [Theory]
    [InlineData(TransactionStatus.Pending, null)]
    [InlineData(TransactionStatus.Approved, RejectionReason.HighValue)]
    [InlineData(TransactionStatus.Rejected, null)]
    public async Task Handle_InvalidStatusReasonCombination_FailsWithoutPersisting(
        TransactionStatus status,
        RejectionReason? reason)
    {
        var transaction = CreatePendingTransaction();
        var repository = new FakeTransactionRepository();
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            transaction.TransactionExternalId,
            status,
            reason));

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
    }

    [Fact]
    public async Task Handle_UnknownTransaction_FailsWithoutUpdate()
    {
        var repository = new FakeTransactionRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new TransactionEvaluatedEvent(
            Guid.NewGuid(),
            TransactionStatus.Approved,
            null));

        Assert.True(result.IsFailure);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
    }

    [Fact]
    public async Task Handle_PersistenceFailure_PropagatesSoConsumerCanRetryWithoutCommitting()
    {
        var transaction = CreatePendingTransaction();
        var repository = new FakeTransactionRepository
        {
            UpdateException = new InvalidOperationException("temporary database failure")
        };
        repository.Seed(transaction);
        var handler = CreateHandler(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new TransactionEvaluatedEvent(
                transaction.TransactionExternalId,
                TransactionStatus.Approved,
                null)));

        Assert.Equal("temporary database failure", exception.Message);
        Assert.Contains("UpdateAsync", repository.OperationLog);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
    }
}
