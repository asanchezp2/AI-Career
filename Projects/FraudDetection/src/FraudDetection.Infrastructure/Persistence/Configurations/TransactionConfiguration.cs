using System.Text.Json;
using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.CustomerId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_CustomerId_CreatedAt");

        builder.Property(t => t.Id)
            .HasConversion<TransactionIdConverter>()
            .ValueGeneratedNever();

        builder.Property(t => t.CustomerId)
            .HasConversion<CustomerIdConverter>()
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<TransactionStatusConverter>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Country)
            .HasMaxLength(2)
            .IsRequired(false);

        builder.Property(t => t.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new())
            .IsRequired(false);

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(t => t.Amount).IsRequired();
    }
}
