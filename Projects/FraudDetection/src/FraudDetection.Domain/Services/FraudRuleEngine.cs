using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Specifications.Transactions;

namespace FraudDetection.Domain.Services;

/// <summary>
/// Domain service that evaluates a transaction against the fixed fraud rules
/// using the Specification Pattern.
///
/// The real challenge defines exactly two rejection criteria (see ADR-051):
/// 1. HighValue — transaction value &gt; 2000.
/// 2. DailyAccumulated — day's accumulated value of the same source account &gt; 20000.
///
/// Both rules REJECT — there are no review/flag rules anymore — so the engine
/// is fully deterministic: run the rules in fixed precedence order (HighValue
/// first), and the first satisfied rule determines the rejection reason. If
/// neither rule matches, the transaction is approved.
///
/// The daily accumulated amount is computed by the repository layer and passed
/// in: the engine stays a pure domain service with no I/O.
/// </summary>
public class FraudRuleEngine
{
    /// <summary>
    /// Evaluates the specified transaction against the two fixed fraud rules.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <param name="dailyAccumulatedAmount">
    /// The accumulated value of the transaction's source account for the current day,
    /// INCLUDING the transaction being evaluated (it is already persisted as Pending).
    /// </param>
    /// <returns>The evaluation result: Approved, or Rejected with the matching reason.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public FraudRuleEngineResult Evaluate(Transaction transaction, decimal dailyAccumulatedAmount)
    {
        Guard.AgainstNull(transaction, nameof(transaction));

        // Precedence: HighValue is checked first — a transaction that exceeds the
        // single-transaction limit is rejected for that reason even when it would
        // also breach the daily accumulated limit.
        if (new HighValueSpecification().IsSatisfiedBy(transaction))
            return FraudRuleEngineResult.Rejected(RejectionReason.HighValue);

        if (new DailyAccumulatedSpecification(dailyAccumulatedAmount).IsSatisfiedBy(transaction))
            return FraudRuleEngineResult.Rejected(RejectionReason.DailyAccumulated);

        return FraudRuleEngineResult.Approved();
    }
}