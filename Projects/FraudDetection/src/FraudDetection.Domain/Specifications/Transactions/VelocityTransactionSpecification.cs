using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications.Transactions;

/// <summary>
/// Determines whether a Transaction exceeds the maximum allowed transaction count
/// within a specified time window (velocity check).
/// 
/// The RecentTransactionCount must be set by the application layer before evaluation.
/// </summary>
public class VelocityTransactionSpecification : ISpecification
{
    private readonly int _maxTransactionCount;
    private readonly TimeSpan _timeWindow;

    /// <summary>
    /// Creates a new VelocityTransactionSpecification.
    /// </summary>
    /// <param name="maxTransactionCount">Maximum allowed transactions in the time window (minimum 1).</param>
    /// <param name="timeWindow">Time window for velocity checking.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxTransactionCount is less than 1.</exception>
    public VelocityTransactionSpecification(int maxTransactionCount, TimeSpan timeWindow)
    {
        Guard.AgainstOutOfRange(maxTransactionCount, 1, int.MaxValue, nameof(maxTransactionCount));
        _maxTransactionCount = maxTransactionCount;
        _timeWindow = timeWindow;
    }

    /// <summary>
    /// Evaluates whether the transaction's customer exceeds the velocity threshold.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <returns>True when the recent transaction count is at or above the maximum; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public bool IsSatisfiedBy(Transaction transaction)
    {
        Guard.AgainstNull(transaction, nameof(transaction));
        return transaction.RecentTransactionCount >= _maxTransactionCount;
    }

    /// <summary>
    /// The maximum number of transactions allowed within the time window.
    /// </summary>
    public int MaxTransactionCount => _maxTransactionCount;

    /// <summary>
    /// The time window for velocity checking.
    /// </summary>
    public TimeSpan TimeWindow => _timeWindow;
}
