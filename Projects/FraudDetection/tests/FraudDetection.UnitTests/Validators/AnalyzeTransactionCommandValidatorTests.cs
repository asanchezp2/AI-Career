using FluentValidation.TestHelper;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

namespace FraudDetection.UnitTests.Validators;

public class AnalyzeTransactionCommandValidatorTests
{
    private readonly AnalyzeTransactionValidator _validator = new();

    [Fact]
    public async Task Valid_Command_PassesValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Timestamp_Required_ReturnsValidationError()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timestamp)
            .WithErrorMessage("Timestamp is required.");
    }

    [Fact]
    public async Task Empty_TransactionId_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.Empty,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorMessage("Transaction ID is required.");
    }

    [Fact]
    public async Task Empty_CustomerId_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.Empty,
            Amount = 100,
            Currency = "USD"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required.");
    }

    [Fact]
    public async Task Amount_Zero_PassesValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 0,
            Currency = "USD"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public async Task Amount_Negative_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = -100,
            Currency = "USD"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than or equal to zero.");
    }

    [Fact]
    public async Task Empty_Currency_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = string.Empty
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required.");
    }

    [Fact]
    public async Task Currency_LengthLessThanThree_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "US"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be exactly 3 characters.");
    }

    [Fact]
    public async Task Currency_LengthMoreThanThree_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USDX"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be exactly 3 characters.");
    }

    [Fact]
    public async Task Currency_Lowercase_FailsValidation()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "usd"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be uppercase.");
    }
}
