using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Domain.Entities;

/// <summary>
/// A customer explicitly flagged as blacklisted.
/// </summary>
public class BlacklistedCustomer
{
    public CustomerId CustomerId { get; private set; }
    public string Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BlacklistedCustomer() { CustomerId = null!; Reason = null!; } // EF Core

    public BlacklistedCustomer(CustomerId customerId, string reason)
    {
        Guard.AgainstNull(customerId, nameof(customerId));
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        CustomerId = customerId;
        Reason = reason;
        CreatedAt = DateTime.UtcNow;
    }
}
