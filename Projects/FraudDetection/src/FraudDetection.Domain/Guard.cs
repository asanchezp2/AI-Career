namespace FraudDetection.Domain;

/// <summary>
/// Provides defensive validation guards for domain entities and value objects.
/// Replaces repetitive null/range/empty checks with single-call preconditions.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Guards against null reference-type values.
    /// </summary>
    public static void AgainstNull<T>(T value, string parameterName) where T : class
        => ArgumentNullException.ThrowIfNull(value, parameterName);

    /// <summary>
    /// Guards against null nullable value-type values.
    /// </summary>
    public static void AgainstNull<T>(T? value, string parameterName) where T : struct
    {
        if (!value.HasValue)
            throw new ArgumentNullException(parameterName);
    }

    /// <summary>
    /// Guards against strings that are null, empty, or consist only of whitespace.
    /// </summary>
    public static void AgainstNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null, empty, or whitespace.", parameterName);
    }

    /// <summary>
    /// Guards against integer values outside the specified inclusive range.
    /// </summary>
    public static void AgainstOutOfRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be between {min} and {max}.");
    }

    /// <summary>
    /// Guards against decimal values outside the specified inclusive range.
    /// </summary>
    public static void AgainstOutOfRange(decimal value, decimal min, decimal max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be between {min} and {max}.");
    }

    /// <summary>
    /// Guards against empty GUIDs.
    /// </summary>
    public static void AgainstEmptyGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{parameterName} cannot be an empty GUID.", parameterName);
    }

    /// <summary>
    /// Guards against negative decimal values. Zero and positive values are allowed.
    /// </summary>
    public static void AgainstNegative(decimal value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be negative.");
    }
}
