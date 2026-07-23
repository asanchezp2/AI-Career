using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications.Transactions;

/// <summary>
/// Determines whether a Transaction has an amount greater than or equal to a configurable threshold.
/// </summary>
public class HighAmountTransactionSpecification : ISpecification
{
    private readonly decimal _threshold;

    /// <summary>
    /// Creates a new HighAmountTransactionSpecification with the specified threshold.
    /// </summary>
    /// <param name="threshold">The minimum amount to consider a transaction as high-amount (non-negative).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative.</exception>
    public HighAmountTransactionSpecification(decimal threshold)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold cannot be negative.");

        _threshold = threshold;
    }

    /// <summary>
    /// Evaluates whether the transaction amount is greater than or equal to the configured threshold.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <returns>True when the transaction amount is >= threshold; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public bool IsSatisfiedBy(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var transactionAmount = transaction.Amount.Amount;
        return transactionAmount >= _threshold;
    }
}
