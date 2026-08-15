using System.Text.Json;
using Confluent.Kafka;
using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;
using FraudDetection.Domain;
using FraudDetection.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudDetection.Infrastructure.Messaging;

/// <summary>
/// Kafka implementation of the IEventPublisher port using the direct
/// Confluent.Kafka client (no MassTransit — see ADR-053).
///
/// Serialization: JSON (System.Text.Json) with camelCase + lowercase enums.
/// Message key: the transaction's external ID, which guarantees per-transaction
/// partitioning and therefore per-transaction ordering on the consumer side.
///
/// Producer durability: Acks.All + EnableIdempotence — a message is only
/// acknowledged once it is durably persisted by the brokers, and the broker
/// deduplicates retries. Registered as a singleton; the underlying producer is
/// thread-safe.
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaTopicOptions _topics;
    private readonly ILogger<KafkaEventPublisher> _logger;

    /// <summary>
    /// Creates a new KafkaEventPublisher from the bound Kafka options.
    /// </summary>
    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNull(logger, nameof(logger));

        _topics = options.Value.Topics;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            // Fail fast in dev/demo when Kafka is unreachable instead of hanging
            // on the default 5-minute message timeout.
            MessageTimeoutMs = 10_000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <inheritdoc />
    public Task PublishAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(@event, nameof(@event));
        return ProduceAsync(
            _topics.TransactionCreated,
            @event.TransactionExternalId.ToString(),
            @event,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(TransactionEvaluatedEvent @event, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(@event, nameof(@event));
        return ProduceAsync(
            _topics.TransactionEvaluated,
            @event.TransactionExternalId.ToString(),
            @event,
            cancellationToken);
    }

    /// <summary>
    /// Serializes the message as JSON and produces it with the transaction
    /// external ID as the partitioning key.
    /// </summary>
    private async Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Serialize(message, KafkaJsonSerializerOptions.Default);

        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = value },
                cancellationToken);

            if (result.Status != PersistenceStatus.Persisted)
            {
                // Only reachable with unusual broker configs (e.g. acks=0); logged
                // rather than thrown because the message was still delivered.
                _logger.LogWarning(
                    "Message {MessageType} for transaction {TransactionExternalId} " +
                    "delivered to topic {Topic} with status {Status}",
                    typeof(T).Name,
                    key,
                    topic,
                    result.Status);
            }
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish {MessageType} for transaction {TransactionExternalId} to topic {Topic}: {ErrorReason}",
                typeof(T).Name,
                key,
                topic,
                ex.Error.Reason);
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _producer.Dispose();
}