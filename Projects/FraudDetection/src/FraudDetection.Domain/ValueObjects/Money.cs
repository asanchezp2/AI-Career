namespace FraudDetection.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// </summary>
public record Money
{
    /// <summary>
    /// The monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// The ISO-4217 currency code (3 characters, uppercase).
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Creates a new Money instance.
    /// </summary>
    /// <param name="amount">The monetary amount (cannot be negative).</param>
    /// <param name="currency">The ISO-4217 currency code (3 characters, uppercase).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when currency is invalid.</exception>
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be exactly 3 characters.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}
