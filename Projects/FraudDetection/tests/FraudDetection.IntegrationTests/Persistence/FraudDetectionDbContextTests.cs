using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.IntegrationTests.Persistence;

public class FraudDetectionDbContextTests
{
    private static FraudDetectionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FraudDetectionDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new FraudDetectionDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void DbContext_CanBeCreated()
    {
        using var context = CreateDbContext();
        Assert.NotNull(context);
    }

    [Fact]
    public void DbContext_DatabaseCreated_HasTransactionsTable()
    {
        using var context = CreateDbContext();

        var transaction = context.Model.FindEntityType(typeof(Transaction));
        Assert.NotNull(transaction);
        Assert.Equal("Transactions", transaction.GetTableName());
    }

    [Fact]
    public void DbContext_DatabaseCreated_HasFraudRulesTable()
    {
        using var context = CreateDbContext();

        var entityType = context.Model.FindEntityType(typeof(FraudRule));
        Assert.NotNull(entityType);
        Assert.Equal("FraudRules", entityType.GetTableName());
    }

    [Fact]
    public async Task SaveAndRetrieveTransaction_RoundtripsCorrectly()
    {
        using var context = CreateDbContext();

        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(1500.50m, "USD");
        var transaction = new Transaction(transactionId, customerId, amount);

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        Assert.NotNull(loaded);
        Assert.Equal(transactionId, loaded!.Id);
        Assert.Equal(customerId, loaded.CustomerId);
        Assert.Equal(amount, loaded.Amount);
        Assert.Equal(TransactionStatus.Pending, loaded.Status);
        Assert.NotEqual(default, loaded.CreatedAt);
    }

    [Fact]
    public async Task SaveAndRetrieveTransaction_AfterStatusChange()
    {
        using var context = CreateDbContext();

        var transaction = new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(50000, "USD"));
        transaction.MarkForReview();

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(loaded);
        Assert.Equal(TransactionStatus.UnderReview, loaded!.Status);
    }

    [Fact]
    public async Task SaveAndRetrieveFraudRule_RoundtripsCorrectly()
    {
        using var context = CreateDbContext();

        var fraudRule = new FraudRule(
            FraudRuleId.New(),
            "HighAmount",
            50);

        context.FraudRules.Add(fraudRule);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.FraudRules
            .FirstOrDefaultAsync(r => r.Id == fraudRule.Id);

        Assert.NotNull(loaded);
        Assert.Equal(fraudRule.Id, loaded!.Id);
        Assert.Equal("HighAmount", loaded.RuleName);
        Assert.Equal(50, loaded.RiskScore);
        Assert.True(loaded.IsEnabled);
    }

    [Fact]
    public async Task SaveAndRetrieveFraudRule_DisabledRule()
    {
        using var context = CreateDbContext();

        var fraudRule = new FraudRule(
            FraudRuleId.New(),
            "OldRule",
            30);
        fraudRule.Disable();

        context.FraudRules.Add(fraudRule);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.FraudRules
            .FirstOrDefaultAsync(r => r.Id == fraudRule.Id);

        Assert.NotNull(loaded);
        Assert.False(loaded!.IsEnabled);
    }

    [Fact]
    public async Task MoneyPrecision_RoundtripsCorrectly()
    {
        using var context = CreateDbContext();

        var amount = new Money(12345.67m, "USD");
        var transaction = new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            amount);

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(loaded);
        Assert.Equal(12345.67m, loaded!.Amount.Amount);
        Assert.Equal("USD", loaded.Amount.Currency);
    }

    [Fact]
    public async Task MultipleTransactions_AreIndependent()
    {
        using var context = CreateDbContext();

        var tx1 = new Transaction(
            TransactionId.New(), CustomerId.New(), new Money(100, "USD"));
        var tx2 = new Transaction(
            TransactionId.New(), CustomerId.New(), new Money(200, "EUR"));

        context.Transactions.AddRange(tx1, tx2);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var count = await context.Transactions.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task SaveAndRetrieveTransaction_WithDifferentCurrency()
    {
        using var context = CreateDbContext();

        var transaction = new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(999.99m, "MXN"));

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loaded = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(loaded);
        Assert.Equal("MXN", loaded!.Amount.Currency);
    }

    [Fact]
    public async Task FraudRule_RiskScoreBoundaries_PersistCorrectly()
    {
        using var context = CreateDbContext();

        var ruleMin = new FraudRule(FraudRuleId.New(), "MinScore", 0);
        var ruleMax = new FraudRule(FraudRuleId.New(), "MaxScore", 100);

        context.FraudRules.AddRange(ruleMin, ruleMax);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var loadedMin = await context.FraudRules
            .FirstOrDefaultAsync(r => r.Id == ruleMin.Id);
        var loadedMax = await context.FraudRules
            .FirstOrDefaultAsync(r => r.Id == ruleMax.Id);

        Assert.NotNull(loadedMin);
        Assert.NotNull(loadedMax);
        Assert.Equal(0, loadedMin!.RiskScore);
        Assert.Equal(100, loadedMax!.RiskScore);
    }
}
