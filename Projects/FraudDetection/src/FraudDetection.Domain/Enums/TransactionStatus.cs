namespace FraudDetection.Domain.Enums;

/// <summary>
/// Represents the possible states of a financial transaction.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction has been created and is awaiting processing.
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// Transaction has been rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Transaction is under manual or automated review.
    /// </summary>
    UnderReview
}
