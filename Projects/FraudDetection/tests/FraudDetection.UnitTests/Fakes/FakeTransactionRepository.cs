using FraudDetection.Application.Abstractions;
using FraudDetection.Domain.Entities;

namespace FraudDetection.UnitTests.Fakes;

/// <summary>
/// In-memory ITransactionRepository for Application-layer unit tests. Stores the
/// added transactions, serves preconfigured values for the daily-accumulated
/// query, and records every call in an optional shared operation log so tests
/// can assert invocation order (e.g. persist before publish).
/// </summary>
public sealed class FakeTransactionRepository : ITransactionRepository
{
    private readonly Dictionary<Guid, Transaction> _store = new();
    private readonly List<string> _operationLog;

    public FakeTransactionRepository(List<string>? operationLog = null)
    {
        _operationLog = operationLog ?? new List<string>();
    }

    /// <summary>
    /// The value returned by GetDailyAccumulatedAsync (models the repository's
    /// accumulation INCLUDING the transaction being evaluated, per ADR-057).
    /// </summary>
    public decimal DailyAccumulated { get; set; }

    /// <summary>
    /// Optional callback invoked by GetDailyAccumulatedAsync, used to capture
    /// the arguments (e.g. the day boundary) the handler passes through.
    /// </summary>
    public Action<Guid, DateOnly>? OnGetDailyAccumulated { get; set; }

    /// <summary>
    /// When set, AddAsync throws it instead of persisting (simulates a failure).
    /// </summary>
    public Exception? AddException { get; set; }

    public IReadOnlyList<string> OperationLog => _operationLog;

    public IReadOnlyCollection<Transaction> Stored => _store.Values;

    public void Seed(Transaction transaction) => _store[transaction.TransactionExternalId] = transaction;

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _operationLog.Add(nameof(AddAsync));

        if (AddException is not null)
            throw AddException;

        _store[transaction.TransactionExternalId] = transaction;
        return Task.CompletedTask;
    }

    public Task<Transaction?> GetByIdAsync(Guid transactionExternalId, CancellationToken cancellationToken = default)
    {
        _operationLog.Add(nameof(GetByIdAsync));
        _store.TryGetValue(transactionExternalId, out var transaction);
        return Task.FromResult(transaction);
    }

    public Task<decimal> GetDailyAccumulatedAsync(Guid sourceAccountId, DateOnly day, CancellationToken cancellationToken = default)
    {
        _operationLog.Add(nameof(GetDailyAccumulatedAsync));
        OnGetDailyAccumulated?.Invoke(sourceAccountId, day);
        return Task.FromResult(DailyAccumulated);
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _operationLog.Add(nameof(UpdateAsync));
        _store[transaction.TransactionExternalId] = transaction;
        return Task.CompletedTask;
    }
}