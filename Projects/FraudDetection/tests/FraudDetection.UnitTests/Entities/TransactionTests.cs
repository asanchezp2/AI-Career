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

    [Fact]
    public void Pending_To_Approved()
    {
        // Arrange
        var transaction = CreatePendingTransaction();

        // Act
        transaction.Approve();

        // Assert
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
    }

    [Fact]
    public void Pending_To_Rejected()
    {
        // Arrange
        var transaction = CreatePendingTransaction();

        // Act
        transaction.Reject();

        // Assert
        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
    }

    [Fact]
    public void Pending_To_UnderReview()
    {
        // Arrange
        var transaction = CreatePendingTransaction();

        // Act
        transaction.MarkForReview();

        // Assert
        Assert.Equal(TransactionStatus.UnderReview, transaction.Status);
    }

    [Fact]
    public void Approved_CannotChangeAgain()
    {
        // Arrange
        var transaction = CreatePendingTransaction();
        transaction.Approve();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transaction.Reject());
        Assert.Throws<InvalidOperationException>(() => transaction.MarkForReview());
    }

    [Fact]
    public void Rejected_CannotChangeAgain()
    {
        // Arrange
        var transaction = CreatePendingTransaction();
        transaction.Reject();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transaction.Approve());
        Assert.Throws<InvalidOperationException>(() => transaction.MarkForReview());
    }

    [Fact]
    public void UnderReview_CannotChangeAgain()
    {
        // Arrange
        var transaction = CreatePendingTransaction();
        transaction.MarkForReview();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transaction.Approve());
        Assert.Throws<InvalidOperationException>(() => transaction.Reject());
    }

    private static Transaction CreatePendingTransaction() =>
        new(TransactionId.New(), CustomerId.New(), new Money(100, "USD"));
}
