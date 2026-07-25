using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications;

namespace FraudDetection.Application.Abstractions;

/// <summary>
/// Provides fraud rules and their associated specifications for transaction evaluation.
/// </summary>
public interface IFraudRuleProvider
{
    /// <summary>
    /// Gets all available fraud rules.
    /// </summary>
    IReadOnlyCollection<FraudRule> GetAllRules();

    /// <summary>
    /// Gets the specification mapping keyed by rule name.
    /// Each enabled rule should have a corresponding specification to evaluate.
    /// </summary>
    IReadOnlyDictionary<string, ISpecification> GetSpecifications();
}
