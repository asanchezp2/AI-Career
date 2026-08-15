namespace FraudDetection.Application.Events;

/// <summary>
/// Integration event published on the "transaction-created" Kafka topic whenever
/// the API persists a new transaction (always in Pending status).
/// Consumed by the anti-fraud worker, which evaluates it and publishes a
/// TransactionEvaluatedEvent back.
///
/// The event carries the full creation snapshot so the consumer never needs to
/// re-read the payload from the API — only the transaction row (for the status
/// transition) and the day's accumulated sum.
/// </summary>
public sealed record TransactionCreatedEvent(
    Guid TransactionExternalId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    int TransferTypeId,
    decimal Value,
    DateTime CreatedAt);