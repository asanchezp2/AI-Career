namespace FraudDetection.Application.Features.Transactions.GetTransaction;

/// <summary>
/// Response DTO for retrieving a persisted transaction.
/// Matches the challenge's Resource 2 contract base (transactionExternalId +
/// createdAt) extended with the status field and — when rejected — the
/// rejection reason (decision audit trail).
/// </summary>
public sealed record GetTransactionResponse(
    Guid TransactionExternalId,
    DateTime CreatedAt,
    string Status,
    string? RejectionReason);