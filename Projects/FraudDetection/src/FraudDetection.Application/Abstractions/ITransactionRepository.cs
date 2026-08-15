using FraudDetection.Domain.Entities;

namespace FraudDetection.Application.Abstractions;

/// <summary>
/// Repository interface for transaction persistence.
/// Abstraction in the Application layer — implementation in Infrastructure.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Adds a transaction to the persistent store (persisted as Pending).
    /// </summary>
    /// <param name="transaction">The transaction to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by its external identifier.
    /// Returns null when no transaction with the given ID exists.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid transactionExternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the accumulated <see cref="Transaction.Value"/> for the given source
    /// account within the specified UTC day [startOfDay, nextDayStart).
    /// The sum INCLUDES the transaction being evaluated when it is part of that day —
    /// it is already persisted as Pending by the time the evaluation runs (see ADR-057).
    /// </summary>
    /// <param name="sourceAccountId">The source account to aggregate.</param>
    /// <param name="day">The UTC day boundary (inclusive start, exclusive end).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The accumulated value; zero when no transactions exist for that day.</returns>
    Task<decimal> GetDailyAccumulatedAsync(Guid sourceAccountId, DateOnly day, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing transaction (e.g. the status transition
    /// applied by the anti-fraud evaluation).
    /// </summary>
    /// <param name="transaction">The transaction to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}