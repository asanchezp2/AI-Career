using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Entities;

public class TransactionTests
{
    [Fact]
    public void Transaction_CreatedSuccessfully()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        // Act
        var transaction = new Transaction(transactionId, customerId, amount);

        // Assert
        Assert.Equal(transactionId, transaction.Id);
        Assert.Equal(customerId, transaction.CustomerId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
        Assert.NotEqual(default, transaction.CreatedAt);
    }

    [Fact]
    public void Transaction_NullId_Throws()
    {
        // Arrange
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Transaction(null!, customerId, amount));
    }

    [Fact]
    public void Transaction_NullCustomerId_Throws()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var amount = new Money(100, "USD");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Transaction(transactionId, null!, amount));
    }

    [Fact]
    public void Transaction_NullMoney_Throws()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Transaction(transactionId, customerId, null!));
    }

    [Fact]
    public void Transaction_DifferentIds_AreDifferentEntities()
    {
        // Arrange
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        var transaction1 = new Transaction(TransactionId.New(), customerId, amount);
        var transaction2 = new Transaction(TransactionId.New(), customerId, amount);

        // Act & Assert
        Assert.NotEqual(transaction1, transaction2);
    }
}
