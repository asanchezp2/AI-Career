namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Represents a command to analyze a transaction for fraud.
/// </summary>
public class AnalyzeTransactionCommand
{
    /// <summary>
    /// The unique identifier of the transaction.
    /// </summary>
    public Guid TransactionId { get; init; }

    /// <summary>
    /// The customer initiating the transaction.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// The monetary amount of the transaction.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// The ISO-4217 currency code (3 characters, uppercase).
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Optional ISO 3166-1 alpha-2 country code of the transaction origin.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Optional key-value metadata attached to the transaction.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// The date and time when the transaction occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; }
}
