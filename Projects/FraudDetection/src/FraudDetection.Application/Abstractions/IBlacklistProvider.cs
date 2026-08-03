using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Application.Abstractions;

/// <summary>
/// Provides blacklisted customer information.
/// </summary>
public interface IBlacklistProvider
{
    Task<bool> IsBlacklistedAsync(CustomerId customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BlacklistedCustomer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(BlacklistedCustomer customer, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}
