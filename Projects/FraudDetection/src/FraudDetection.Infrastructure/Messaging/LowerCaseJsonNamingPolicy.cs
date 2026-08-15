using System.Text.Json;

namespace FraudDetection.Infrastructure.Messaging;

/// <summary>
/// JSON naming policy that converts a name to lowercase. Used by the Kafka
/// serializer so enum values are emitted as LOWERCASE strings
/// ("approved", "highvalue") — consistent with the HTTP wire format and the
/// database storage. .NET 8 does not ship <see cref="JsonNamingPolicy.LowerCase"/>
/// (added in .NET 9), hence this tiny policy. Reading remains case-insensitive.
/// </summary>
public sealed class LowerCaseJsonNamingPolicy : JsonNamingPolicy
{
    /// <inheritdoc />
    public override string ConvertName(string name) => name.ToLowerInvariant();
}