using FraudDetection.Application.Abstractions;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Handles the AnalyzeTransaction command.
/// Creates a transaction, evaluates it against fraud rules,
/// and applies the recommended status.
/// </summary>
public sealed class AnalyzeTransactionHandler
{
    private readonly FraudRuleEngine _engine;
    private readonly IFraudRuleProvider _ruleProvider;

    /// <summary>
    /// Creates a new AnalyzeTransactionHandler with the required dependencies.
    /// </summary>
    public AnalyzeTransactionHandler(FraudRuleEngine engine, IFraudRuleProvider ruleProvider)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(ruleProvider);

        _engine = engine;
        _ruleProvider = ruleProvider;
    }

    /// <summary>
    /// Executes the AnalyzeTransaction command.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <returns>The result of the analysis.</returns>
    public AnalyzeTransactionResult Handle(AnalyzeTransactionCommand command)
    {
        var transactionId = TransactionId.From(command.TransactionId);
        var customerId = CustomerId.From(command.CustomerId);
        var amount = new Money(command.Amount, command.Currency);

        var transaction = new Transaction(transactionId, customerId, amount);

        var rules = _ruleProvider.GetAllRules();
        var specifications = _ruleProvider.GetSpecifications();
        var evaluation = _engine.Evaluate(transaction, rules, specifications);

        ApplyRecommendedStatus(transaction, evaluation.RecommendedStatus);

        return new AnalyzeTransactionResult(
            transaction.Id.Value,
            transaction.Status,
            evaluation.TotalRiskScore);
    }

    /// <summary>
    /// Applies the engine's recommended status to the transaction using domain behavior.
    /// </summary>
    private static void ApplyRecommendedStatus(Transaction transaction, TransactionStatus recommendedStatus)
    {
        switch (recommendedStatus)
        {
            case TransactionStatus.Approved:
                transaction.Approve();
                break;

            case TransactionStatus.UnderReview:
                transaction.MarkForReview();
                break;

            case TransactionStatus.Rejected:
                transaction.Reject();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported recommended status: {recommendedStatus}");
        }
    }
}
