namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Represents the result of an AnalyzeTransaction command execution.
/// </summary>
public class AnalyzeTransactionResult
{
    /// <summary>
    /// The unique identifier of the analyzed transaction.
    /// </summary>
    public Guid TransactionId { get; }

    /// <summary>
    /// The numeric status code of the transaction after analysis.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// The string representation of the transaction status after analysis.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// The total risk score calculated by the fraud rule engine.
    /// Zero indicates no risk detected.
    /// </summary>
    public int TotalRiskScore { get; }

    /// <summary>
    /// The names of the fraud rules that were matched during analysis.
    /// </summary>
    public IReadOnlyCollection<string> MatchedRules { get; }

    /// <summary>
    /// Creates a new AnalyzeTransactionResult instance.
    /// </summary>
    public AnalyzeTransactionResult(
        Guid transactionId,
        int statusCode,
        string status,
        int totalRiskScore = 0,
        IReadOnlyCollection<string>? matchedRules = null)
    {
        TransactionId = transactionId;
        StatusCode = statusCode;
        Status = status;
        TotalRiskScore = totalRiskScore;
        MatchedRules = matchedRules ?? Array.Empty<string>();
    }
}
