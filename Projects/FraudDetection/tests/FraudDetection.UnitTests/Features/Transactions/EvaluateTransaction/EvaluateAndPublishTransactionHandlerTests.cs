using FraudDetection.Application.Events;
using FraudDetection.Application.Features.Transactions.EvaluateTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudDetection.UnitTests.Features.Transactions.EvaluateTransaction;

public class EvaluateAndPublishTransactionHandlerTests
{
    [Fact]
    public async Task Handle_EvaluatesAndPublishesResponseWithoutChangingStoredState()
    {
        var transaction = new Transaction(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 2500m,
            new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc));
        var repository = new FakeTransactionRepository { DailyAccumulated = 2500m };
        repository.Seed(transaction);
        var publisher = new FakeEventPublisher();
        var evaluator = new EvaluateTransactionHandler(
            repository,
            new FraudRuleEngine(),
            NullLogger<EvaluateTransactionHandler>.Instance);
        var handler = new EvaluateAndPublishTransactionHandler(evaluator, publisher);

        var result = await handler.Handle(new TransactionCreatedEvent(
            transaction.TransactionExternalId,
            transaction.SourceAccountId,
            transaction.TargetAccountId,
            transaction.TransferTypeId,
            transaction.Value,
            transaction.CreatedAt));

        Assert.NotNull(result);
        Assert.Equal(TransactionStatus.Rejected, result.Status);
        Assert.Equal(RejectionReason.HighValue, result.RejectionReason);
        Assert.DoesNotContain("UpdateAsync", repository.OperationLog);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);

        var response = Assert.Single(publisher.EvaluatedEvents);
        Assert.Equal(transaction.TransactionExternalId, response.TransactionExternalId);
        Assert.Equal(TransactionStatus.Rejected, response.Status);
        Assert.Equal(RejectionReason.HighValue, response.RejectionReason);
    }
}
