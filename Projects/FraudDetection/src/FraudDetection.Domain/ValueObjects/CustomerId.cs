namespace FraudDetection.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier of a Customer.
/// </summary>
public record CustomerId
{
    /// <summary>
    /// The unique identifier value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new CustomerId with a new unique identifier.
    /// </summary>
    /// <returns>A new CustomerId instance.</returns>
    public static CustomerId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a CustomerId from an existing valid Guid.
    /// </summary>
    /// <param name="value">The Guid value to use.</param>
    /// <returns>A new CustomerId instance.</returns>
    /// <exception cref="ArgumentException">Thrown when Guid.Empty is provided.</exception>
    public static CustomerId From(Guid value) => new(value);

    /// <summary>
    /// Creates a new CustomerId instance.
    /// </summary>
    /// <param name="value">The underlying unique identifier.</param>
    /// <exception cref="ArgumentException">Thrown when Guid.Empty is provided.</exception>
    private CustomerId(Guid value)
    {
        Guard.AgainstEmptyGuid(value, nameof(value));
        Value = value;
    }
}
