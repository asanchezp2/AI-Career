using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Enums;

namespace FraudDetection.UnitTests.Features.Transactions.AnalyzeTransaction;

public class AnalyzeTransactionHandlerTests
{
    private readonly AnalyzeTransactionHandler _handler = new();

    [Fact]
    public void Handle_ValidCommand_ReturnsResultWithCorrectTransactionId()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 250.50m,
            Currency = "USD"
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Equal(transactionId, result.TransactionId);
    }

    [Fact]
    public void Handle_ValidCommand_ReturnsPendingTransaction()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "EUR"
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        // The Handler creates a Transaction in its initial state (Pending).
        // No fraud evaluation exists yet — status changes will come with FraudRuleEngine.
        Assert.Equal(TransactionStatus.Pending, result.Status);
    }

    [Fact]
    public void Handle_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = -50,
            Currency = "USD"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _handler.Handle(command));
        Assert.Contains("Amount", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Handle_WithEmptyTransactionId_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.Empty,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(command));
    }

    [Fact]
    public void Handle_WithEmptyCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.Empty,
            Amount = 100,
            Currency = "USD"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(command));
    }

    [Fact]
    public void Handle_WithInvalidCurrency_ThrowsArgumentException()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "InvalidCurrency"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(command));
    }

    [Fact]
    public void Handle_ZeroAmount_DomainAcceptsValue()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 0,
            Currency = "USD"
        };

        // Act & Assert
        // Zero amount passes the Handler because Amount > 0 is a validation rule
        // in the Application Layer (FluentValidation), not a Domain invariant.
        // Money only rejects negative amounts.
        var result = _handler.Handle(command);
        Assert.Equal(TransactionStatus.Pending, result.Status);
    }
}
