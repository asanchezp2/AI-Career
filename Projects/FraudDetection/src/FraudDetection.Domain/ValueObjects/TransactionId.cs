namespace FraudDetection.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier of a Transaction.
/// </summary>
public record TransactionId
{
    /// <summary>
    /// The unique identifier value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new TransactionId with a new unique identifier.
    /// </summary>
    /// <returns>A new TransactionId instance.</returns>
    public static TransactionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a TransactionId from an existing valid Guid..
    /// </summary>
    /// <param name="value">The Guid value to use.</param>
    /// <returns>A new TransactionId instance.</returns>
    /// <exception cref="ArgumentException">Thrown when Guid.Empty is provided.</exception>
    public static TransactionId From(Guid value) => new(value);

    /// <summary>
    /// Creates a new TransactionId instance.
    /// </summary>
    /// <param name="value">The underlying unique identifier.</param>
    /// <exception cref="ArgumentException">Thrown when Guid.Empty is provided.</exception>
    private TransactionId(Guid value)
    {
        Guard.AgainstEmptyGuid(value, nameof(value));
        Value = value;
    }
}
