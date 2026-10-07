using System.Text.Json;
using Confluent.Kafka;
using FraudDetection.Application.Events;
using FraudDetection.Application.Features.Transactions.ApplyTransactionEvaluation;
using FraudDetection.Domain;
using FraudDetection.Infrastructure.Configuration;
using FraudDetection.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace FraudDetection.Api.Messaging;

/// <summary>
/// Consumes the anti-fraud worker's response topic and applies each response to
/// the transaction database. Offsets are committed only after persistence (or
/// after a permanent poison/invalid message is logged and skipped).
/// </summary>
public sealed class KafkaTransactionEvaluatedConsumer : BackgroundService
{
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaTransactionEvaluatedConsumer> _logger;

    public KafkaTransactionEvaluatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaTransactionEvaluatedConsumer> logger)
    {
        Guard.AgainstNull(options, nameof(options));
        Guard.AgainstNull(scopeFactory, nameof(scopeFactory));
        Guard.AgainstNull(logger, nameof(logger));

        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let Generic Host finish startup before the synchronous Kafka poll waits for a message.
        await Task.Yield();
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.EvaluationResultGroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_options.AutoOffsetReset),
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.Topics.TransactionEvaluated);

        _logger.LogInformation(
            "API status updater subscribed to topic {Topic} (group {GroupId})",
            _options.Topics.TransactionEvaluated,
            _options.EvaluationResultGroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(PollTimeout);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka response consume error: {ErrorReason}", ex.Error.Reason);
                    await Task.Delay(RetryDelay, stoppingToken);
                    continue;
                }

                if (result is null)
                {
                    await Task.Yield();
                    continue;
                }

                try
                {
                    await ProcessMessageAsync(consumer, result, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Seek back explicitly: continuing to consume and committing
                    // a later record would otherwise skip this failed response.
                    _logger.LogError(
                        ex,
                        "Failed to apply worker response at offset {Offset}; retrying",
                        result.Offset);
                    consumer.Seek(new TopicPartitionOffset(result.TopicPartition, result.Offset));
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("API status updater stopped");
        }
    }

    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        TransactionEvaluatedEvent? evaluated;
        try
        {
            evaluated = JsonSerializer.Deserialize<TransactionEvaluatedEvent>(
                result.Message.Value,
                KafkaJsonSerializerOptions.Default);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Poison response on topic {Topic} at offset {Offset}; committing and skipping",
                result.Topic,
                result.Offset);
            consumer.Commit(result);
            return;
        }

        if (evaluated is null)
        {
            _logger.LogError(
                "Null response on topic {Topic} at offset {Offset}; committing and skipping",
                result.Topic,
                result.Offset);
            consumer.Commit(result);
            return;
        }

        if (!Guid.TryParse(result.Message.Key, out var key)
            || key != evaluated.TransactionExternalId)
        {
            _logger.LogError(
                "Response key does not match its transaction ID at topic {Topic}, offset {Offset}; " +
                "committing and skipping",
                result.Topic,
                result.Offset);
            consumer.Commit(result);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ApplyTransactionEvaluationHandler>();
        var application = await handler.Handle(evaluated, cancellationToken);

        if (application.IsFailure)
        {
            // Invalid, unknown, or conflicting responses cannot be fixed by
            // redelivery. Keep the poison-message policy consistent with the
            // evaluation worker and let operators inspect the logged reason.
            _logger.LogError(
                "Worker response rejected at topic {Topic}, offset {Offset}: {Reason}; " +
                "committing and skipping",
                result.Topic,
                result.Offset,
                application.Error);
        }

        consumer.Commit(result);
    }
}
