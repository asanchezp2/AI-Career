using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Specifications.Transactions;

public class BlacklistCustomerSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_BlacklistedCustomer_ReturnsTrue()
    {
        // Arrange
        var blacklistedCustomerId = CustomerId.New();
        var specification = new BlacklistCustomerSpecification(new[] { blacklistedCustomerId });
        var transaction = CreateTransaction(blacklistedCustomerId);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NonBlacklistedCustomer_ReturnsFalse()
    {
        // Arrange
        var blacklistedCustomerId = CustomerId.New();
        var differentCustomerId = CustomerId.New();
        var specification = new BlacklistCustomerSpecification(new[] { blacklistedCustomerId });
        var transaction = CreateTransaction(differentCustomerId);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_EmptyBlacklist_ReturnsFalse()
    {
        // Arrange
        var specification = new BlacklistCustomerSpecification(Array.Empty<CustomerId>());
        var transaction = CreateTransaction(CustomerId.New());

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_MultipleBlacklistedCustomers_MatchesCorrectly()
    {
        // Arrange
        var blacklist = new[]
        {
            CustomerId.New(),
            CustomerId.New(),
            CustomerId.New()
        };
        var specification = new BlacklistCustomerSpecification(blacklist);
        var transaction = CreateTransaction(blacklist[1]); // second customer is blacklisted

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var specification = new BlacklistCustomerSpecification(Array.Empty<CustomerId>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => specification.IsSatisfiedBy(null!));
    }

    [Fact]
    public void Constructor_NullBlacklistedCustomers_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new BlacklistCustomerSpecification(null!));
    }

    private static Transaction CreateTransaction(CustomerId customerId)
    {
        return new Transaction(
            TransactionId.New(),
            customerId,
            new Money(100, "USD"),
            DateTime.UtcNow);
    }
}
