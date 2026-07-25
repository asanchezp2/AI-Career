using FraudDetection.Application.Abstractions;
using FraudDetection.Domain.Entities;
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
            new(FraudRuleId.New(), "HighAmount", 50)
        }.AsReadOnly();
    }

    private static IReadOnlyDictionary<string, ISpecification> InitializeSpecifications()
    {
        return new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000m)
        };
    }
}
