using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Application.Abstractions;

/// <summary>
/// Repository interface for transaction persistence.
/// Abstraction in the Application layer — implementation in Infrastructure.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Adds a transaction to the persistent store.
    /// </summary>
    /// <param name="transaction">The transaction to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by its unique identifier.
    /// Returns null when no transaction with the given ID exists.
    /// </summary>
    Task<Transaction?> GetByIdAsync(TransactionId transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts transactions for the given customer since the specified date/time.
    /// Used for velocity detection rules.
    /// </summary>
    Task<int> GetTransactionCountSinceAsync(CustomerId customerId, DateTime since, CancellationToken cancellationToken = default);
}
