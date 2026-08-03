using FraudDetection.Application.Abstractions;
using FraudDetection.Domain;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Configuration;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FraudDetection.Infrastructure.Providers;

/// <summary>
/// Provides fraud rules and specifications using EF Core database access.
/// Rules are loaded from the database; specifications are created in code based on rule names.
/// The Blacklist specification is intentionally not created here — it is dynamic and
/// loaded per request by the application layer via <see cref="IBlacklistProvider"/>.
/// </summary>
public sealed class DbFraudRuleProvider : IFraudRuleProvider
{
    private readonly FraudDetectionDbContext _dbContext;
    private readonly FraudRuleOptions _options;
    private readonly IReadOnlyDictionary<string, ISpecification> _specifications;

    public DbFraudRuleProvider(FraudDetectionDbContext dbContext, IOptions<FraudRuleOptions> options)
    {
        Guard.AgainstNull(dbContext, nameof(dbContext));
        Guard.AgainstNull(options, nameof(options));
        _dbContext = dbContext;
        _options = options.Value;
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

    private IReadOnlyDictionary<string, ISpecification> InitializeSpecifications()
    {
        return new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(_options.HighAmountThreshold),
            ["Velocity"] = new VelocityTransactionSpecification(
                maxTransactionCount: _options.VelocityMaxTransactions,
                timeWindow: TimeSpan.FromMinutes(_options.VelocityWindowMinutes)),
            ["HighRiskCountry"] = new HighRiskCountrySpecification(_options.HighRiskCountries)
        };
    }
}
