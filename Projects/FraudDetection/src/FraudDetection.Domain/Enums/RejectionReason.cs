namespace FraudDetection.Domain.Enums;

/// <summary>
/// Identifies which fraud rule caused a transaction to be rejected.
/// Acts as an audit log on the transaction — it records WHY the transaction
/// was rejected without any risk scoring (binary decision model, see
/// ADR-056). Stored as a nullable lowercase string in the database; it is
/// only ever set when <see cref="TransactionStatus.Rejected"/>.
/// </summary>
public enum RejectionReason
{
    /// <summary>
    /// The transaction's value exceeded the single-transaction limit
    /// (> 2000, see HighValueSpecification).
    /// </summary>
    HighValue,

    /// <summary>
    /// The accumulated value for the same source account in the current
    /// day exceeded the daily limit (> 20000, see DailyAccumulatedSpecification).
    /// </summary>
    DailyAccumulated
}