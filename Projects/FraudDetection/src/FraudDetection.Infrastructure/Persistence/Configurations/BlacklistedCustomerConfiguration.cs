using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class BlacklistedCustomerConfiguration : IEntityTypeConfiguration<BlacklistedCustomer>
{
    public void Configure(EntityTypeBuilder<BlacklistedCustomer> builder)
    {
        builder.ToTable("BlacklistedCustomers");

        // The customer ID is the natural key — one entry per customer.
        builder.HasKey(b => b.CustomerId);

        builder.Property(b => b.CustomerId)
            .HasConversion<CustomerIdConverter>()
            .ValueGeneratedNever();

        builder.Property(b => b.Reason)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();
    }
}
