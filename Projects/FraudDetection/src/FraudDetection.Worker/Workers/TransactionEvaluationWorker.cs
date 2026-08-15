using System.Text.Json;
using Confluent.Kafka;
using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Events;
using FraudDetection.Application.Features.Transactions.EvaluateTransaction;
using FraudDetection.Domain;
using FraudDetection.Infrastructure.Configuration;
using FraudDetection.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace FraudDetection.Worker.Workers;

/// <summary>
/// The anti-fraud consumer: a BackgroundService that subscribes to the
/// TransactionCreated topic and, for every message:
///   1. deserializes the TransactionCreatedEvent,
///   2. evaluates the transaction via the EvaluateTransactionHandler
///      (Application layer — loads the row, computes the day's accumulated
///      value, runs the fraud rules, persists the status transition),
///   3. publishes the TransactionEvaluated event,
///   4. commits the offset.
///
/// Delivery semantics (documented in ADR-058): at-least-once. The offset is
/// committed ONLY after the evaluation is persisted and the evaluated event is
/// published. On a crash in between, the message is redelivered — the handler
/// is idempotent (an already-evaluated transaction replays its current state).
/// Processing exceptions are logged and NOT committed (retried on the next
/// poll); unparseable poison messages are logged, committed, and skipped so a
/// single bad message cannot wedge the consumer.
///
/// Scoped dependencies (DbContext + handler) are resolved per message via
/// IServiceScopeFactory: the worker itself is a singleton hosted service and
/// must never hold a scoped DbContext.
/// </summary>
public sealed class TransactionEvaluationWorker : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TransactionEvaluationWorker> _logger;

    /// <summary>
    /// Creates a new TransactionEvaluationWorker with the required dependencies.
    /// </summary>
    public TransactionEvaluationWorker(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        IEventPublisher eventPublisher,
        ILogger<TransactionEvaluationWorker> logger)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNull(scopeFactory, nameof(scopeFactory));
        Guard.AgainstNull(eventPublisher, nameof(eventPublisher));
        Guard.AgainstNull(logger, nameof(logger));

        _options = options.Value;
        _scopeFactory = scopeFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_options.AutoOffsetReset),
            // Manual commits: the offset is committed only after the evaluation
            // is persisted AND TransactionEvaluated is published (at-least-once).
            EnableAutoCommit = false,
            // Dev/demo convenience: missing topics are created automatically by
            // the broker (AUTO_CREATE_TOPICS_ENABLE=true in docker-compose).
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.Topics.TransactionCreated);

        _logger.LogInformation(
            "Transaction evaluation worker subscribed to topic {Topic} (group {GroupId})",
            _options.Topics.TransactionCreated,
            _options.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                }
                catch (ConsumeException ex)
                {
                    // Transient broker/connection errors — log and keep polling.
                    // The consumer stays subscribed; recovery happens on retry.
                    _logger.LogError(ex, "Kafka consume error: {ErrorReason}", ex.Error.Reason);
                    continue;
                }

                if (result is null || stoppingToken.IsCancellationRequested)
                    continue;

                try
                {
                    await ProcessMessageAsync(consumer, result, stoppingToken);
                    _logger.LogDebug(
                        "Processed and committed offset {Offset} for transaction {TransactionExternalId}",
                        result.Offset,
                        result.Message.Key);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw; // shutdown requested — exit the loop without logging an error
                }
                catch (Exception ex)
                {
                    // Processing failed. Deliberately NOT committed: the message
                    // is redelivered on the next poll — at-least-once semantics.
                    _logger.LogError(
                        ex,
                        "Failed to process message for transaction {TransactionExternalId} " +
                        "at offset {Offset}; it will be retried on the next poll",
                        result.Message.Key,
                        result.Offset);
                }
            }
        }
        finally
        {
            // Graceful leave-group so the partition is rebalanced cleanly
            // instead of waiting for the session timeout.
            consumer.Close();
            _logger.LogInformation("Transaction evaluation worker stopped");
        }
    }

    /// <summary>
    /// Processes a single consumed message: deserialize → evaluate → publish →
    /// commit. Throws on processing failures so the caller can decide whether
    /// the offset is committed.
    /// </summary>
    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        TransactionCreatedEvent created;
        try
        {
            created = JsonSerializer.Deserialize<TransactionCreatedEvent>(
                result.Message.Value,
                KafkaJsonSerializerOptions.Default)
                ?? throw new JsonException("Deserialized message is null.");
        }
        catch (JsonException ex)
        {
            // Poison message: unparseable payload. Committing and skipping is
            // deliberate — retrying can never succeed and would wedge the
            // consumer. Logged loudly for operator investigation.
            _logger.LogError(
                ex,
                "Poison message on topic {Topic} at offset {Offset} — committing and skipping",
                result.Topic,
                result.Offset);
            consumer.Commit(result);
            return;
        }

        // Evaluate in a per-message scope: the handler and its DbContext are
        // scoped services; the hosted service itself is a singleton.
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<EvaluateTransactionHandler>();

        var evaluation = await handler.Handle(
            new EvaluateTransactionCommand { TransactionExternalId = created.TransactionExternalId },
            cancellationToken);

        if (evaluation is null)
        {
            // The transaction row does not exist in the database (queried
            // successfully — the DB was reachable). Retrying cannot help, so
            // commit and skip.
            _logger.LogWarning(
                "Transaction {TransactionExternalId} not found in database — committing and skipping",
                created.TransactionExternalId);
            consumer.Commit(result);
            return;
        }

        await _eventPublisher.PublishAsync(
            new TransactionEvaluatedEvent(
                evaluation.TransactionExternalId,
                evaluation.Status,
                evaluation.RejectionReason),
            cancellationToken);

        // The evaluation is persisted (UpdateAsync inside the handler) and the
        // evaluated event is published. Only now is the offset committed —
        // at-least-once semantics (ADR-058): a crash before this point leaves
        // the offset uncommitted and the message is redelivered (idempotent).
        consumer.Commit(result);
    }
}