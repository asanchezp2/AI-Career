using FraudDetection.Domain.Enums;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Domain.Entities;

/// <summary>
/// Represents a financial transaction within the fraud detection system.
/// </summary>
public class Transaction
{
    /// <summary>
    /// The unique identifier of this transaction.
    /// </summary>
    public TransactionId Id { get; }

    /// <summary>
    /// The customer who initiated the transaction.
    /// </summary>
    public CustomerId CustomerId { get; }

    /// <summary>
    /// The monetary amount of this transaction.
    /// </summary>
    public Money Amount { get; }

    /// <summary>
    /// The date and time when this transaction was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// The current status of this transaction.
    /// </summary>
    public TransactionStatus Status { get; }

    /// <summary>
    /// Creates a new Transaction instance.
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
