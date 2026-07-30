using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Specifications.Transactions;

public class VelocityTransactionSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_RecentCountEqualsMax_ReturnsTrue()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1));
        var transaction = CreateTransaction(recentCount: 5);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_RecentCountExceedsMax_ReturnsTrue()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1));
        var transaction = CreateTransaction(recentCount: 10);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_RecentCountBelowMax_ReturnsFalse()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1));
        var transaction = CreateTransaction(recentCount: 3);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_ZeroRecentCount_ReturnsFalse()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1));
        var transaction = CreateTransaction(recentCount: 0);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_MaxCountOneAndRecentOne_ReturnsTrue()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 1, timeWindow: TimeSpan.FromMinutes(30));
        var transaction = CreateTransaction(recentCount: 1);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 5, timeWindow: TimeSpan.FromHours(1));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => specification.IsSatisfiedBy(null!));
    }

    [Fact]
    public void Constructor_MaxCountLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new VelocityTransactionSpecification(0, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void MaxTransactionCount_Property_IsExposed()
    {
        // Arrange
        var specification = new VelocityTransactionSpecification(
            maxTransactionCount: 10, timeWindow: TimeSpan.FromHours(2));

        // Act & Assert
        Assert.Equal(10, specification.MaxTransactionCount);
        Assert.Equal(TimeSpan.FromHours(2), specification.TimeWindow);
    }

    private static Transaction CreateTransaction(int recentCount = 0)
    {
        return new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(100, "USD"),
            DateTime.UtcNow)
        {
            RecentTransactionCount = recentCount
        };
    }
}
