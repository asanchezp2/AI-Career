using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FraudDetection.UnitTests.Features.Transactions.AnalyzeTransaction;

public class AnalyzeTransactionHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsResultWithCorrectTransactionId()
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
        var result = await handler.Handle(command);

        // Assert
        Assert.Equal(transactionId, result.TransactionId);
    }

    [Fact]
    public async Task Handle_ValidCommand_NoMatchingRules_TransactionIsApproved()
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
        var result = await handler.Handle(command);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task Handle_HighAmountTransaction_TransactionIsUnderReview()
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
        var result = await handler.Handle(command);

        // Assert
        Assert.Equal("UnderReview", result.Status);
        Assert.Equal(50, result.TotalRiskScore);
    }

    [Fact]
    public async Task Handle_LowAmountTransaction_DoesNotMatchHighAmountRule()
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
        var result = await handler.Handle(command);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task Handle_DisabledRules_AreIgnored()
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
        var result = await handler.Handle(command);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task Handle_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
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
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.Handle(command));
        Assert.Contains("Amount", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyTransactionId_ThrowsArgumentException()
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
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command));
    }

    [Fact]
    public async Task Handle_WithEmptyCustomerId_ThrowsArgumentException()
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
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command));
    }

    [Fact]
    public async Task Handle_WithInvalidCurrencyLength_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "TOOLONG"
        };
        var handler = CreateHandler(WithNoMatchingRules());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command));
    }

    [Fact]
    public async Task Handle_ZeroAmount_DomainAcceptsValue()
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
        var result = await handler.Handle(command);

        // Assert
        // Zero amount passes Domain (Money accepts zero), no rules match → Approved
        Assert.Equal("Approved", result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task Handle_AppliesStatusThroughTransactionBehavior()
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
        var result = await handler.Handle(command);

        // Assert
        // The status transition went through transaction.MarkForReview(),
        // which means the Transaction entity managed the state change.
        Assert.Equal("UnderReview", result.Status);
    }

    [Fact]
    public async Task Handle_BlacklistedCustomer_TransactionIsRejected()
    {
        // Arrange
        var blacklistedCustomerId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = blacklistedCustomerId,
            Amount = 100,
            Currency = "USD"
        };
        var handler = CreateHandler(WithBlacklistRejectionRule(blacklistedCustomerId));

        // Act
        var result = await handler.Handle(command);

        // Assert
        Assert.Equal("Rejected", result.Status);
        Assert.Equal(100, result.TotalRiskScore);
    }

    private static AnalyzeTransactionHandler CreateHandler(
        TestFraudRuleProvider provider)
    {
        var engine = new FraudRuleEngine();
        var repository = new StubTransactionRepository();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AnalyzeTransactionHandler>();
        return new AnalyzeTransactionHandler(engine, provider, repository, logger);
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

    private static TestFraudRuleProvider WithBlacklistRejectionRule(Guid blacklistedCustomerId)
    {
        var blacklistedCustomer = CustomerId.From(blacklistedCustomerId);
        var rule = new FraudRule(FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject);
        return new TestFraudRuleProvider(
            new[] { rule },
            new Dictionary<string, ISpecification>
            {
                ["Blacklist"] = new BlacklistCustomerSpecification(new[] { blacklistedCustomer })
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

    /// <summary>
    /// Stub implementation of ITransactionRepository for unit tests.
    /// Returns zero recent transactions and completes silently.
    /// </summary>
    private sealed class StubTransactionRepository : ITransactionRepository
    {
        public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Transaction?> GetByIdAsync(TransactionId transactionId, CancellationToken cancellationToken = default)
            => Task.FromResult<Transaction?>(null);

        public Task<int> GetTransactionCountSinceAsync(CustomerId customerId, DateTime since, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
