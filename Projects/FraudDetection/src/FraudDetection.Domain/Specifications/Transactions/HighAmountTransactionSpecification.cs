using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications.Transactions;

    /// <summary>
    /// Determines whether a Transaction has an amount greater than a configurable threshold.
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
        Guard.AgainstNegative(threshold, nameof(threshold));
        _threshold = threshold;
    }

    /// <summary>
    /// Evaluates whether the transaction amount is greater than the configured threshold.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <returns>True when the transaction amount is > threshold; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public bool IsSatisfiedBy(Transaction transaction)
    {
        Guard.AgainstNull(transaction, nameof(transaction));

        var transactionAmount = transaction.Amount.Amount;
        return transactionAmount > _threshold;
    }
}
