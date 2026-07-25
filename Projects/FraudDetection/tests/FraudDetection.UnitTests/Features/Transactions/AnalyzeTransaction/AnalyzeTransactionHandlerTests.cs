using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Features.Transactions.AnalyzeTransaction;

public class AnalyzeTransactionHandlerTests
{
    [Fact]
    public void Handle_ValidCommand_ReturnsResultWithCorrectTransactionId()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 250.50m,
            Currency = "USD"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.Equal(transactionId, result.TransactionId);
    }

    [Fact]
    public void Handle_ValidCommand_NoMatchingRules_TransactionIsApproved()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "EUR"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.Equal(TransactionStatus.Approved, result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public void Handle_HighAmountTransaction_TransactionIsUnderReview()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 50000,
            Currency = "USD"
        };
        var handler = CreateHandler(WithHighAmountRule(10000, 50));

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.Equal(TransactionStatus.UnderReview, result.Status);
        Assert.Equal(50, result.TotalRiskScore);
    }

    [Fact]
    public void Handle_LowAmountTransaction_DoesNotMatchHighAmountRule()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };
        var handler = CreateHandler(WithHighAmountRule(10000, 50));

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.Equal(TransactionStatus.Approved, result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public void Handle_DisabledRules_AreIgnored()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 50000,
            Currency = "USD"
        };
        var handler = CreateHandler(WithDisabledHighAmountRule(10000, 50));

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.Equal(TransactionStatus.Approved, result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public void Handle_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = -50,
            Currency = "USD"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => handler.Handle(command));
        Assert.Contains("Amount", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Handle_WithEmptyTransactionId_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.Empty,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => handler.Handle(command));
    }

    [Fact]
    public void Handle_WithEmptyCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.Empty,
            Amount = 100,
            Currency = "USD"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => handler.Handle(command));
    }

    [Fact]
    public void Handle_WithInvalidCurrency_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "InvalidCurrency"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => handler.Handle(command));
    }

    [Fact]
    public void Handle_ZeroAmount_DomainAcceptsValue()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 0,
            Currency = "USD"
        };
        // Zero amount does not trigger HighAmount (threshold 10000), so Approved
        var handler = CreateHandler(WithHighAmountRule(10000, 50));

        // Act
        var result = handler.Handle(command);

        // Assert
        // Zero amount passes Domain (Money accepts zero), no rules match → Approved
        Assert.Equal(TransactionStatus.Approved, result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public void Handle_AppliesStatusThroughTransactionBehavior()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 80000,
            Currency = "MXN"
        };
        var handler = CreateHandler(WithHighAmountRule(10000, 50));

        // Act
        var result = handler.Handle(command);

        // Assert
        // The status transition went through transaction.MarkForReview(),
        // which means the Transaction entity managed the state change.
        Assert.Equal(TransactionStatus.UnderReview, result.Status);
    }

    private static AnalyzeTransactionHandler CreateHandler(
        TestFraudRuleProvider provider)
    {
        var engine = new FraudRuleEngine();
        return new AnalyzeTransactionHandler(engine, provider);
    }

    private static TestFraudRuleProvider WithNoMatchingRules()
    {
        return new TestFraudRuleProvider(
            Array.Empty<FraudRule>(),
            new Dictionary<string, ISpecification>());
    }

    private static TestFraudRuleProvider WithHighAmountRule(
        decimal threshold, int riskScore)
    {
        var rule = new FraudRule(FraudRuleId.New(), "HighAmount", riskScore);
        return new TestFraudRuleProvider(
            new[] { rule },
            new Dictionary<string, ISpecification>
            {
                ["HighAmount"] = new HighAmountTransactionSpecification(threshold)
            });
    }

    private static TestFraudRuleProvider WithDisabledHighAmountRule(
        decimal threshold, int riskScore)
    {
        var rule = new FraudRule(FraudRuleId.New(), "HighAmount", riskScore);
        rule.Disable();
        return new TestFraudRuleProvider(
            new[] { rule },
            new Dictionary<string, ISpecification>
            {
                ["HighAmount"] = new HighAmountTransactionSpecification(threshold)
            });
    }

    /// <summary>
    /// Test double for IFraudRuleProvider that allows full control over rules and specifications.
    /// </summary>
    private sealed class TestFraudRuleProvider : IFraudRuleProvider
    {
        private readonly IReadOnlyCollection<FraudRule> _rules;
        private readonly IReadOnlyDictionary<string, ISpecification> _specifications;

        public TestFraudRuleProvider(
            IReadOnlyCollection<FraudRule> rules,
            IReadOnlyDictionary<string, ISpecification> specifications)
        {
            _rules = rules;
            _specifications = specifications;
        }

        public IReadOnlyCollection<FraudRule> GetAllRules() => _rules;
        public IReadOnlyDictionary<string, ISpecification> GetSpecifications() => _specifications;
    }
}
