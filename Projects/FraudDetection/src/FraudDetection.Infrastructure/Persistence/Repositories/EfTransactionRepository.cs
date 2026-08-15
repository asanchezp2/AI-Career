using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Exceptions;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
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
    /// <remarks>
    /// A duplicate primary key surfaces as a DbUpdateException. It is translated
    /// to <see cref="TransactionConflictException"/> — defensive only: the
    /// transaction external ID is server-generated (Guid.NewGuid), so a
    /// collision is virtually impossible, but an unhandled 500 would be worse.
    /// </remarks>
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueKeyViolation(ex))
        {
            throw new TransactionConflictException(transaction.TransactionExternalId, ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// AsNoTracking guarantees the PERSISTED row is returned — after a failed
    /// insert the attempted entity stays in the change tracker and a tracked
    /// query would return that unsaved entity instead of the database row.
    /// </remarks>
    public async Task<Transaction?> GetByIdAsync(Guid transactionExternalId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionExternalId == transactionExternalId, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Day window: [midnight UTC, midnight UTC + 1 day). The sum INCLUDES the
    /// transaction being evaluated — it is already persisted as Pending when
    /// the anti-fraud worker runs the evaluation (semantics documented in
    /// ADR-057). The (SourceAccountId, CreatedAt) composite index covers this
    /// query.
    /// </remarks>
    public async Task<decimal> GetDailyAccumulatedAsync(
        Guid sourceAccountId,
        DateOnly day,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        // SQLite cannot translate SUM(decimal) to SQL (it has no native decimal
        // type), so the aggregate is computed over a double projection and the
        // single result is cast back to decimal. SQL Server translates this
        // identically (SUM(CAST(Value AS float))). The cast back to decimal is
        // exact to the cent for any realistic daily accumulation — double
        // keeps ~15-17 significant digits, far beyond a one-day account total.
        var total = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.SourceAccountId == sourceAccountId
                        && t.CreatedAt >= startOfDay
                        && t.CreatedAt < endOfDay)
            .Select(t => (double)t.Value)
            .SumAsync(cancellationToken);

        return (decimal)total;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The entity was loaded with AsNoTracking, so Update attaches it with
    /// Modified state and rewrites the row. Acceptable here because only the
    /// status/rejection-reason columns change and this deployment uses a single
    /// writer per transaction (the anti-fraud worker). No concurrency token —
    /// last write wins — a documented pragmatic choice (see ADR-054).
    /// </remarks>
    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Detects a unique-key (primary key or unique index) constraint violation
    /// across the providers used by this project: SQL Server in production and
    /// SQLite in the integration test suite.
    /// </summary>
    private static bool IsUniqueKeyViolation(DbUpdateException ex)
    {
        // SQL Server: 2601 = duplicate key in a unique index, 2627 = unique constraint
        // (primary key) violation.
        if (ex.InnerException is SqlException { Number: 2601 or 2627 })
            return true;

        // SQLite (integration tests): unique/PK violations surface with this message.
        // Other SQLite constraint failures (e.g. NOT NULL) do not match.
        return ex.InnerException?.Message.Contains(
            "UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
    }
}