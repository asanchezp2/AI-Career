namespace FraudDetection.Domain.Enums;

/// <summary>
/// Defines the action to take when a fraud rule is matched.
/// </summary>
public enum FraudRuleAction
{
    /// <summary>
    /// The transaction should be flagged for manual or automated review.
    /// </summary>
    Review,

    /// <summary>
    /// The transaction should be automatically rejected.
    /// </summary>
    Reject
}
