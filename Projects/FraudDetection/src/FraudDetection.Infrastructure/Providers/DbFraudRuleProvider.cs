using FraudDetection.Application.Abstractions;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Providers;

/// <summary>
/// Provides fraud rules and specifications using EF Core database access.
/// Rules are loaded from the database; specifications are created in code based on rule names.
/// </summary>
public sealed class DbFraudRuleProvider : IFraudRuleProvider
{
    private readonly FraudDetectionDbContext _dbContext;
    private readonly IReadOnlyDictionary<string, ISpecification> _specifications;

    public DbFraudRuleProvider(FraudDetectionDbContext dbContext)
    {
        Guard.AgainstNull(dbContext, nameof(dbContext));
        _dbContext = dbContext;
        _specifications = InitializeSpecifications();
    }

    public IReadOnlyCollection<FraudRule> GetAllRules()
    {
        return _dbContext.FraudRules
            .Where(r => r.IsEnabled)
            .AsNoTracking()
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyDictionary<string, ISpecification> GetSpecifications()
    {
        return _specifications;
    }

    private static IReadOnlyDictionary<string, ISpecification> InitializeSpecifications()
    {
        return new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000m),
            ["Velocity"] = new VelocityTransactionSpecification(maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1)),
            ["Blacklist"] = new BlacklistCustomerSpecification(GetBlacklistedCustomers()),
            ["HighRiskCountry"] = new HighRiskCountrySpecification(GetHighRiskCountries())
        };
    }

    private static IEnumerable<CustomerId> GetBlacklistedCustomers()
    {
        // Pre-seeded blacklist for demo purposes.
        // In production, this would be loaded from a dedicated table.
        return new List<CustomerId>
        {
            CustomerId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"))
        };
    }

    private static IEnumerable<string> GetHighRiskCountries()
    {
        // ISO 3166-1 alpha-2 country codes for high-risk regions
        return new[] { "IR", "KP", "SY", "VE" };
    }
}
