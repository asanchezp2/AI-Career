using FraudDetection.Application.Abstractions;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
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
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _specifications = InitializeSpecifications();
    }

    public IReadOnlyCollection<FraudRule> GetAllRules()
    {
        return _dbContext.FraudRules
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
            ["HighAmount"] = new HighAmountTransactionSpecification(10000m)
        };
    }
}
