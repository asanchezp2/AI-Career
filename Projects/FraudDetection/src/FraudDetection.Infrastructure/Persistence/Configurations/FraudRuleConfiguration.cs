using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class FraudRuleConfiguration : IEntityTypeConfiguration<FraudRule>
{
    public void Configure(EntityTypeBuilder<FraudRule> builder)
    {
        builder.ToTable("FraudRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion<FraudRuleIdConverter>()
            .ValueGeneratedNever();

        builder.Property(r => r.RuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.RiskScore)
            .IsRequired();

        builder.Property(r => r.IsEnabled)
            .IsRequired();
    }
}
