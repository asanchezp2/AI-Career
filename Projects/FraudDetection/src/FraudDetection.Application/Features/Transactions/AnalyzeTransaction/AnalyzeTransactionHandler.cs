using FraudDetection.Application.Abstractions;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Handles the AnalyzeTransaction command.
/// Creates a transaction, evaluates it against fraud rules with real velocity data,
/// applies the recommended status, and persists the result.
/// </summary>
public sealed class AnalyzeTransactionHandler
{
    private readonly FraudRuleEngine _engine;
    private readonly IFraudRuleProvider _ruleProvider;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<AnalyzeTransactionHandler> _logger;

    /// <summary>
    /// Creates a new AnalyzeTransactionHandler with the required dependencies.
    /// </summary>
    public AnalyzeTransactionHandler(
        FraudRuleEngine engine,
        IFraudRuleProvider ruleProvider,
        ITransactionRepository transactionRepository,
        ILogger<AnalyzeTransactionHandler> logger)
    {
        Guard.AgainstNull(engine, nameof(engine));
        Guard.AgainstNull(ruleProvider, nameof(ruleProvider));
        Guard.AgainstNull(transactionRepository, nameof(transactionRepository));
        Guard.AgainstNull(logger, nameof(logger));

        _engine = engine;
        _ruleProvider = ruleProvider;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executes the AnalyzeTransaction command asynchronously.
    /// Creates a transaction with optional country and metadata, queries real velocity data,
    /// evaluates against fraud rules, persists, and returns the result.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the analysis.</returns>
    public async Task<AnalyzeTransactionResult> Handle(
        AnalyzeTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transactionId = TransactionId.From(command.TransactionId);
        var customerId = CustomerId.From(command.CustomerId);
        var amount = new Money(command.Amount, command.Currency);

        _logger.LogInformation(
            "Transaction analysis started for customer {CustomerId}",
            customerId.Value);

        var transaction = new Transaction(
            transactionId,
            customerId,
            amount,
            command.Timestamp,
            command.Country,
            command.Metadata);

        // Query real velocity: count transactions in the last hour
        var since = DateTime.UtcNow.AddHours(-1);
        var recentCount = await _transactionRepository.GetTransactionCountSinceAsync(
            customerId, since, cancellationToken);
        transaction.RecentTransactionCount = recentCount;

        var rules = _ruleProvider.GetAllRules();
        var specifications = _ruleProvider.GetSpecifications();
        var evaluation = _engine.Evaluate(transaction, rules, specifications);

        var statusResult = ApplyRecommendedStatus(transaction, evaluation.RecommendedStatus);
        if (statusResult.IsFailure)
        {
            _logger.LogError(
                "Failed to apply status {RecommendedStatus} to transaction {TransactionId}: {Error}",
                evaluation.RecommendedStatus,
                transaction.Id.Value,
                statusResult.Error);
            throw new InvalidOperationException(statusResult.Error);
        }

        // Persist the evaluated transaction
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Transaction {TransactionId} analyzed with status {Status} and risk score {RiskScore}",
            transaction.Id.Value,
            transaction.Status,
            evaluation.TotalRiskScore);

        _logger.LogInformation(
            "Transaction {TransactionId} persisted successfully",
            transaction.Id.Value);

        return new AnalyzeTransactionResult(
            transaction.Id.Value,
            (int)transaction.Status,
            transaction.Status.ToString(),
            evaluation.TotalRiskScore,
            evaluation.MatchedRules.Select(r => r.RuleName).ToList());
    }

    /// <summary>
    /// Applies the engine's recommended status to the transaction using domain behavior.
    /// Since the transaction was just created (Pending), the transition should always
    /// succeed. A failure indicates a programming error.
    /// </summary>
    private static Result ApplyRecommendedStatus(Transaction transaction, TransactionStatus recommendedStatus)
    {
        return recommendedStatus switch
        {
            TransactionStatus.Approved => transaction.Approve(),
            TransactionStatus.UnderReview => transaction.MarkForReview(),
            TransactionStatus.Rejected => transaction.Reject(),
            _ => Result.Failure($"Invalid status: {recommendedStatus}")
        };
    }
}
