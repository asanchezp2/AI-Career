using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;
using FraudDetection.Domain.Specifications;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Services;

public class FraudRuleEngineTests
{
    private readonly FraudRuleEngine _engine = new();

    [Fact]
    public void Evaluate_NoRules_ReturnsApprovedWithZeroScore()
    {
        // Arrange
        var transaction = CreateTransaction(100);
        var rules = Array.Empty<FraudRule>();
        var specifications = new Dictionary<string, ISpecification>();

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(0, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Empty(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_EmptyRulesCollection_ReturnsApprovedWithZeroScore()
    {
        // Arrange
        var transaction = CreateTransaction(100);
        var rules = new List<FraudRule>();
        var specifications = new Dictionary<string, ISpecification>();

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(0, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Empty(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_AllRulesDisabled_ReturnsApprovedWithZeroScore()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var highAmountRule = CreateRule("HighAmount", 50);
        highAmountRule.Disable();
        var rules = new[] { highAmountRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000)
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(0, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Empty(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_OneApplicableRule_ReturnsUnderReviewWithCorrectScore()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var highAmountRule = CreateRule("HighAmount", 50);
        var rules = new[] { highAmountRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000)
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(50, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.UnderReview, result.RecommendedStatus);
        Assert.Single(result.MatchedRules);
        Assert.Contains(highAmountRule, result.MatchedRules);
    }

    [Fact]
    public void Evaluate_MultipleApplicableRules_AccumulatesScore()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var highAmountRule = CreateRule("HighAmount", 50);
        var crossBorderRule = CreateRule("CrossBorder", 30);
        var rules = new[] { highAmountRule, crossBorderRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000),
            ["CrossBorder"] = new AlwaysTrueSpecification()
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(80, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.UnderReview, result.RecommendedStatus);
        Assert.Equal(2, result.MatchedRules.Count);
    }

    [Fact]
    public void Evaluate_DisabledRule_IsIgnored()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var enabledRule = CreateRule("HighAmount", 50);
        var disabledRule = CreateRule("CrossBorder", 80);
        disabledRule.Disable();
        var rules = new[] { enabledRule, disabledRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000),
            ["CrossBorder"] = new AlwaysTrueSpecification()
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(50, result.TotalRiskScore);
        Assert.Single(result.MatchedRules);
        Assert.Contains(enabledRule, result.MatchedRules);
    }

    [Fact]
    public void Evaluate_RuleWithoutSpecification_IsSkipped()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var ruleWithSpec = CreateRule("HighAmount", 50);
        var ruleWithoutSpec = CreateRule("UnknownRule", 90);
        var rules = new[] { ruleWithSpec, ruleWithoutSpec };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000)
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(50, result.TotalRiskScore);
        Assert.Single(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_SpecificationNotSatisfied_SkipsRule()
    {
        // Arrange
        var transaction = CreateTransaction(500);
        var highAmountRule = CreateRule("HighAmount", 50);
        var rules = new[] { highAmountRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000)
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(0, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Empty(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_NoRulesMatch_ReturnsApproved()
    {
        // Arrange
        var transaction = CreateTransaction(500);
        var rule1 = CreateRule("HighAmount", 40);
        var rule2 = CreateRule("CrossBorder", 30);
        var rules = new[] { rule1, rule2 };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000),
            ["CrossBorder"] = new AlwaysFalseSpecification()
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(0, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Empty(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_NullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var rules = Array.Empty<FraudRule>();
        var specifications = new Dictionary<string, ISpecification>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _engine.Evaluate(null!, rules, specifications));
    }

    [Fact]
    public void Evaluate_NullFraudRules_ThrowsArgumentNullException()
    {
        // Arrange
        var transaction = CreateTransaction(100);
        var specifications = new Dictionary<string, ISpecification>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _engine.Evaluate(transaction, null!, specifications));
    }

    [Fact]
    public void Evaluate_NullSpecifications_ThrowsArgumentNullException()
    {
        // Arrange
        var transaction = CreateTransaction(100);
        var rules = Array.Empty<FraudRule>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _engine.Evaluate(transaction, rules, null!));
    }

    [Fact]
    public void Evaluate_ResultContainsMatchedRules()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var rule1 = CreateRule("HighAmount", 50);
        var rule2 = CreateRule("CrossBorder", 30);
        var rules = new[] { rule1, rule2 };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000),
            ["CrossBorder"] = new AlwaysTrueSpecification()
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(2, result.MatchedRules.Count);
        Assert.Contains(rule1, result.MatchedRules);
        Assert.Contains(rule2, result.MatchedRules);
    }

    [Fact]
    public void Evaluate_RejectionRuleMatched_ReturnsRejected()
    {
        // Arrange
        var transaction = CreateTransaction(100);
        var rejectRule = new FraudRule(FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject);
        var rules = new[] { rejectRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["Blacklist"] = new AlwaysTrueSpecification()
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(100, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Rejected, result.RecommendedStatus);
        Assert.Single(result.MatchedRules);
    }

    [Fact]
    public void Evaluate_NoRejectionRule_ButReviewRuleMatched_ReturnsUnderReview()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var reviewRule = new FraudRule(FraudRuleId.New(), "HighAmount", 50, FraudRuleAction.Review);
        var rules = new[] { reviewRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["HighAmount"] = new HighAmountTransactionSpecification(10000)
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(50, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.UnderReview, result.RecommendedStatus);
    }

    [Fact]
    public void Evaluate_RejectionAndReviewRulesMatched_ReturnsRejected()
    {
        // Arrange
        var transaction = CreateTransaction(20000);
        var rejectRule = new FraudRule(FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject);
        var reviewRule = new FraudRule(FraudRuleId.New(), "HighAmount", 50, FraudRuleAction.Review);
        var rules = new[] { rejectRule, reviewRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["Blacklist"] = new AlwaysTrueSpecification(),
            ["HighAmount"] = new HighAmountTransactionSpecification(10000)
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(150, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Rejected, result.RecommendedStatus);
        Assert.Equal(2, result.MatchedRules.Count);
    }

    [Fact]
    public void Evaluate_DisabledRejectionRule_IsIgnored()
    {
        // Arrange
        var transaction = CreateTransaction(100);
        var rejectRule = new FraudRule(FraudRuleId.New(), "Blacklist", 100, FraudRuleAction.Reject);
        rejectRule.Disable();
        var rules = new[] { rejectRule };
        var specifications = new Dictionary<string, ISpecification>
        {
            ["Blacklist"] = new AlwaysTrueSpecification()
        };

        // Act
        var result = _engine.Evaluate(transaction, rules, specifications);

        // Assert
        Assert.Equal(0, result.TotalRiskScore);
        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Empty(result.MatchedRules);
    }

    private static Transaction CreateTransaction(decimal amount, string currency = "USD")
    {
        return new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(amount, currency),
            DateTime.UtcNow);
    }

    private static FraudRule CreateRule(string name, int riskScore)
    {
        return new FraudRule(FraudRuleId.New(), name, riskScore);
    }

    /// <summary>
    /// Test specification that always returns true.
    /// </summary>
    private sealed class AlwaysTrueSpecification : ISpecification
    {
        public bool IsSatisfiedBy(Transaction transaction) => true;
    }

    /// <summary>
    /// Test specification that always returns false.
    /// </summary>
    private sealed class AlwaysFalseSpecification : ISpecification
    {
        public bool IsSatisfiedBy(Transaction transaction) => false;
    }
}
