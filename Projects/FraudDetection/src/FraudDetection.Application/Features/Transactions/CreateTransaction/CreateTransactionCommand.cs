using System.Text.Json.Serialization;

namespace FraudDetection.Application.Features.Transactions.CreateTransaction;

/// <summary>
/// Represents a command to create a financial transaction.
/// Maps 1:1 to the challenge's Resource 1 payload:
/// <c>{ "sourceAccountId": "Guid", "targetAccountId": "Guid", "tranferTypeId": 1, "value": 120 }</c>.
///
/// The transaction external ID and the creation timestamp are server-generated —
/// they are not part of the request (see ADR-058).
/// </summary>
[JsonConverter(typeof(CreateTransactionCommandConverter))]
public class CreateTransactionCommand
{
    /// <summary>
    /// The account that funds the transaction.
    /// </summary>
    public Guid SourceAccountId { get; init; }

    /// <summary>
    /// The account that receives the transaction.
    /// </summary>
    public Guid TargetAccountId { get; init; }

    /// <summary>
    /// The transfer type identifier (must be greater than zero).
    ///
    /// <see cref="JsonPropertyNameAttribute"/> declares the challenge's literal
    /// wire name — the real challenge document spells this field with the typo
    /// "tranferTypeId" (missing the 's'). The correctly-spelled variant is also
    /// accepted at (de)serialization time by <see cref="CreateTransactionCommandConverter"/>,
    /// which prefers the challenge spelling when both are present. See the
    /// converter for the rationale.
    /// </summary>
    [JsonPropertyName("tranferTypeId")]
    public int TransferTypeId { get; init; }

    /// <summary>
    /// The monetary value of the transaction (must be greater than zero).
    /// </summary>
    public decimal Value { get; init; }
}