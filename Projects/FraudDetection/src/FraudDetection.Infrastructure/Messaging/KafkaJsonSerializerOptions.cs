using System.Text.Json;
using System.Text.Json.Serialization;

namespace FraudDetection.Infrastructure.Messaging;

/// <summary>
/// Shared JSON serialization options for Kafka messages: camelCase property
/// names (matching the HTTP wire format) and enums serialized as LOWERCASE
/// strings ("approved", "highvalue"), consistent with the database storage.
///
/// Used by both the publisher (KafkaEventPublisher) and the consumer
/// (TransactionEvaluationWorker in the Worker project).
/// </summary>
public static class KafkaJsonSerializerOptions
{
    /// <summary>
    /// The shared serializer options instance. Safe to reuse: JsonSerializerOptions
    /// is immutable once used for serialization and this instance is read-only by design.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(new LowerCaseJsonNamingPolicy()) }
    };
}