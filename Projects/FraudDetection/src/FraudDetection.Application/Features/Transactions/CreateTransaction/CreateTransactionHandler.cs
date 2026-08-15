using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Application.Features.Transactions.CreateTransaction;

/// <summary>
/// Handles the CreateTransaction command.
///
/// The handler does NOT evaluate fraud rules — there is no synchronous
/// evaluation in the request path (see ADR-058). It:
///   1. creates the domain Transaction in Pending status (server-generated ID
///      and UTC timestamp),
///   2. persists it via the repository,
///   3. publishes the TransactionCreated integration event to Kafka, which the
///      anti-fraud worker consumes to evaluate the transaction asynchronously.
///
/// Persist-then-publish is a deliberate trade-off: if publishing fails the
/// client sees a 500 while the transaction row remains pending. The proper
/// production fix (transactional outbox) is documented as a future enhancement
/// in ADR-058.
/// </summary>
public sealed class CreateTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateTransactionHandler> _logger;

    /// <summary>
    /// Creates a new CreateTransactionHandler with the required dependencies.
    /// </summary>
    public CreateTransactionHandler(
        ITransactionRepository transactionRepository,
        IEventPublisher eventPublisher,
        ILogger<CreateTransactionHandler> logger)
    {
        Guard.AgainstNull(transactionRepository, nameof(transactionRepository));
        Guard.AgainstNull(eventPublisher, nameof(eventPublisher));
        Guard.AgainstNull(logger, nameof(logger));

        _transactionRepository = transactionRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Executes the CreateTransaction command asynchronously.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction (external ID, creation time, status).</returns>
    public async Task<CreateTransactionResult> Handle(
        CreateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = new Transaction(
            transactionExternalId: Guid.NewGuid(),
            sourceAccountId: command.SourceAccountId,
            targetAccountId: command.TargetAccountId,
            transferTypeId: command.TransferTypeId,
            value: command.Value);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _eventPublisher.PublishAsync(
            new TransactionCreatedEvent(
                transaction.TransactionExternalId,
                transaction.SourceAccountId,
                transaction.TargetAccountId,
                transaction.TransferTypeId,
                transaction.Value,
                transaction.CreatedAt),
            cancellationToken);

        _logger.LogInformation(
            "Transaction {TransactionExternalId} created with status {Status} " +
            "and queued for asynchronous fraud evaluation",
            transaction.TransactionExternalId,
            transaction.Status);

        return new CreateTransactionResult(
            transaction.TransactionExternalId,
            transaction.CreatedAt,
            transaction.Status.ToString().ToLowerInvariant());
    }
}