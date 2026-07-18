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
}
