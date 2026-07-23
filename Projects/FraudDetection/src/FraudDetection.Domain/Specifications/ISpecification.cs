using FraudDetection.Domain.Entities;

namespace FraudDetection.Domain.Specifications;

/// <summary>
/// Defines a specification that determines whether a Transaction satisfies a given criterion.
/// </summary>
public interface ISpecification
{
    /// <summary>
    /// Evaluates whether the specified transaction satisfies the criterion.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <returns>True if the transaction satisfies the criterion; otherwise, false.</returns>
    bool IsSatisfiedBy(Transaction transaction);
}
