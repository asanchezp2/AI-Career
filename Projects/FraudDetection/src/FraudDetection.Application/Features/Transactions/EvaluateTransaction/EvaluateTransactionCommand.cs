namespace FraudDetection.Application.Features.Transactions.EvaluateTransaction;

/// <summary>
/// Represents a command to evaluate a created transaction against the fraud rules.
/// Consumed by the anti-fraud worker after receiving a TransactionCreated event.
/// The day boundary is derived from the persisted transaction's CreatedAt — it is
/// not part of the command.
/// </summary>
public class EvaluateTransactionCommand
{
    /// <summary>
    /// The external identifier of the transaction to evaluate.
    /// </summary>
    public Guid TransactionExternalId { get; init; }
}