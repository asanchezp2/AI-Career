using FraudDetection.Domain.Enums;

namespace FraudDetection.Domain.Entities;

/// <summary>
/// Represents a financial transaction within the fraud detection system.
///
/// A transaction is created as <see cref="TransactionStatus.Pending"/> by the API and
/// is later evaluated asynchronously (via Kafka) by the anti-fraud worker, which
/// transitions it to <see cref="TransactionStatus.Approved"/> or
/// <see cref="TransactionStatus.Rejected"/>. The transition rules are the only
/// invariants this entity enforces — see <see cref="Approve"/> and <see cref="Reject"/>.
/// </summary>
public class Transaction
{
    /// <summary>
    /// EF Core parameterless constructor (used for materialization only).
    /// </summary>
    private Transaction()
    {
    }

    /// <summary>
    /// The unique identifier of this transaction, as exposed by the API contract
    /// (<c>transactionExternalId</c>). Server-generated at creation time — clients
    /// never supply it.
    /// </summary>
    public Guid TransactionExternalId { get; private set; }

    /// <summary>
    /// The account that funds the transaction (external system identifier).
    /// </summary>
    public Guid SourceAccountId { get; private set; }

    /// <summary>
    /// The account that receives the transaction (external system identifier).
    /// </summary>
    public Guid TargetAccountId { get; private set; }

    /// <summary>
    /// The transfer type identifier, as supplied by the client (must be &gt; 0).
    /// </summary>
    public int TransferTypeId { get; private set; }

    /// <summary>
    /// The monetary value of the transaction (must be &gt; 0).
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>
    /// The date and time when this transaction was created (UTC).
    /// Server-generated at creation time; used as the day boundary for the
    /// daily-accumulated fraud rule (see ADR-057).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The current status of this transaction.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>
    /// Which fraud rule caused the rejection. Only set when
    /// <see cref="Status"/> is <see cref="TransactionStatus.Rejected"/>;
    /// null otherwise. Acts as the decision audit trail (see ADR-056).
    /// </summary>
    public RejectionReason? RejectionReason { get; private set; }

    /// <summary>
    /// Creates a new Transaction instance with Pending status.
    /// </summary>
    /// <param name="transactionExternalId">The unique transaction identifier (server-generated).</param>
    /// <param name="sourceAccountId">The funding account identifier (cannot be Guid.Empty).</param>
    /// <param name="targetAccountId">The receiving account identifier (cannot be Guid.Empty).</param>
    /// <param name="transferTypeId">The transfer type identifier (must be &gt; 0).</param>
    /// <param name="value">The transaction value (must be &gt; 0).</param>
    /// <param name="createdAt">
    /// Optional creation timestamp (UTC). Defaults to <see cref="DateTime.UtcNow"/> —
    /// provided only as a testability hook; the client never supplies it.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when an identifier is Guid.Empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when transferTypeId is not positive or value is not positive.</exception>
    public Transaction(
        Guid transactionExternalId,
        Guid sourceAccountId,
        Guid targetAccountId,
        int transferTypeId,
        decimal value,
        DateTime? createdAt = null)
    {
        Guard.AgainstEmptyGuid(transactionExternalId, nameof(transactionExternalId));
        Guard.AgainstEmptyGuid(sourceAccountId, nameof(sourceAccountId));
        Guard.AgainstEmptyGuid(targetAccountId, nameof(targetAccountId));
        Guard.AgainstOutOfRange(transferTypeId, 1, int.MaxValue, nameof(transferTypeId));
        Guard.AgainstNonPositive(value, nameof(value));

        TransactionExternalId = transactionExternalId;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        TransferTypeId = transferTypeId;
        Value = value;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        Status = TransactionStatus.Pending;
        RejectionReason = null;
    }

    /// <summary>
    /// Transitions the transaction to Approved status.
    /// Only allowed when the transaction is Pending; any rejection reason is cleared.
    /// </summary>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    public Result Approve()
    {
        if (Status != TransactionStatus.Pending)
            return Result.Failure(
                $"Only transactions in Pending status can change state. Current status: {Status}.");

        Status = TransactionStatus.Approved;
        RejectionReason = null;
        return Result.Success();
    }

    /// <summary>
    /// Transitions the transaction to Rejected status.
    /// Only allowed when the transaction is Pending, and a reason is mandatory —
    /// a rejection without a documented cause would break the audit trail.
    /// </summary>
    /// <param name="reason">The fraud rule that caused the rejection.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    public Result Reject(RejectionReason reason)
    {
        Guard.AgainstUndefinedEnum(reason, nameof(reason));

        if (Status != TransactionStatus.Pending)
            return Result.Failure(
                $"Only transactions in Pending status can change state. Current status: {Status}.");

        Status = TransactionStatus.Rejected;
        RejectionReason = reason;
        return Result.Success();
    }
}