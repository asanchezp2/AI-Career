using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.Services;

namespace FraudDetection.UnitTests.Services;

public class FraudRuleEngineTests
{
    private readonly FraudRuleEngine _engine = new();

    private static Transaction CreateTransaction(decimal value) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, value);

    [Fact]
    public void Evaluate_NoRuleMatches_ReturnsApproved()
    {
        var result = _engine.Evaluate(CreateTransaction(100m), dailyAccumulatedAmount: 1000m);

        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void Evaluate_HighValueOnly_ReturnsRejectedWithHighValue()
    {
        var result = _engine.Evaluate(CreateTransaction(2500m), dailyAccumulatedAmount: 1000m);

        Assert.Equal(TransactionStatus.Rejected, result.RecommendedStatus);
        Assert.Equal(RejectionReason.HighValue, result.RejectionReason);
    }

    [Fact]
    public void Evaluate_DailyAccumulatedOnly_ReturnsRejectedWithDailyAccumulated()
    {
        var result = _engine.Evaluate(CreateTransaction(100m), dailyAccumulatedAmount: 25000m);

        Assert.Equal(TransactionStatus.Rejected, result.RecommendedStatus);
        Assert.Equal(RejectionReason.DailyAccumulated, result.RejectionReason);
    }

    [Fact]
    public void Evaluate_BothRulesMatch_ReturnsHighValuePrecedence()
    {
        // Both rules match (value > 2000 and accumulated > 20000); the high-value
        // rule is evaluated first, so it wins (documented precedence, ADR-057).
        var result = _engine.Evaluate(CreateTransaction(5000m), dailyAccumulatedAmount: 50000m);

        Assert.Equal(TransactionStatus.Rejected, result.RecommendedStatus);
        Assert.Equal(RejectionReason.HighValue, result.RejectionReason);
    }

    [Fact]
    public void Evaluate_ValueExactlyAtHighValueLimit_ReturnsApproved()
    {
        var result = _engine.Evaluate(CreateTransaction(2000m), dailyAccumulatedAmount: 1000m);

        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void Evaluate_AccumulatedExactlyAtDailyLimit_ReturnsApproved()
    {
        var result = _engine.Evaluate(CreateTransaction(100m), dailyAccumulatedAmount: 20000m);

        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void Evaluate_ZeroAccumulatedWithLowValue_ReturnsApproved()
    {
        var result = _engine.Evaluate(CreateTransaction(100m), dailyAccumulatedAmount: 0m);

        Assert.Equal(TransactionStatus.Approved, result.RecommendedStatus);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void Evaluate_NullTransaction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _engine.Evaluate(null!, dailyAccumulatedAmount: 0m));
    }
}