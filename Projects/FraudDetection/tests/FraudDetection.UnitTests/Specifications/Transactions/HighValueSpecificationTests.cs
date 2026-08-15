using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications.Transactions;

namespace FraudDetection.UnitTests.Specifications.Transactions;

public class HighValueSpecificationTests
{
    private static Transaction CreateTransaction(decimal value) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, value);

    [Fact]
    public void IsSatisfiedBy_ValueExactlyAtLimit_ReturnsFalse()
    {
        var transaction = CreateTransaction(HighValueSpecification.HighValueLimit);

        var result = new HighValueSpecification().IsSatisfiedBy(transaction);

        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_ValueJustAboveLimit_ReturnsTrue()
    {
        var transaction = CreateTransaction(HighValueSpecification.HighValueLimit + 0.01m);

        var result = new HighValueSpecification().IsSatisfiedBy(transaction);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_SmallValue_ReturnsFalse()
    {
        var transaction = CreateTransaction(100m);

        var result = new HighValueSpecification().IsSatisfiedBy(transaction);

        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_WellAboveLimit_ReturnsTrue()
    {
        var transaction = CreateTransaction(5000m);

        var result = new HighValueSpecification().IsSatisfiedBy(transaction);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NullTransaction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new HighValueSpecification().IsSatisfiedBy(null!));
    }
}