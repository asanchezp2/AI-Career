namespace FraudDetection.Domain.Enums;

/// <summary>
/// Represents the possible states of a financial transaction.
/// The real challenge defines exactly three states — there is no "under review"
/// state; a transaction is either waiting for evaluation, or has been approved/rejected.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction has been created and queued for asynchronous fraud evaluation.
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction passed the fraud evaluation and was approved.
    /// </summary>
    Approved,

    /// <summary>
    /// Transaction was rejected by the fraud evaluation.
    /// </summary>
    Rejected
}