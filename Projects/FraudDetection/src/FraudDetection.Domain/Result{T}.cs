namespace FraudDetection.Domain;

/// <summary>
/// Represents the outcome of a domain operation that returns a value.
/// Provides a type-safe way to represent success with a value or failure with an error.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the value returned by the operation if it succeeded; otherwise, default.
    /// </summary>
    public T? Value { get; }

    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(string error) : base(false, error)
    {
        Value = default;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new Result<T>(value);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static new Result<T> Failure(string error) => new Result<T>(error);
}
