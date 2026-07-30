using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Domain.Specifications.Transactions;

/// <summary>
/// Determines whether a Transaction was initiated by a blacklisted customer.
/// </summary>
public class BlacklistCustomerSpecification : ISpecification
{
    private readonly HashSet<CustomerId> _blacklistedCustomers;

    /// <summary>
    /// Creates a new BlacklistCustomerSpecification with the specified blacklisted customers.
    /// </summary>
    /// <param name="blacklistedCustomers">The collection of blacklisted customer IDs.</param>
    /// <exception cref="ArgumentNullException">Thrown when blacklistedCustomers is null.</exception>
    public BlacklistCustomerSpecification(IEnumerable<CustomerId> blacklistedCustomers)
    {
        Guard.AgainstNull(blacklistedCustomers, nameof(blacklistedCustomers));
        _blacklistedCustomers = new HashSet<CustomerId>(blacklistedCustomers);
    }

    /// <summary>
    /// Evaluates whether the transaction's customer is in the blacklist.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <returns>True when the customer is blacklisted; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public bool IsSatisfiedBy(Transaction transaction)
    {
        Guard.AgainstNull(transaction, nameof(transaction));
        return _blacklistedCustomers.Contains(transaction.CustomerId);
    }
}
