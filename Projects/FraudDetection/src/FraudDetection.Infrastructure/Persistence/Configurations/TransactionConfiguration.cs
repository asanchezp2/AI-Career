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

        builder.HasKey(t => t.TransactionExternalId);

        // Composite index on (SourceAccountId, CreatedAt) covering the daily
        // accumulated query: equality on the source account + range scan on the
        // UTC day window. Previously (CustomerId, CreatedAt) for the velocity
        // rule — replaced by the real challenge's daily-accumulation rule
        // (see ADR-051/ADR-057).
        builder.HasIndex(t => new { t.SourceAccountId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_SourceAccountId_CreatedAt");

        builder.Property(t => t.TransactionExternalId)
            .IsRequired();

        builder.Property(t => t.SourceAccountId)
            .IsRequired();

        builder.Property(t => t.TargetAccountId)
            .IsRequired();

        builder.Property(t => t.TransferTypeId)
            .IsRequired();

        builder.Property(t => t.Value)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Status and rejection reason are stored as LOWERCASE strings so the
        // database representation matches the JSON wire format
        // ("pending"/"approved"/"rejected", "highvalue"/"dailyaccumulated").
        builder.Property(t => t.Status)
            .HasConversion<TransactionStatusConverter>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.RejectionReason)
            .HasConversion<RejectionReasonConverter>()
            .HasMaxLength(20)
            .IsRequired(false);
    }
}