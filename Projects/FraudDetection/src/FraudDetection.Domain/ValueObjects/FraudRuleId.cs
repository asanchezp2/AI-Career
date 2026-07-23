namespace FraudDetection.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier of a FraudRule.
/// </summary>
public record FraudRuleId
{
    /// <summary>
    /// The unique identifier value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new FraudRuleId with a new unique identifier.
    /// </summary>
    /// <returns>A new FraudRuleId instance.</returns>
    public static FraudRuleId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a FraudRuleId from an existing valid Guid.
    /// </summary>
    /// <param name="value">The Guid value to use.</param>
    /// <returns>A new FraudRuleId instance.</returns>
    /// <exception cref="ArgumentException">Thrown when Guid.Empty is provided.</exception>
    public static FraudRuleId From(Guid value) => new(value);

    /// <summary>
    /// Creates a new FraudRuleId instance.
    /// </summary>
    /// <param name="value">The underlying unique identifier.</param>
    /// <exception cref="ArgumentException">Thrown when Guid.Empty is provided.</exception>
    private FraudRuleId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A fraud rule identifier must contain a valid Guid.", nameof(value));

        Value = value;
    }
}
