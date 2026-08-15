using FraudDetection.Domain.Entities;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FraudDetection.IntegrationTests.Persistence;

public class DbContextTests
{
    private static IEntityType TransactionEntity()
    {
        var options = new DbContextOptionsBuilder<FraudDetectionDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new FraudDetectionDbContext(options);
        return context.Model.FindEntityType(typeof(Transaction))!;
    }

    [Fact]
    public void Transaction_MapsToTableNamedTransactions()
    {
        Assert.Equal("Transactions", TransactionEntity().GetTableName());
    }

    [Fact]
    public void Transactions_HasIndexOnSourceAccountIdAndCreatedAt()
    {
        var entity = TransactionEntity();

        var index = Assert.Single(
            entity.GetIndexes(),
            i => i.Properties
                .Select(p => p.Name)
                .SequenceEqual(new[]
                {
                    nameof(Transaction.SourceAccountId),
                    nameof(Transaction.CreatedAt)
                }));

        Assert.Equal("IX_Transactions_SourceAccountId_CreatedAt", index.GetDatabaseName());
    }

    [Fact]
    public void Value_HasPrecision18AndScale2()
    {
        var property = TransactionEntity().FindProperty(nameof(Transaction.Value))!;

        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(2, property.GetScale());
    }

    [Fact]
    public void Status_IsRequiredLowerCaseStringWithMaxLength20()
    {
        var property = TransactionEntity().FindProperty(nameof(Transaction.Status))!;

        // GetProviderClrType() is only materialized on a runtime-finalized
        // model, which the metadata tests here intentionally do not build.
        // The value converter's provider type is the authoritative assertion
        // of the persisted representation ("stored as a lowercase string").
        Assert.NotNull(property.GetValueConverter());
        Assert.Equal(typeof(string), property.GetValueConverter()!.ProviderClrType);
        Assert.Equal(20, property.GetMaxLength());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void RejectionReason_IsNullableLowerCaseStringWithMaxLength20()
    {
        var property = TransactionEntity().FindProperty(nameof(Transaction.RejectionReason))!;

        // Same rationale as Status_IsRequiredLowerCaseStringWithMaxLength20:
        // assert the string provider type through the configured converter.
        Assert.NotNull(property.GetValueConverter());
        Assert.Equal(typeof(string), property.GetValueConverter()!.ProviderClrType);
        Assert.Equal(20, property.GetMaxLength());
        Assert.True(property.IsNullable);
    }
}