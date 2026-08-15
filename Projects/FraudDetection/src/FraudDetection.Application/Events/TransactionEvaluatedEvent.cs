using FraudDetection.Domain.Enums;

namespace FraudDetection.Application.Events;

/// <summary>
/// Integration event published on the "transaction-evaluated" Kafka topic after
/// the anti-fraud worker applies the fraud decision to a transaction.
/// RejectionReason is null unless the transaction was rejected — it records
/// which rule caused the rejection (decision audit trail, see ADR-056).
/// </summary>
public sealed record TransactionEvaluatedEvent(
    Guid TransactionExternalId,
    TransactionStatus Status,
    RejectionReason? RejectionReason);