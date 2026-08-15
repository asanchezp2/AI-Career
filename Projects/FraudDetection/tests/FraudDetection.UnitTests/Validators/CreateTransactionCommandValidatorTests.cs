using System.Text.Json;
using FraudDetection.Application.Features.Transactions.CreateTransaction;

namespace FraudDetection.UnitTests.Validators;

public class CreateTransactionCommandValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    private static CreateTransactionCommand CreateCommand(
        Guid? sourceAccountId = null,
        Guid? targetAccountId = null,
        int transferTypeId = 1,
        decimal value = 100m) =>
        new()
        {
            SourceAccountId = sourceAccountId ?? Guid.NewGuid(),
            TargetAccountId = targetAccountId ?? Guid.NewGuid(),
            TransferTypeId = transferTypeId,
            Value = value
        };

    [Fact]
    public async Task Validate_ValidCommand_IsValid()
    {
        var result = await _validator.ValidateAsync(CreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_LiteralChallengePayload_IsValid()
    {
        // The exact payload from the real challenge document (Challenge_BE-LT.docx),
        // including its literal `tranferTypeId` spelling. Contract fidelity: this
        // must deserialize and pass validation (the API returns 201 for it).
        const string challengePayload =
            """
            {
              "sourceAccountId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
              "targetAccountId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
              "tranferTypeId": 1,
              "value": 120
            }
            """;

        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(challengePayload);

        Assert.NotNull(command);
        Assert.Equal(1, command!.TransferTypeId);

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_MissingSourceAccountId_Fails()
    {
        var result = await _validator.ValidateAsync(CreateCommand(sourceAccountId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateTransactionCommand.SourceAccountId));
    }

    [Fact]
    public async Task Validate_MissingTargetAccountId_Fails()
    {
        var result = await _validator.ValidateAsync(CreateCommand(targetAccountId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateTransactionCommand.TargetAccountId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_NonPositiveTransferTypeId_Fails(int transferTypeId)
    {
        var result = await _validator.ValidateAsync(CreateCommand(transferTypeId: transferTypeId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateTransactionCommand.TransferTypeId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Validate_NonPositiveValue_Fails(decimal value)
    {
        var result = await _validator.ValidateAsync(CreateCommand(value: value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateTransactionCommand.Value));
    }
}