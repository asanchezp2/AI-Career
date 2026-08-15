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
///      at-least-once Kafka delivery (see ADR-058),
///   4. compute the day's accumulated value for the source account (INCLUDING
///      this transaction, which is already persisted as Pending — ADR-057),
///   5. run the fraud rules via FraudRuleEngine,
///   6. apply the recommended status through domain behavior, persist it, and
///      return the result.
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

        var statusResult = ApplyRecommendedStatus(transaction, evaluation);
        if (statusResult.IsFailure)
        {
            // A transition failure here is a programming error: the transaction
            // was loaded as Pending and the engine only recommends Pending-exit
            // statuses. Fail loudly rather than silently dropping the message.
            _logger.LogError(
                "Failed to apply recommended status {Status} to transaction {TransactionExternalId}: {Error}",
                evaluation.RecommendedStatus,
                transaction.TransactionExternalId,
                statusResult.Error);
            throw new InvalidOperationException(statusResult.Error);
        }

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Transaction {TransactionExternalId} evaluated: {Status}{RejectionReason}",
            transaction.TransactionExternalId,
            transaction.Status,
            transaction.RejectionReason is not null ? $" ({transaction.RejectionReason})" : string.Empty);

        return new EvaluateTransactionResult(
            transaction.TransactionExternalId,
            transaction.Status,
            transaction.RejectionReason);
    }

    /// <summary>
    /// Applies the engine's recommended status to the transaction using domain behavior.
    /// </summary>
    private static Result ApplyRecommendedStatus(Transaction transaction, FraudRuleEngineResult evaluation)
    {
        return evaluation.RecommendedStatus switch
        {
            TransactionStatus.Approved => transaction.Approve(),
            TransactionStatus.Rejected => transaction.Reject(evaluation.RejectionReason!.Value),
            _ => Result.Failure($"Invalid recommended status: {evaluation.RecommendedStatus}")
        };
    }
}