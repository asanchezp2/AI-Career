using FraudDetection.Application.Events;

namespace FraudDetection.Application.Abstractions;

/// <summary>
/// Publishes integration events to the asynchronous messaging infrastructure
/// (Kafka). Abstraction in the Application layer — implementation in
/// Infrastructure (KafkaEventPublisher).
///
/// The port is strongly typed per event, so topic names and serialization
/// details stay inside the adapter: the Application layer knows nothing about
/// Kafka. See ADR-053.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes the TransactionCreated event (produced by the API when a
    /// transaction is created; consumed by the anti-fraud worker).
    /// </summary>
    Task PublishAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the TransactionEvaluated event (produced by the anti-fraud
    /// worker after evaluation; updates the transaction state downstream).
    /// </summary>
    Task PublishAsync(TransactionEvaluatedEvent @event, CancellationToken cancellationToken = default);
}