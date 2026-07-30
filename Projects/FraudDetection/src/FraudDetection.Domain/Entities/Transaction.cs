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
    private Transaction()
    {
        Id = null!;
        CustomerId = null!;
        Amount = null!;
        RecentTransactionCount = 0;
        Metadata = new Dictionary<string, string>();
        CreatedAt = DateTime.UtcNow;
    }

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
    /// The ISO 3166-1 alpha-2 country code of the transaction origin.
    /// Optional — may be null if the origin country is unknown.
    /// </summary>
    public string? Country { get; private set; }

    /// <summary>
    /// Optional key-value metadata attached to the transaction.
    /// </summary>
    public Dictionary<string, string> Metadata { get; private set; }

    /// <summary>
    /// The date and time when this transaction was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The current status of this transaction.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>
    /// Number of recent transactions by this customer.
    /// Set by the application layer before fraud evaluation. Used for velocity rules.
    /// </summary>
    public int RecentTransactionCount { get; set; }

    /// <summary>
    /// Creates a new Transaction instance with Pending status.
    /// </summary>
    /// <param name="id">The unique transaction identifier.</param>
    /// <param name="customerId">The customer initiating the transaction.</param>
    /// <param name="amount">The monetary amount of the transaction.</param>
    /// <param name="timestamp">The date and time when the transaction occurred (UTC).</param>
    /// <param name="country">Optional ISO 3166-1 alpha-2 country code of the transaction origin.</param>
    /// <param name="metadata">Optional key-value metadata attached to the transaction.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when country is provided but is whitespace.</exception>
    public Transaction(
        TransactionId id,
        CustomerId customerId,
        Money amount,
        DateTime timestamp,
        string? country = null,
        Dictionary<string, string>? metadata = null)
    {
        Guard.AgainstNull(id, nameof(id));
        Guard.AgainstNull(customerId, nameof(customerId));
        Guard.AgainstNull(amount, nameof(amount));

        if (country is not null)
            Guard.AgainstNullOrWhiteSpace(country, nameof(country));

        Id = id;
        CustomerId = customerId;
        Amount = amount;
        Country = country;
        Metadata = metadata ?? new Dictionary<string, string>();
        CreatedAt = timestamp;
        Status = TransactionStatus.Pending;
    }

    /// <summary>
    /// Transitions the transaction to Approved status.
    /// Only allowed when the transaction is Pending.
    /// </summary>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    public Result Approve() => ChangeStatus(TransactionStatus.Approved);

    /// <summary>
    /// Transitions the transaction to Rejected status.
    /// Only allowed when the transaction is Pending.
    /// </summary>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    public Result Reject() => ChangeStatus(TransactionStatus.Rejected);

    /// <summary>
    /// Transitions the transaction to UnderReview status.
    /// Only allowed when the transaction is Pending.
    /// </summary>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    public Result MarkForReview() => ChangeStatus(TransactionStatus.UnderReview);

    /// <summary>
    /// Changes the transaction status if the current status is Pending.
    /// </summary>
    /// <param name="newStatus">The target status to transition to.</param>
    /// <returns>A Result indicating success or failure with an error message.</returns>
    private Result ChangeStatus(TransactionStatus newStatus)
    {
        if (Status != TransactionStatus.Pending)
            return Result.Failure(
                $"Only transactions in Pending status can change state. Current status: {Status}.");

        Status = newStatus;
        return Result.Success();
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
