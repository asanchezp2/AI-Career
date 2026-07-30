using FraudDetection.Application.Abstractions;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Infrastructure.Providers;

/// <summary>
/// Provides in-memory fraud rules and specifications for the current development stage.
/// Replaces database-backed storage until persistence is implemented.
/// </summary>
public sealed class InMemoryFraudRuleProvider : IFraudRuleProvider
{
    private readonly IReadOnlyCollection<FraudRule> _rules;
    private readonly IReadOnlyDictionary<string, ISpecification> _specifications;

    public InMemoryFraudRuleProvider()
    {
        _rules = InitializeRules();
        _specifications = InitializeSpecifications();
    }

    public IReadOnlyCollection<FraudRule> GetAllRules() => _rules;

    public IReadOnlyDictionary<string, ISpecification> GetSpecifications() => _specifications;

    private static IReadOnlyCollection<FraudRule> InitializeRules()
    {
        return new List<FraudRule>
        {
            new(FraudRuleId.New(), "HighAmount", 50, FraudRuleAction.Review),
            new(FraudRuleId.New(), "Velocity", 70, FraudRuleAction.Reject),
            new(FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject),
            new(FraudRuleId.New(), "HighRiskCountry", 30, FraudRuleAction.Review)
        }.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, ISpecification> InitializeSpecifications()
    {
        return new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000m),
            ["Velocity"] = new VelocityTransactionSpecification(maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1)),
            ["Blacklist"] = new BlacklistCustomerSpecification(GetBlacklistedCustomers()),
            ["HighRiskCountry"] = new HighRiskCountrySpecification(GetHighRiskCountryCodes())
        };
    }

    private static IEnumerable<CustomerId> GetBlacklistedCustomers()
    {
        // Pre-seeded blacklist for demo purposes.
        // In production, this would be loaded from a persistent store.
        return new List<CustomerId>
        {
            CustomerId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"))
        };
    }

    private static IEnumerable<string> GetHighRiskCountryCodes()
    {
        // ISO 3166-1 alpha-2 country codes for high-risk regions.
        return new[] { "IR", "KP", "SY", "VE" };
    }
}
