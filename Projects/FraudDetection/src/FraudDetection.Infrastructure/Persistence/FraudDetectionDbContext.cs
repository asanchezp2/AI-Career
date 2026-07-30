using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence;

public sealed class FraudDetectionDbContext : DbContext
{
    public FraudDetectionDbContext(DbContextOptions<FraudDetectionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FraudRule> FraudRules => Set<FraudRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new FraudRuleConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
