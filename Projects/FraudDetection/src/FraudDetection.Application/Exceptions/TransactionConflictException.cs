namespace FraudDetection.Application.Exceptions;

/// <summary>
/// Signals that an attempt to persist a transaction failed because a transaction
/// with the same ID already exists (unique primary key violation).
/// Thrown by the Infrastructure repository on a duplicate insert and caught by
/// the AnalyzeTransactionHandler, which re-reads the persisted row and responds
/// with an idempotent replay or a conflict. Never escapes to the HTTP layer.
/// </summary>
public sealed class TransactionConflictException : Exception
{
    /// <summary>
    /// Creates a new TransactionConflictException for the given transaction ID.
    /// </summary>
    /// <param name="transactionId">The ID of the transaction that already exists.</param>
    /// <param name="innerException">The underlying database exception.</param>
    public TransactionConflictException(Guid transactionId, Exception innerException)
        : base($"A transaction with ID '{transactionId}' already exists.", innerException)
    {
        TransactionId = transactionId;
    }

    /// <summary>
    /// The ID of the transaction that already exists.
    /// </summary>
    public Guid TransactionId { get; }
}
