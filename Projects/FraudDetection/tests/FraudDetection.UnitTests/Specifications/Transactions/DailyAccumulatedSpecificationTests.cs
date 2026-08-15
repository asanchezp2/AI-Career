using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications.Transactions;

namespace FraudDetection.UnitTests.Specifications.Transactions;

public class DailyAccumulatedSpecificationTests
{
    private static Transaction CreateTransaction() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

    [Fact]
    public void IsSatisfiedBy_AccumulatedExactlyAtLimit_ReturnsFalse()
    {
        var specification = new DailyAccumulatedSpecification(
            DailyAccumulatedSpecification.DailyAccumulatedLimit);

        var result = specification.IsSatisfiedBy(CreateTransaction());

        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_AccumulatedJustAboveLimit_ReturnsTrue()
    {
        var specification = new DailyAccumulatedSpecification(
            DailyAccumulatedSpecification.DailyAccumulatedLimit + 0.01m);

        var result = specification.IsSatisfiedBy(CreateTransaction());

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_AccumulatedZero_ReturnsFalse()
    {
        var specification = new DailyAccumulatedSpecification(0m);

        var result = specification.IsSatisfiedBy(CreateTransaction());

        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_AccumulatedBelowLimit_ReturnsFalse()
    {
        var specification = new DailyAccumulatedSpecification(15000m);

        var result = specification.IsSatisfiedBy(CreateTransaction());

        Assert.False(result);
    }

    [Fact]
    public void Constructor_NegativeAccumulated_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DailyAccumulatedSpecification(-1m));
    }

    [Fact]
    public void IsSatisfiedBy_NullTransaction_ThrowsArgumentNullException()
    {
        var specification = new DailyAccumulatedSpecification(1000m);

        Assert.Throws<ArgumentNullException>(
            () => specification.IsSatisfiedBy(null!));
    }
}