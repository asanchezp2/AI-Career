using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Entities;

public class BlacklistedCustomerTests
{
    [Fact]
    public void BlacklistedCustomer_CreatedSuccessfully()
    {
        // Arrange
        var customerId = CustomerId.New();

        // Act
        var customer = new BlacklistedCustomer(customerId, "Fraud detected");

        // Assert
        Assert.Equal(customerId, customer.CustomerId);
        Assert.Equal("Fraud detected", customer.Reason);
        Assert.NotEqual(default, customer.CreatedAt);
    }

    [Fact]
    public void BlacklistedCustomer_NullCustomerId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new BlacklistedCustomer(null!, "Fraud detected"));
    }

    [Fact]
    public void BlacklistedCustomer_NullReason_ThrowsArgumentException()
    {
        // Arrange
        var customerId = CustomerId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new BlacklistedCustomer(customerId, null!));
    }

    [Fact]
    public void BlacklistedCustomer_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var customerId = CustomerId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new BlacklistedCustomer(customerId, string.Empty));
    }

    [Fact]
    public void BlacklistedCustomer_WhitespaceReason_ThrowsArgumentException()
    {
        // Arrange
        var customerId = CustomerId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new BlacklistedCustomer(customerId, "   "));
    }
}
