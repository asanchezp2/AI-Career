using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Application.Features.Transactions.ApplyTransactionEvaluation;

/// <summary>
/// Applies the anti-fraud worker's Kafka response to the persisted transaction.
/// A successful result is safe to commit; failures describe permanent invalid
/// or uncorrelated messages, while persistence exceptions are allowed to bubble
/// so the Kafka consumer can retry without committing the offset.
/// </summary>
public sealed class ApplyTransactionEvaluationHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<ApplyTransactionEvaluationHandler> _logger;

    public ApplyTransactionEvaluationHandler(
        ITransactionRepository transactionRepository,
        ILogger<ApplyTransactionEvaluationHandler> logger)
    {
        Guard.AgainstNull(transactionRepository, nameof(transactionRepository));
        Guard.AgainstNull(logger, nameof(logger));

        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        TransactionEvaluatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(@event, nameof(@event));

        var validationError = Validate(@event);
        if (validationError is not null)
            return Result.Failure(validationError);

        var transaction = await _transactionRepository.GetByIdAsync(
            @event.TransactionExternalId,
            cancellationToken);

        if (transaction is null)
        {
            _logger.LogWarning(
                "Evaluation response references unknown transaction {TransactionExternalId}",
                @event.TransactionExternalId);
            return Result.Failure("The response references an unknown transaction.");
        }

        if (transaction.Status != TransactionStatus.Pending)
        {
            if (transaction.Status == @event.Status
                && transaction.RejectionReason == @event.RejectionReason)
            {
                _logger.LogInformation(
                    "Duplicate evaluation response ignored for transaction {TransactionExternalId}",
                    transaction.TransactionExternalId);
                return Result.Success();
            }

            _logger.LogError(
                "Conflicting evaluation response for terminal transaction {TransactionExternalId}: " +
                "stored {StoredStatus}/{StoredReason}, received {ReceivedStatus}/{ReceivedReason}",
                transaction.TransactionExternalId,
                transaction.Status,
                transaction.RejectionReason,
                @event.Status,
                @event.RejectionReason);
            return Result.Failure("A conflicting response was received for a terminal transaction.");
        }

        var transition = @event.Status switch
        {
            TransactionStatus.Approved => transaction.Approve(),
            TransactionStatus.Rejected => transaction.Reject(@event.RejectionReason!.Value),
            _ => Result.Failure($"Unsupported response status: {@event.Status}.")
        };

        if (transition.IsFailure)
            return transition;

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Transaction {TransactionExternalId} status updated from worker response to {Status}{Reason}",
            transaction.TransactionExternalId,
            transaction.Status,
            transaction.RejectionReason is not null ? $" ({transaction.RejectionReason})" : string.Empty);

        return Result.Success();
    }

    private static string? Validate(TransactionEvaluatedEvent @event)
    {
        if (@event.TransactionExternalId == Guid.Empty)
            return "TransactionExternalId must not be empty.";

        if (!Enum.IsDefined(typeof(TransactionStatus), @event.Status))
            return "The response contains an undefined transaction status.";

        return @event.Status switch
        {
            TransactionStatus.Approved when @event.RejectionReason is not null =>
                "An approved transaction cannot have a rejection reason.",
            TransactionStatus.Rejected when @event.RejectionReason is null =>
                "A rejected transaction must have a rejection reason.",
            TransactionStatus.Rejected when @event.RejectionReason is { } reason
                && !Enum.IsDefined(typeof(RejectionReason), reason) =>
                "The response contains an undefined rejection reason.",
            TransactionStatus.Pending =>
                "The response cannot transition a transaction to Pending.",
            _ => null
        };
    }
}
