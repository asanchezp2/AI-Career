using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;

namespace FraudDetection.UnitTests.Fakes;

/// <summary>
/// In-memory IEventPublisher for Application-layer unit tests. Records every
/// published event and optionally appends to a shared operation log so tests
/// can assert invocation order against the repository fake.
/// </summary>
public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _published = new();
    private readonly List<string> _operationLog;

    public FakeEventPublisher(List<string>? operationLog = null)
    {
        _operationLog = operationLog ?? new List<string>();
    }

    public IReadOnlyList<object> Published => _published;

    public IReadOnlyList<TransactionCreatedEvent> CreatedEvents =>
        _published.OfType<TransactionCreatedEvent>().ToList();

    public IReadOnlyList<TransactionEvaluatedEvent> EvaluatedEvents =>
        _published.OfType<TransactionEvaluatedEvent>().ToList();

    public IReadOnlyList<string> OperationLog => _operationLog;

    public Task PublishAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _operationLog.Add(nameof(PublishAsync));
        _published.Add(@event);
        return Task.CompletedTask;
    }

    public Task PublishAsync(TransactionEvaluatedEvent @event, CancellationToken cancellationToken = default)
    {
        _operationLog.Add(nameof(PublishAsync));
        _published.Add(@event);
        return Task.CompletedTask;
    }
}