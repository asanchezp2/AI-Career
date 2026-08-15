using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the FraudDetection database.
/// The real challenge model has a single aggregate: Transaction. The former
/// FraudRules and BlacklistedCustomers tables were removed — the two fraud
/// rules are fixed business constants in the Domain layer (see ADR-051).
/// </summary>
public sealed class FraudDetectionDbContext : DbContext
{
    public FraudDetectionDbContext(DbContextOptions<FraudDetectionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}