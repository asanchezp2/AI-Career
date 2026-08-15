using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;

namespace FraudDetection.Domain.Services;

/// <summary>
/// Represents the result of evaluating a transaction against the fraud rules.
/// The decision model is binary: the transaction is either Approved or Rejected,
/// and a rejection always carries the reason (which rule matched). There is no
/// risk scoring — see ADR-056.
/// </summary>
public sealed record FraudRuleEngineResult(
    TransactionStatus RecommendedStatus,
    RejectionReason? RejectionReason)
{
    /// <summary>
    /// Builds an approved result.
    /// </summary>
    public static FraudRuleEngineResult Approved() =>
        new(TransactionStatus.Approved, null);

    /// <summary>
    /// Builds a rejected result with the rule that caused the rejection.
    /// </summary>
    public static FraudRuleEngineResult Rejected(RejectionReason reason) =>
        new(TransactionStatus.Rejected, reason);
}