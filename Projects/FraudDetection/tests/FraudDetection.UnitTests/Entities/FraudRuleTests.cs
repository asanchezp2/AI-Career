using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Entities;

public class FraudRuleTests
{
    [Fact]
    public void FraudRule_CreatedSuccessfully()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act
        var rule = new FraudRule(fraudRuleId, "HighAmount", 50);

        // Assert
        Assert.Equal(fraudRuleId, rule.Id);
        Assert.Equal("HighAmount", rule.RuleName);
        Assert.Equal(50, rule.RiskScore);
    }

    [Fact]
    public void FraudRule_StartsEnabled()
    {
        // Arrange & Act
        var rule = CreateValidRule();

        // Assert
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void FraudRule_NullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FraudRule(null!, "HighAmount", 50));
    }

    [Fact]
    public void FraudRule_NullRuleName_ThrowsArgumentException()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FraudRule(fraudRuleId, null!, 50));
    }

    [Fact]
    public void FraudRule_EmptyRuleName_ThrowsArgumentException()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FraudRule(fraudRuleId, string.Empty, 50));
    }

    [Fact]
    public void FraudRule_WhitespaceRuleName_ThrowsArgumentException()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FraudRule(fraudRuleId, "   ", 50));
    }

    [Fact]
    public void FraudRule_RiskScoreLessThanZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FraudRule(fraudRuleId, "HighAmount", -1));
    }

    [Fact]
    public void FraudRule_RiskScoreGreaterThan100_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FraudRule(fraudRuleId, "HighAmount", 101));
    }

    [Fact]
    public void FraudRule_RiskScoreZero_IsValid()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act
        var rule = new FraudRule(fraudRuleId, "ZeroScore", 0);

        // Assert
        Assert.Equal(0, rule.RiskScore);
    }

    [Fact]
    public void FraudRule_RiskScore100_IsValid()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();

        // Act
        var rule = new FraudRule(fraudRuleId, "MaxScore", 100);

        // Assert
        Assert.Equal(100, rule.RiskScore);
    }

    [Fact]
    public void FraudRule_DefaultAction_IsReview()
    {
        // Arrange & Act
        var rule = new FraudRule(FraudRuleId.New(), "TestRule", 50);

        // Assert
        Assert.Equal(FraudRuleAction.Review, rule.Action);
    }

    [Fact]
    public void FraudRule_ExplicitReviewAction_IsReview()
    {
        // Arrange & Act
        var rule = new FraudRule(FraudRuleId.New(), "ReviewRule", 50, FraudRuleAction.Review);

        // Assert
        Assert.Equal(FraudRuleAction.Review, rule.Action);
    }

    [Fact]
    public void FraudRule_ExplicitRejectAction_IsReject()
    {
        // Arrange & Act
        var rule = new FraudRule(FraudRuleId.New(), "RejectRule", 70, FraudRuleAction.Reject);

        // Assert
        Assert.Equal(FraudRuleAction.Reject, rule.Action);
        Assert.Equal(70, rule.RiskScore);
    }

    [Fact]
    public void Disable_SetsIsEnabledToFalse()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act
        rule.Disable();

        // Assert
        Assert.False(rule.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledToTrue()
    {
        // Arrange
        var rule = CreateValidRule();
        rule.Disable();

        // Act
        rule.Enable();

        // Assert
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void Enable_CanReactivateDisabledRule()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act
        rule.Disable();
        rule.Enable();

        // Assert
        Assert.True(rule.IsEnabled);
    }

    [Fact]
    public void ChangeRiskScore_UpdatesCorrectly()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act
        rule.ChangeRiskScore(75);

        // Assert
        Assert.Equal(75, rule.RiskScore);
    }

    [Fact]
    public void ChangeRiskScore_LessThanZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => rule.ChangeRiskScore(-1));
    }

    [Fact]
    public void ChangeRiskScore_GreaterThan100_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => rule.ChangeRiskScore(150));
    }

    [Fact]
    public void Rename_UpdatesCorrectly()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act
        rule.Rename("CrossBorder");

        // Assert
        Assert.Equal("CrossBorder", rule.RuleName);
    }

    [Fact]
    public void Rename_Null_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => rule.Rename(null!));
    }

    [Fact]
    public void Rename_Empty_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => rule.Rename(string.Empty));
    }

    [Fact]
    public void Rename_Whitespace_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateValidRule();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => rule.Rename("   "));
    }

    [Fact]
    public void SameFraudRuleId_AreEqual()
    {
        // Arrange
        var fraudRuleId = FraudRuleId.New();
        var rule1 = new FraudRule(fraudRuleId, "HighAmount", 50);
        var rule2 = new FraudRule(fraudRuleId, "CrossBorder", 80);

        // Act & Assert
        Assert.Equal(rule1, rule2);
    }

    [Fact]
    public void DifferentFraudRuleId_AreNotEqual()
    {
        // Arrange
        var rule1 = new FraudRule(FraudRuleId.New(), "HighAmount", 50);
        var rule2 = new FraudRule(FraudRuleId.New(), "HighAmount", 50);

        // Act & Assert
        Assert.NotEqual(rule1, rule2);
    }

    private static FraudRule CreateValidRule() =>
        new(FraudRuleId.New(), "HighAmount", 50);
}
