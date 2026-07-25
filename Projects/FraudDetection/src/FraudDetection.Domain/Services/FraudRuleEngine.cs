using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Specifications;

namespace FraudDetection.Domain.Services;

/// <summary>
/// Domain service that evaluates a transaction against configured fraud rules
/// using the Specification Pattern to determine applicable rules and calculate risk.
/// </summary>
public class FraudRuleEngine
{
    /// <summary>
    /// Evaluates the specified transaction against the given fraud rules and specifications.
    /// Only enabled rules with a matching specification that is satisfied contribute to the risk score.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <param name="fraudRules">The collection of fraud rules to evaluate against.</param>
    /// <param name="specifications">
    /// A dictionary mapping rule names to their specifications.
    /// A rule without a matching entry is skipped.
    /// </param>
    /// <returns>The evaluation result with total risk score and recommended status.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public FraudRuleEngineResult Evaluate(
        Transaction transaction,
        IEnumerable<FraudRule> fraudRules,
        IReadOnlyDictionary<string, ISpecification> specifications)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(fraudRules);
        ArgumentNullException.ThrowIfNull(specifications);

        var matchedRules = new List<FraudRule>();
        var totalRiskScore = 0;

        foreach (var rule in fraudRules)
        {
            if (!rule.IsEnabled)
                continue;

            if (!specifications.TryGetValue(rule.RuleName, out var specification))
                continue;

            if (specification.IsSatisfiedBy(transaction))
            {
                matchedRules.Add(rule);
                totalRiskScore += rule.RiskScore;
            }
        }

        var recommendedStatus = totalRiskScore > 0
            ? TransactionStatus.UnderReview
            : TransactionStatus.Approved;

        return new FraudRuleEngineResult(
            totalRiskScore,
            recommendedStatus,
            matchedRules.AsReadOnly());
    }
}
