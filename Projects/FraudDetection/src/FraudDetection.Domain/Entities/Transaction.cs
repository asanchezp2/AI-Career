using FraudDetection.Domain.Enums;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Domain.Entities;

/// <summary>
/// Represents a financial transaction within the fraud detection system.
/// </summary>
public class Transaction
{
    /// <summary>
    /// EF Core parameterless constructor (used for materialization only).
    /// </summary>
    private Transaction() { Id = null!; CustomerId = null!; Amount = null!; }

    /// <summary>
    /// The unique identifier of this transaction.
    /// </summary>
    public TransactionId Id { get; private set; }

    /// <summary>
    /// The customer who initiated the transaction.
    /// </summary>
    public CustomerId CustomerId { get; private set; }

    /// <summary>
    /// The monetary amount of this transaction.
    /// </summary>
    public Money Amount { get; private set; }

    /// <summary>
    /// The date and time when this transaction was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The current status of this transaction.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>
    /// Creates a new Transaction instance with Pending status.
    /// </summary>
    /// <param name="id">The unique transaction identifier.</param>
    /// <param name="customerId">The customer initiating the transaction.</param>
    /// <param name="amount">The monetary amount of the transaction.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public Transaction(TransactionId id, CustomerId customerId, Money amount)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentNullException.ThrowIfNull(amount);

        Id = id;
        CustomerId = customerId;
        Amount = amount;
        CreatedAt = DateTime.UtcNow;
        Status = TransactionStatus.Pending;
    }

    /// <summary>
    /// Transitions the transaction to Approved status.
    /// Only allowed when the transaction is Pending.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transaction is not Pending.</exception>
    public void Approve() => ChangeStatus(TransactionStatus.Approved);

    /// <summary>
    /// Transitions the transaction to Rejected status.
    /// Only allowed when the transaction is Pending.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transaction is not Pending.</exception>
    public void Reject() => ChangeStatus(TransactionStatus.Rejected);

    /// <summary>
    /// Transitions the transaction to UnderReview status.
    /// Only allowed when the transaction is Pending.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transaction is not Pending.</exception>
    public void MarkForReview() => ChangeStatus(TransactionStatus.UnderReview);

    /// <summary>
    /// Changes the transaction status if the current status is Pending.
    /// </summary>
    /// <param name="newStatus">The target status to transition to.</param>
    /// <exception cref="InvalidOperationException">Thrown when the transaction is not Pending.</exception>
    private void ChangeStatus(TransactionStatus newStatus)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException(
                $"Only transactions in Pending status can change state. Current status: {Status}.");

        Status = newStatus;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Transaction.
    /// Two transactions are equal if and only if they have the same TransactionId.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is Transaction other && Id.Equals(other.Id);

    /// <summary>
    /// Returns the hash code of this Transaction based on its TransactionId.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Determines whether two Transaction instances are equal.
    /// </summary>
    public static bool operator ==(Transaction? left, Transaction? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two Transaction instances are not equal.
    /// </summary>
    public static bool operator !=(Transaction? left, Transaction? right) =>
        !Equals(left, right);
}
