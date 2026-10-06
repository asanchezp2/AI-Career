using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;
using FraudDetection.Domain;

namespace FraudDetection.Application.Features.Transactions.EvaluateTransaction;

/// <summary>
/// Orchestrates the anti-fraud use case: evaluate a created transaction and
/// publish the response consumed by the API to update its state.
/// </summary>
public sealed class EvaluateAndPublishTransactionHandler
{
    private readonly EvaluateTransactionHandler _evaluationHandler;
    private readonly IEventPublisher _eventPublisher;

    public EvaluateAndPublishTransactionHandler(
        EvaluateTransactionHandler evaluationHandler,
        IEventPublisher eventPublisher)
    {
        Guard.AgainstNull(evaluationHandler, nameof(evaluationHandler));
        Guard.AgainstNull(eventPublisher, nameof(eventPublisher));

        _evaluationHandler = evaluationHandler;
        _eventPublisher = eventPublisher;
    }

    public async Task<EvaluateTransactionResult?> Handle(
        TransactionCreatedEvent created,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(created, nameof(created));

        var evaluation = await _evaluationHandler.Handle(
            new EvaluateTransactionCommand
            {
                TransactionExternalId = created.TransactionExternalId
            },
            cancellationToken);

        if (evaluation is null)
            return null;

        await _eventPublisher.PublishAsync(
            new TransactionEvaluatedEvent(
                evaluation.TransactionExternalId,
                evaluation.Status,
                evaluation.RejectionReason),
            cancellationToken);

        return evaluation;
    }
}
