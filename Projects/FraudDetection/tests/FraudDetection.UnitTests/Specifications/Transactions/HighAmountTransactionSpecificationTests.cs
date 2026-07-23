using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Specifications.Transactions;

public class HighAmountTransactionSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_AmountBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var transaction = CreateTransaction(5000);
        var specification = new HighAmountTransactionSpecification(10000);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_AmountEqualToThreshold_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(10000);
        var specification = new HighAmountTransactionSpecification(10000);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_AmountAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(15000);
        var specification = new HighAmountTransactionSpecification(10000);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_DifferentThresholds_ProducesDifferentResults()
    {
        // Arrange
        var transaction = CreateTransaction(7500);
        var lowerThreshold = new HighAmountTransactionSpecification(5000);
        var higherThreshold = new HighAmountTransactionSpecification(10000);

        // Act
        var lowerResult = lowerThreshold.IsSatisfiedBy(transaction);
        var higherResult = higherThreshold.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(lowerResult);   // 7500 >= 5000
        Assert.False(higherResult); // 7500 < 10000
    }

    [Fact]
    public void Constructor_NegativeThreshold_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new HighAmountTransactionSpecification(-1));
        Assert.Contains("Threshold", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsSatisfiedBy_ZeroThreshold_AnyAmountReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(0);
        var specification = new HighAmountTransactionSpecification(0);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_ThresholdZeroAndAmountPositive_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(1);
        var specification = new HighAmountTransactionSpecification(0);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var specification = new HighAmountTransactionSpecification(10000);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => specification.IsSatisfiedBy(null!));
    }

    [Fact]
    public void IsSatisfiedBy_WithDifferentCurrencies_ComparesAmountOnly()
    {
        // Arrange
        var transactionUsd = CreateTransaction(10000, "USD");
        var transactionEur = CreateTransaction(10000, "EUR");
        var specification = new HighAmountTransactionSpecification(10000);

        // Act
        var resultUsd = specification.IsSatisfiedBy(transactionUsd);
        var resultEur = specification.IsSatisfiedBy(transactionEur);

        // Assert
        Assert.True(resultUsd);
        Assert.True(resultEur);
    }

    private static Transaction CreateTransaction(decimal amount, string currency = "USD")
    {
        return new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(amount, currency));
    }
}
