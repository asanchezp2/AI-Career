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
        var transaction = new Transaction(transactionId, customerId, amount, DateTime.UtcNow);

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
        Assert.Throws<ArgumentNullException>(() => new Transaction(null!, customerId, amount, DateTime.UtcNow));
    }

    [Fact]
    public void Transaction_NullCustomerId_Throws()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var amount = new Money(100, "USD");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Transaction(transactionId, null!, amount, DateTime.UtcNow));
    }

    [Fact]
    public void Transaction_NullMoney_Throws()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Transaction(transactionId, customerId, null!, DateTime.UtcNow));
    }

    [Fact]
    public void Transaction_DifferentIds_AreDifferentEntities()
    {
        // Arrange
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        var transaction1 = new Transaction(TransactionId.New(), customerId, amount, DateTime.UtcNow);
        var transaction2 = new Transaction(TransactionId.New(), customerId, amount, DateTime.UtcNow);

        // Act & Assert
        Assert.NotEqual(transaction1, transaction2);
    }

    [Fact]
    public void Pending_To_Approved()
    {
        // Arrange
        var transaction = CreatePendingTransaction();

        // Act
        var result = transaction.Approve();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
    }

    [Fact]
    public void Pending_To_Rejected()
    {
        // Arrange
        var transaction = CreatePendingTransaction();

        // Act
        var result = transaction.Reject();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
    }

    [Fact]
    public void Pending_To_UnderReview()
    {
        // Arrange
        var transaction = CreatePendingTransaction();

        // Act
        var result = transaction.MarkForReview();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.UnderReview, transaction.Status);
    }

    [Fact]
    public void Approved_CannotChangeAgain()
    {
        // Arrange
        var transaction = CreatePendingTransaction();
        var approveResult = transaction.Approve();
        Assert.True(approveResult.IsSuccess);

        // Act & Assert
        var rejectResult = transaction.Reject();
        Assert.True(rejectResult.IsFailure);
        Assert.Contains("Approved", rejectResult.Error);

        var reviewResult = transaction.MarkForReview();
        Assert.True(reviewResult.IsFailure);
        Assert.Contains("Approved", reviewResult.Error);
    }

    [Fact]
    public void Rejected_CannotChangeAgain()
    {
        // Arrange
        var transaction = CreatePendingTransaction();
        var rejectResult = transaction.Reject();
        Assert.True(rejectResult.IsSuccess);

        // Act & Assert
        var approveResult = transaction.Approve();
        Assert.True(approveResult.IsFailure);
        Assert.Contains("Rejected", approveResult.Error);

        var reviewResult = transaction.MarkForReview();
        Assert.True(reviewResult.IsFailure);
        Assert.Contains("Rejected", reviewResult.Error);
    }

    [Fact]
    public void UnderReview_CannotChangeAgain()
    {
        // Arrange
        var transaction = CreatePendingTransaction();
        var reviewResult = transaction.MarkForReview();
        Assert.True(reviewResult.IsSuccess);

        // Act & Assert
        var approveResult = transaction.Approve();
        Assert.True(approveResult.IsFailure);
        Assert.Contains("UnderReview", approveResult.Error);

        var rejectResult = transaction.Reject();
        Assert.True(rejectResult.IsFailure);
        Assert.Contains("UnderReview", rejectResult.Error);
    }

    [Fact]
    public void Transaction_WithValidCountry_StoresCountry()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        // Act
        var transaction = new Transaction(transactionId, customerId, amount, DateTime.UtcNow, country: "US");

        // Assert
        Assert.Equal("US", transaction.Country);
    }

    [Fact]
    public void Transaction_WithNullCountry_Allowed()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        // Act
        var transaction = new Transaction(transactionId, customerId, amount, DateTime.UtcNow, country: null);

        // Assert
        Assert.Null(transaction.Country);
    }

    [Fact]
    public void Transaction_WithWhitespaceCountry_ThrowsArgumentException()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new Transaction(transactionId, customerId, amount, DateTime.UtcNow, country: "   "));
    }

    [Fact]
    public void Transaction_DefaultMetadata_IsEmpty()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");

        // Act
        var transaction = new Transaction(transactionId, customerId, amount, DateTime.UtcNow);

        // Assert
        Assert.NotNull(transaction.Metadata);
        Assert.Empty(transaction.Metadata);
    }

    [Fact]
    public void Transaction_WithMetadata_StoresItems()
    {
        // Arrange
        var transactionId = TransactionId.New();
        var customerId = CustomerId.New();
        var amount = new Money(100, "USD");
        var metadata = new Dictionary<string, string>
        {
            ["source"] = "web",
            ["channel"] = "mobile"
        };

        // Act
        var transaction = new Transaction(transactionId, customerId, amount, DateTime.UtcNow, metadata: metadata);

        // Assert
        Assert.Equal(2, transaction.Metadata.Count);
        Assert.Equal("web", transaction.Metadata["source"]);
        Assert.Equal("mobile", transaction.Metadata["channel"]);
    }

    [Fact]
    public void Transaction_Metadata_CanAddItemsAfterConstruction()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(100, "USD"),
            DateTime.UtcNow);

        // Act
        transaction.Metadata["ip_address"] = "192.168.1.1";
        transaction.Metadata["user_agent"] = "Mozilla/5.0";

        // Assert
        Assert.Equal(2, transaction.Metadata.Count);
        Assert.Equal("192.168.1.1", transaction.Metadata["ip_address"]);
        Assert.Equal("Mozilla/5.0", transaction.Metadata["user_agent"]);
    }

    private static Transaction CreatePendingTransaction() =>
        new(TransactionId.New(), CustomerId.New(), new Money(100, "USD"), DateTime.UtcNow);
}
