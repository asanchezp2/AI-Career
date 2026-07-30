using FraudDetection.Application.Abstractions;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the ITransactionRepository.
/// Provides transaction persistence and querying against the FraudDetection database.
/// </summary>
public sealed class EfTransactionRepository : ITransactionRepository
{
    private readonly FraudDetectionDbContext _context;

    /// <summary>
    /// Creates a new EfTransactionRepository with the given database context.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public EfTransactionRepository(FraudDetectionDbContext context)
    {
        Guard.AgainstNull(context, nameof(context));
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetTransactionCountSinceAsync(CustomerId customerId, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId && t.CreatedAt >= since)
            .CountAsync(cancellationToken);
    }
}
