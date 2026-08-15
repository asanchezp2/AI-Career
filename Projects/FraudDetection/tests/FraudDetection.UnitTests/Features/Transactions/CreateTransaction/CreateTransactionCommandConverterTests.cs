using System.Text.Json;
using FraudDetection.Application.Features.Transactions.CreateTransaction;

namespace FraudDetection.UnitTests.Features.Transactions.CreateTransaction;

/// <summary>
/// Verifies the challenge-contract fidelity decision: the create payload binds
/// the challenge's literal field name <c>tranferTypeId</c> (typo preserved) while
/// also accepting the correctly-spelled <c>transferTypeId</c> alias — both
/// case-insensitively. See <see cref="CreateTransactionCommandConverter"/>.
/// </summary>
public class CreateTransactionCommandConverterTests
{
    private static readonly Guid SourceAccountId = Guid.Parse("3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b");
    private static readonly Guid TargetAccountId = Guid.Parse("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d");

    private static string Payload(string transferTypeField, string transferTypeValue = "1") =>
        $$"""
        {
          "sourceAccountId": "{{SourceAccountId}}",
          "targetAccountId": "{{TargetAccountId}}",
          "{{transferTypeField}}": {{transferTypeValue}},
          "value": 120
        }
        """;

    [Fact]
    public void Deserialize_ChallengeSpellingTranferTypeId_BindsTransferTypeId()
    {
        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(
            Payload("tranferTypeId"));

        Assert.NotNull(command);
        Assert.Equal(SourceAccountId, command!.SourceAccountId);
        Assert.Equal(TargetAccountId, command.TargetAccountId);
        Assert.Equal(1, command.TransferTypeId);
        Assert.Equal(120m, command.Value);
    }

    [Fact]
    public void Deserialize_CorrectSpellingTransferTypeId_BindsTransferTypeId()
    {
        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(
            Payload("transferTypeId"));

        Assert.NotNull(command);
        Assert.Equal(1, command!.TransferTypeId);
        Assert.Equal(120m, command.Value);
    }

    [Theory]
    [InlineData("TRANFERTYPEID")]
    [InlineData("TransferTypeId")]
    [InlineData("tranfertypeid")]
    public void Deserialize_MixedCaseSpelling_StillBinds(string fieldName)
    {
        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(
            Payload(fieldName));

        Assert.NotNull(command);
        Assert.Equal(1, command!.TransferTypeId);
    }

    [Fact]
    public void Deserialize_BothSpellings_ChallengeSpellingWins()
    {
        var json = $$"""
            {
              "sourceAccountId": "{{SourceAccountId}}",
              "targetAccountId": "{{TargetAccountId}}",
              "tranferTypeId": 7,
              "transferTypeId": 9,
              "value": 120
            }
            """;

        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(json);

        Assert.NotNull(command);
        Assert.Equal(7, command!.TransferTypeId);
    }

    [Fact]
    public void Deserialize_MissingTransferType_BindsZero()
    {
        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(
            Payload("unrelatedField"));

        Assert.NotNull(command);
        Assert.Equal(0, command!.TransferTypeId);
    }

    [Fact]
    public void Deserialize_MissingRequiredFields_BindsDefaults()
    {
        var command = JsonSerializer.Deserialize<CreateTransactionCommand>("{}");

        Assert.NotNull(command);
        Assert.Equal(Guid.Empty, command!.SourceAccountId);
        Assert.Equal(Guid.Empty, command.TargetAccountId);
        Assert.Equal(0, command.TransferTypeId);
        Assert.Equal(0m, command.Value);
    }

    [Fact]
    public void Deserialize_InvalidGuid_ThrowsJsonException()
    {
        var json = """{ "sourceAccountId": "not-a-guid", "value": 120 }""";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateTransactionCommand>(json));
    }

    [Fact]
    public void Serialize_Command_WritesChallengeSpelling()
    {
        var command = new CreateTransactionCommand
        {
            SourceAccountId = SourceAccountId,
            TargetAccountId = TargetAccountId,
            TransferTypeId = 1,
            Value = 120m
        };

        var json = JsonSerializer.Serialize(command);

        Assert.Contains("\"tranferTypeId\":1", json);
        Assert.DoesNotContain("\"transferTypeId\"", json);
    }

    [Fact]
    public void RoundTrip_DeserializeThenSerialize_PreservesCommand()
    {
        var command = JsonSerializer.Deserialize<CreateTransactionCommand>(
            Payload("transferTypeId"));

        var roundTripped = JsonSerializer.Deserialize<CreateTransactionCommand>(
            JsonSerializer.Serialize(command));

        Assert.NotNull(roundTripped);
        Assert.Equal(command!.SourceAccountId, roundTripped!.SourceAccountId);
        Assert.Equal(command.TargetAccountId, roundTripped.TargetAccountId);
        Assert.Equal(command.TransferTypeId, roundTripped.TransferTypeId);
        Assert.Equal(command.Value, roundTripped.Value);
    }
}