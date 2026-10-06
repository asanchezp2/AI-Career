using FraudDetection.Application.Abstractions;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Application.Features.Transactions.EvaluateTransaction;

/// <summary>
/// Handles the EvaluateTransaction command — the anti-fraud evaluation flow.
/// Lives in the Application layer (rather than in the Worker project) so the
/// whole evaluation logic is unit-testable without Kafka or hosting concerns.
///
/// Flow:
///   1. load the transaction by its external identifier,
///   2. if missing — log and return null (the worker skips publishing),
///   3. if already evaluated (not Pending) — replay: return the current state
///      without re-evaluating. This makes the consumer idempotent under
///      at-least-once Kafka delivery (see ARCHITECTURE.md),
///   4. compute the day's accumulated value for the source account (INCLUDING
///      this transaction, which is already persisted as Pending — ARCHITECTURE.md),
///   5. run the fraud rules via FraudRuleEngine,
///   6. return the recommended status for the Worker to publish.
///
/// The worker does not persist the status. The API applies the returned
/// TransactionEvaluated event so the response message is the state-changing
/// contract required by the challenge.
/// </summary>
public sealed class EvaluateTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly FraudRuleEngine _engine;
    private readonly ILogger<EvaluateTransactionHandler> _logger;

    /// <summary>
    /// Creates a new EvaluateTransactionHandler with the required dependencies.
    /// </summary>
    public EvaluateTransactionHandler(
        ITransactionRepository transactionRepository,
        FraudRuleEngine engine,
        ILogger<EvaluateTransactionHandler> logger)
    {
        Guard.AgainstNull(transactionRepository, nameof(transactionRepository));
        Guard.AgainstNull(engine, nameof(engine));
        Guard.AgainstNull(logger, nameof(logger));

        _transactionRepository = transactionRepository;
        _engine = engine;
        _logger = logger;
    }

    /// <summary>
    /// Executes the EvaluateTransaction command asynchronously.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The evaluation result, or null when the transaction does not exist
    /// (the worker logs and does not publish an evaluation for it).
    /// </returns>
    public async Task<EvaluateTransactionResult?> Handle(
        EvaluateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(
            command.TransactionExternalId, cancellationToken);

        if (transaction is null)
        {
            _logger.LogWarning(
                "Evaluation skipped: transaction {TransactionExternalId} not found",
                command.TransactionExternalId);
            return null;
        }

        // At-least-once delivery may redeliver an already-processed message
        // (crash between persist and commit). Re-evaluating is a no-op then —
        // return the current state so the worker republishes it consistently.
        if (transaction.Status != TransactionStatus.Pending)
        {
            _logger.LogInformation(
                "Evaluation replay for transaction {TransactionExternalId}: already {Status}",
                transaction.TransactionExternalId,
                transaction.Status);
            return new EvaluateTransactionResult(
                transaction.TransactionExternalId,
                transaction.Status,
                transaction.RejectionReason);
        }

        var day = DateOnly.FromDateTime(transaction.CreatedAt);
        var dailyAccumulated = await _transactionRepository.GetDailyAccumulatedAsync(
            transaction.SourceAccountId, day, cancellationToken);

        var evaluation = _engine.Evaluate(transaction, dailyAccumulated);

        _logger.LogInformation(
            "Transaction {TransactionExternalId} evaluated: recommended status {Status}{RejectionReason}",
            transaction.TransactionExternalId,
            evaluation.RecommendedStatus,
            evaluation.RejectionReason is not null ? $" ({evaluation.RejectionReason})" : string.Empty);

        return new EvaluateTransactionResult(
            transaction.TransactionExternalId,
            evaluation.RecommendedStatus,
            evaluation.RejectionReason);
    }
}
