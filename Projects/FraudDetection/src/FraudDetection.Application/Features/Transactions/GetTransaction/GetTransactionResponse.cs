namespace FraudDetection.Application.Features.Transactions.GetTransaction;

/// <summary>
/// Response DTO for retrieving a persisted transaction.
/// </summary>
public sealed record GetTransactionResponse(
    Guid TransactionId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string? Country,
    string Status,
    DateTime CreatedAt,
    IReadOnlyDictionary<string, string> Metadata);
