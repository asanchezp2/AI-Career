using FraudDetection.Domain.Enums;

namespace FraudDetection.Application.Features.Transactions.EvaluateTransaction;

/// <summary>
/// The result of an EvaluateTransaction command execution: the applied status
/// and — when rejected — the rule that caused the rejection.
/// </summary>
public sealed record EvaluateTransactionResult(
    Guid TransactionExternalId,
    TransactionStatus Status,
    RejectionReason? RejectionReason);