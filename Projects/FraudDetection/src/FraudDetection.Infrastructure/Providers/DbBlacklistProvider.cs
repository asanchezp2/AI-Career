using FraudDetection.Application.Abstractions;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Providers;

/// <summary>
/// Provides blacklisted customer information backed by the EF Core database.
/// </summary>
public class DbBlacklistProvider : IBlacklistProvider
{
    private readonly FraudDetectionDbContext _context;

    public DbBlacklistProvider(FraudDetectionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsBlacklistedAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        return await _context.BlacklistedCustomers
            .AsNoTracking()
            .AnyAsync(b => b.CustomerId == customerId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BlacklistedCustomer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BlacklistedCustomers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BlacklistedCustomer customer, CancellationToken cancellationToken = default)
    {
        _context.BlacklistedCustomers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.BlacklistedCustomers
            .FirstOrDefaultAsync(b => b.CustomerId == customerId, cancellationToken);
        if (entity is null) return false;
        _context.BlacklistedCustomers.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
