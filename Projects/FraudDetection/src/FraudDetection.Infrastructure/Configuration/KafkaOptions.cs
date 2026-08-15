namespace FraudDetection.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the Kafka messaging infrastructure, bound from the
/// "Kafka" section of appsettings.json (or environment variables such as
/// KAFKA__BOOTSTRAPSERVERS).
///
/// Lives in Infrastructure because Kafka is an infrastructure concern — the
/// Application layer only sees the IEventPublisher port (see ADR-053).
/// Validated at startup by <see cref="KafkaOptionsValidator"/>.
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Comma-separated list of Kafka broker addresses (host:port).
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Consumer group ID used by the anti-fraud worker.
    /// </summary>
    public string GroupId { get; set; } = "fraud-detection-worker";

    /// <summary>
    /// Where the consumer starts reading when no committed offset exists:
    /// "Earliest" (replay from the beginning — dev-friendly) or "Latest".
    /// </summary>
    public string AutoOffsetReset { get; set; } = "Earliest";

    /// <summary>
    /// Topic names for the integration events.
    /// </summary>
    public KafkaTopicOptions Topics { get; set; } = new();
}

/// <summary>
/// Kafka topic names bound from the "Kafka:Topics" section.
/// </summary>
public sealed class KafkaTopicOptions
{
    /// <summary>
    /// Topic for TransactionCreated events (produced by the API, consumed by the worker).
    /// </summary>
    public string TransactionCreated { get; set; } = "transaction-created";

    /// <summary>
    /// Topic for TransactionEvaluated events (produced by the worker).
    /// </summary>
    public string TransactionEvaluated { get; set; } = "transaction-evaluated";
}