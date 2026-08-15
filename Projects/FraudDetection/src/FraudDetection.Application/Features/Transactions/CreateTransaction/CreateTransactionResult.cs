namespace FraudDetection.Application.Features.Transactions.CreateTransaction;

/// <summary>
/// The result of a CreateTransaction command execution — the response body of
/// the API's POST endpoint. Matches the challenge's Resource 1 response
/// (transactionExternalId + createdAt) extended with the status field, which
/// is always "pending" at creation time (evaluation is asynchronous).
/// </summary>
public sealed record CreateTransactionResult(
    Guid TransactionExternalId,
    DateTime CreatedAt,
    string Status);