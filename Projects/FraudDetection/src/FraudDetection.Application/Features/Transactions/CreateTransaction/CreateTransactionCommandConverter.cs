using System.Text.Json;
using System.Text.Json.Serialization;

namespace FraudDetection.Application.Features.Transactions.CreateTransaction;

/// <summary>
/// (De)serializes a <see cref="CreateTransactionCommand"/>.
///
/// <para>
/// The real challenge document (Challenge_BE-LT.docx) defines the create payload
/// with the literal field name <c>tranferTypeId</c> (the challenge spells it with
/// a typo — no 's' after "tran"). A client following the document verbatim sends
/// <c>tranferTypeId</c>, so that MUST bind. We also accept the correctly-spelled
/// <c>transferTypeId</c> for robustness: a real client may use either spelling.
/// </para>
///
/// <para>
/// A per-property <c>[JsonConverter]</c> cannot achieve this dual-name binding:
/// in System.Text.Json a converter attached to a property converts that property's
/// <em>value</em>; the JSON <em>property-name</em> matching is owned by the
/// serializer's property resolution (which is case-insensitive but not typo-tolerant).
/// Accepting both names therefore requires intercepting the object-level bind, so
/// the converter lives on the command type. Resolution rules:
/// <list type="bullet">
/// <item>Property names are matched case-insensitively (preserving the host's
/// web-default case-insensitive behavior for every field).</item>
/// <item><c>tranferTypeId</c> takes precedence; <c>transferTypeId</c> is accepted as
/// an alias when <c>tranferTypeId</c> is absent.</item>
/// <item>Missing fields bind to default values (Guid.Empty / 0) so the
/// <see cref="CreateTransactionValidator"/> returns a 400 as before.</item>
/// <item>Values present but of the wrong shape throw <see cref="JsonException"/>
/// (surfaced as a 400 by the framework), matching pre-existing behavior.</item>
/// </list>
/// </para>
/// </summary>
public sealed class CreateTransactionCommandConverter : JsonConverter<CreateTransactionCommand>
{
    /// <summary>
    /// The challenge's literal field name (source of truth; typo preserved).
    /// </summary>
    private const string ChallengeTransferTypeId = "tranferTypeId";

    /// <summary>
    /// The correctly-spelled alias, accepted for robustness.
    /// </summary>
    private const string CorrectTransferTypeId = "transferTypeId";

    public override CreateTransactionCommand Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        return new CreateTransactionCommand
        {
            SourceAccountId = ReadGuid(root, "sourceAccountId"),
            TargetAccountId = ReadGuid(root, "targetAccountId"),
            TransferTypeId = ReadTransferTypeId(root),
            Value = ReadDecimal(root, "value")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        CreateTransactionCommand value,
        JsonSerializerOptions options)
    {
        // The canonical wire representation follows the challenge document.
        writer.WriteStartObject();
        writer.WriteString("sourceAccountId", value.SourceAccountId);
        writer.WriteString("targetAccountId", value.TargetAccountId);
        writer.WriteNumber(ChallengeTransferTypeId, value.TransferTypeId);
        writer.WriteNumber("value", value.Value);
        writer.WriteEndObject();
    }

    private static Guid ReadGuid(JsonElement root, string name)
    {
        if (!TryGetProperty(root, name, out var property) ||
            property.ValueKind != JsonValueKind.String)
            return Guid.Empty;

        if (property.TryGetGuid(out var guid))
            return guid;

        throw new JsonException($"The '{name}' property must be a valid GUID.");
    }

    private static int ReadTransferTypeId(JsonElement root)
    {
        foreach (var name in new[] { ChallengeTransferTypeId, CorrectTransferTypeId })
        {
            if (!TryGetProperty(root, name, out var property) ||
                property.ValueKind != JsonValueKind.Number)
                continue;

            if (property.TryGetInt32(out var transferTypeId))
                return transferTypeId;

            throw new JsonException($"The '{name}' property must be a valid 32-bit integer.");
        }

        return 0;
    }

    private static decimal ReadDecimal(JsonElement root, string name)
    {
        if (!TryGetProperty(root, name, out var property) ||
            property.ValueKind != JsonValueKind.Number)
            return 0m;

        if (property.TryGetDecimal(out var value))
            return value;

        throw new JsonException($"The '{name}' property must be a valid decimal.");
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}