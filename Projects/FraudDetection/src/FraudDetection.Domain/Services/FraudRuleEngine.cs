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
        Guard.AgainstNull(transaction, nameof(transaction));
        Guard.AgainstNull(fraudRules, nameof(fraudRules));
        Guard.AgainstNull(specifications, nameof(specifications));

        var matchedRules = new List<FraudRule>();
        var totalRiskScore = 0;

        foreach (var rule in fraudRules.Where(r => r.IsEnabled))
        {
            if (!specifications.TryGetValue(rule.RuleName, out var specification))
                continue;

            if (specification.IsSatisfiedBy(transaction))
            {
                matchedRules.Add(rule);
                totalRiskScore += rule.RiskScore;
            }
        }

        // Determine recommended status based on matched rule actions.
        // Rejection rules take precedence over review rules.
        var recommendedStatus = matchedRules.Any(r => r.Action == FraudRuleAction.Reject)
            ? TransactionStatus.Rejected
            : totalRiskScore > 0
                ? TransactionStatus.UnderReview
                : TransactionStatus.Approved;

        return new FraudRuleEngineResult(
            totalRiskScore,
            recommendedStatus,
            matchedRules.AsReadOnly());
    }
}
