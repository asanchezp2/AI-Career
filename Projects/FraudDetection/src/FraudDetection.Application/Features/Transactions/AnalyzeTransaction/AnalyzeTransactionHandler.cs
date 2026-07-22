using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Handles the AnalyzeTransaction command.
/// Converts the command into domain objects and executes the analysis flow.
/// </summary>
public sealed class AnalyzeTransactionHandler
{
    /// <summary>
    /// Executes the AnalyzeTransaction command.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <returns>The result of the analysis.</returns>
    public AnalyzeTransactionResult Handle(AnalyzeTransactionCommand command)
    {
        var transactionId = TransactionId.From(command.TransactionId);
        var customerId = CustomerId.From(command.CustomerId);
        var amount = new Money(command.Amount, command.Currency);

        var transaction = new Transaction(transactionId, customerId, amount);

        return new AnalyzeTransactionResult(
            transaction.Id.Value,
            transaction.Status);
    }
}
