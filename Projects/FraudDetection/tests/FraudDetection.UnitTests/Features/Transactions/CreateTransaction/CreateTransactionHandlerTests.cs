using FraudDetection.Application.Features.Transactions.CreateTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraudDetection.UnitTests.Features.Transactions.CreateTransaction;

public class CreateTransactionHandlerTests
{
    private static CreateTransactionCommand CreateCommand(
        decimal value = 100m,
        Guid? sourceAccountId = null,
        Guid? targetAccountId = null,
        int transferTypeId = 1) =>
        new()
        {
            SourceAccountId = sourceAccountId ?? Guid.NewGuid(),
            TargetAccountId = targetAccountId ?? Guid.NewGuid(),
            TransferTypeId = transferTypeId,
            Value = value
        };

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndPersistsPendingTransaction()
    {
        var repository = new FakeTransactionRepository();
        var publisher = new FakeEventPublisher();
        var handler = new CreateTransactionHandler(
            repository, publisher, NullLogger<CreateTransactionHandler>.Instance);
        var command = CreateCommand(value: 120m);

        var result = await handler.Handle(command);

        var persisted = Assert.Single(repository.Stored);
        Assert.NotEqual(Guid.Empty, persisted.TransactionExternalId);
        Assert.Equal(120m, persisted.Value);
        Assert.Equal(command.SourceAccountId, persisted.SourceAccountId);
        Assert.Equal(command.TargetAccountId, persisted.TargetAccountId);
        Assert.Equal(command.TransferTypeId, persisted.TransferTypeId);
        Assert.Equal(TransactionStatus.Pending, persisted.Status);
        Assert.Null(persisted.RejectionReason);
    }

    [Fact]
    public async Task Handle_PersistsBeforePublishing()
    {
        var log = new List<string>();
        var repository = new FakeTransactionRepository(log);
        var publisher = new FakeEventPublisher(log);
        var handler = new CreateTransactionHandler(
            repository, publisher, NullLogger<CreateTransactionHandler>.Instance);

        await handler.Handle(CreateCommand());

        Assert.Equal(new[] { "AddAsync", "PublishAsync" }, log);
    }

    [Fact]
    public async Task Handle_PublishesTransactionCreatedEventWithMatchingFields()
    {
        var repository = new FakeTransactionRepository();
        var publisher = new FakeEventPublisher();
        var handler = new CreateTransactionHandler(
            repository, publisher, NullLogger<CreateTransactionHandler>.Instance);
        var command = CreateCommand(value: 250m);

        var result = await handler.Handle(command);

        var created = Assert.Single(publisher.CreatedEvents);
        Assert.Equal(result.TransactionExternalId, created.TransactionExternalId);
        Assert.Equal(command.SourceAccountId, created.SourceAccountId);
        Assert.Equal(command.TargetAccountId, created.TargetAccountId);
        Assert.Equal(command.TransferTypeId, created.TransferTypeId);
        Assert.Equal(command.Value, created.Value);
        Assert.NotEqual(default(DateTime), created.CreatedAt);
    }

    [Fact]
    public async Task Handle_ReturnsPendingStatusResult()
    {
        var repository = new FakeTransactionRepository();
        var publisher = new FakeEventPublisher();
        var handler = new CreateTransactionHandler(
            repository, publisher, NullLogger<CreateTransactionHandler>.Instance);

        var result = await handler.Handle(CreateCommand());

        Assert.NotEqual(Guid.Empty, result.TransactionExternalId);
        Assert.NotEqual(default(DateTime), result.CreatedAt);
        Assert.Equal("pending", result.Status);
        Assert.Single(publisher.CreatedEvents);
    }

    [Fact]
    public async Task Handle_HighValueTransactionRemainsPending_NoSynchronousEvaluation()
    {
        // Core async requirement (ADR-058): a value above the high-value rule
        // threshold must NOT be evaluated in the request path — the transaction
        // stays Pending and only a TransactionCreated event is published.
        var repository = new FakeTransactionRepository();
        var publisher = new FakeEventPublisher();
        var handler = new CreateTransactionHandler(
            repository, publisher, NullLogger<CreateTransactionHandler>.Instance);

        var result = await handler.Handle(CreateCommand(value: 5000m));

        Assert.Equal("pending", result.Status);
        Assert.Equal(TransactionStatus.Pending, Assert.Single(repository.Stored).Status);
        Assert.Single(publisher.CreatedEvents);
        Assert.Empty(publisher.EvaluatedEvents);
    }

    [Fact]
    public async Task Handle_RepositoryFailure_PropagatesAndDoesNotPublish()
    {
        var repository = new FakeTransactionRepository { AddException = new InvalidOperationException("boom") };
        var publisher = new FakeEventPublisher();
        var handler = new CreateTransactionHandler(
            repository, publisher, NullLogger<CreateTransactionHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand()));

        Assert.Empty(publisher.Published);
    }
}