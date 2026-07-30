using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications.Transactions;

/// <summary>
/// Determines whether a Transaction originates from a high-risk country
/// by comparing the transaction's Country field against a configured list of
/// high-risk country codes.
/// 
/// NOTE: If the transaction has no Country set (null), this specification
/// returns false — it will not match unknown origins.
/// </summary>
public class HighRiskCountrySpecification : ISpecification
{
    private readonly HashSet<string> _highRiskCountryCodes;

    /// <summary>
    /// Creates a new HighRiskCountrySpecification with the specified high-risk country codes.
    /// </summary>
    /// <param name="highRiskCountryCodes">ISO 3166-1 alpha-2 country codes for high-risk regions.</param>
    /// <exception cref="ArgumentNullException">Thrown when highRiskCountryCodes is null.</exception>
    public HighRiskCountrySpecification(IEnumerable<string> highRiskCountryCodes)
    {
        Guard.AgainstNull(highRiskCountryCodes, nameof(highRiskCountryCodes));
        _highRiskCountryCodes = new HashSet<string>(
            highRiskCountryCodes.Select(c => c.ToUpperInvariant()));
    }

    /// <summary>
    /// Evaluates whether the transaction's country is in the high-risk list.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <returns>True when the transaction country is high-risk; false if the country is null or not in the list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public bool IsSatisfiedBy(Transaction transaction)
    {
        Guard.AgainstNull(transaction, nameof(transaction));
        return transaction.Country is not null
            && _highRiskCountryCodes.Contains(transaction.Country);
    }
}
