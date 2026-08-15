using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications.Transactions;

/// <summary>
/// Rejects a transaction when the accumulated value of the same source account
/// in the current day exceeds the daily limit.
///
/// The daily limit is a FIXED business rule of the real challenge (&gt; 20000)
/// and is therefore a constant of the specification itself.
///
/// Following the existing velocity pattern (pre-computed value passed into the
/// specification), the accumulated amount is computed by the repository layer
/// and supplied via the constructor — the specification itself never queries.
/// IMPORTANT: the accumulated sum INCLUDES the transaction being evaluated,
/// because the transaction is already persisted as Pending when the evaluation
/// runs (see ADR-057 for the semantics and the UTC day boundary).
/// </summary>
public class DailyAccumulatedSpecification : ISpecification
{
    /// <summary>
    /// The daily accumulated limit for a single source account defined by the
    /// challenge (strictly greater).
    /// </summary>
    public const decimal DailyAccumulatedLimit = 20000m;

    private readonly decimal _accumulatedToday;

    /// <summary>
    /// Creates a new DailyAccumulatedSpecification with the day's accumulated
    /// value for the transaction's source account.
    /// </summary>
    /// <param name="accumulatedToday">The accumulated value of the source account for the current day (including the transaction being evaluated).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when accumulatedToday is negative.</exception>
    public DailyAccumulatedSpecification(decimal accumulatedToday)
    {
        Guard.AgainstNegative(accumulatedToday, nameof(accumulatedToday));
        _accumulatedToday = accumulatedToday;
    }

    /// <inheritdoc />
    public bool IsSatisfiedBy(Transaction transaction)
    {
        Guard.AgainstNull(transaction, nameof(transaction));
        return _accumulatedToday > DailyAccumulatedLimit;
    }
}