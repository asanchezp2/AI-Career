using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Services;

/// <summary>
/// Represents the result of evaluating a transaction against fraud rules.
/// </summary>
public class FraudRuleEngineResult
{
    /// <summary>
    /// The total accumulated risk score from all applicable rules.
    /// </summary>
    public int TotalRiskScore { get; }

    /// <summary>
    /// The recommended transaction status based on the evaluation.
    /// </summary>
    public TransactionStatus RecommendedStatus { get; }

    /// <summary>
    /// The list of fraud rules that were matched by the transaction.
    /// </summary>
    public IReadOnlyCollection<FraudRule> MatchedRules { get; }

    /// <summary>
    /// Creates a new FraudRuleEngineResult instance.
    /// </summary>
    public FraudRuleEngineResult(
        int totalRiskScore,
        TransactionStatus recommendedStatus,
        IReadOnlyCollection<FraudRule> matchedRules)
    {
        TotalRiskScore = totalRiskScore;
        RecommendedStatus = recommendedStatus;
        MatchedRules = matchedRules;
    }
}
