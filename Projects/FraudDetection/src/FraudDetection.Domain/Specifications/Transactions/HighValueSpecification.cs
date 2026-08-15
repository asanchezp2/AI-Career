using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications.Transactions;

/// <summary>
/// Rejects any transaction whose value exceeds the single-transaction limit.
///
/// The threshold is a FIXED business rule of the real challenge (value &gt; 2000)
/// and is therefore a constant of the specification itself — there is no
/// configurable rules table (see ADR-051). The comparison is strictly greater
/// than, matching the challenge wording: a transaction with value exactly
/// 2000 is accepted.
/// </summary>
public class HighValueSpecification : ISpecification
{
    /// <summary>
    /// The single-transaction value limit defined by the challenge (strictly greater).
    /// </summary>
    public const decimal HighValueLimit = 2000m;

    /// <inheritdoc />
    public bool IsSatisfiedBy(Transaction transaction)
    {
        Guard.AgainstNull(transaction, nameof(transaction));
        return transaction.Value > HighValueLimit;
    }
}