using FraudDetection.Domain.Enums;

namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Represents the result of an AnalyzeTransaction command execution.
/// </summary>
public class AnalyzeTransactionResult
{
    /// <summary>
    /// The unique identifier of the analyzed transaction.
    /// </summary>
    public Guid TransactionId { get; }

    /// <summary>
    /// The status of the transaction after analysis.
    /// </summary>
    public TransactionStatus Status { get; }

    /// <summary>
    /// Creates a new AnalyzeTransactionResult instance.
    /// </summary>
    public AnalyzeTransactionResult(Guid transactionId, TransactionStatus status)
    {
        TransactionId = transactionId;
        Status = status;
    }
}
