using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;

namespace FraudDetection.IntegrationTests.Fakes;

/// <summary>
/// In-memory IEventPublisher used by the integration test suite in place of the
/// real KafkaEventPublisher. Records every published event so tests can assert
/// the messaging contract (shape, fields, topic direction) without a broker.
/// The API only ever publishes TransactionCreated events; TransactionEvaluated
/// events are produced by the worker, which is not exercised here.
/// </summary>
public sealed class RecordingEventPublisher : IEventPublisher
{
    private readonly List<object> _published = new();

    /// <summary>
    /// All published integration events, in publish order.
    /// </summary>
    public IReadOnlyList<object> Published => _published;

    /// <summary>
    /// The TransactionCreated events published so far.
    /// </summary>
    public IReadOnlyList<TransactionCreatedEvent> CreatedEvents =>
        _published.OfType<TransactionCreatedEvent>().ToList();

    /// <summary>
    /// The TransactionEvaluated events published so far.
    /// </summary>
    public IReadOnlyList<TransactionEvaluatedEvent> EvaluatedEvents =>
        _published.OfType<TransactionEvaluatedEvent>().ToList();

    /// <inheritdoc />
    public Task PublishAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _published.Add(@event);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PublishAsync(TransactionEvaluatedEvent @event, CancellationToken cancellationToken = default)
    {
        _published.Add(@event);
        return Task.CompletedTask;
    }
}