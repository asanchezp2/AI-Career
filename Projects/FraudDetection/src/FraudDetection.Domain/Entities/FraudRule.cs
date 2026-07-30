using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Domain.Entities;

/// <summary>
/// Represents a fraud detection rule with configurable risk scoring.
/// </summary>
public class FraudRule
{
    /// <summary>
    /// EF Core parameterless constructor (used for materialization only).
    /// </summary>
    private FraudRule() { Id = null!; RuleName = null!; }

    /// <summary>
    /// The unique identifier of this fraud rule.
    /// </summary>
    public FraudRuleId Id { get; private set; }

    /// <summary>
    /// The name of this fraud rule.
    /// </summary>
    public string RuleName { get; private set; }

    /// <summary>
    /// The risk score assigned by this rule (0–100).
    /// </summary>
    public int RiskScore { get; private set; }

    /// <summary>
    /// Whether this rule is currently enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Creates a new FraudRule instance.
    /// </summary>
    /// <param name="id">The unique rule identifier.</param>
    /// <param name="ruleName">The name of the rule.</param>
    /// <param name="riskScore">The risk score (0–100).</param>
    /// <exception cref="ArgumentNullException">Thrown when id is null.</exception>
    /// <exception cref="ArgumentException">Thrown when ruleName is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when riskScore is outside the 0–100 range.</exception>
    public FraudRule(FraudRuleId id, string ruleName, int riskScore)
    {
        ArgumentNullException.ThrowIfNull(id);
        ValidateRuleName(ruleName);
        ValidateRiskScore(riskScore);

        Id = id;
        RuleName = ruleName;
        RiskScore = riskScore;
        IsEnabled = true;
    }

    /// <summary>
    /// Enables this rule.
    /// </summary>
    public void Enable() => IsEnabled = true;

    /// <summary>
    /// Disables this rule.
    /// </summary>
    public void Disable() => IsEnabled = false;

    /// <summary>
    /// Changes the risk score of this rule.
    /// </summary>
    /// <param name="newRiskScore">The new risk score (0–100).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when newRiskScore is outside the 0–100 range.</exception>
    public void ChangeRiskScore(int newRiskScore)
    {
        ValidateRiskScore(newRiskScore);
        RiskScore = newRiskScore;
    }

    /// <summary>
    /// Renames this rule.
    /// </summary>
    /// <param name="newName">The new name for the rule.</param>
    /// <exception cref="ArgumentException">Thrown when newName is null, empty, or whitespace.</exception>
    public void Rename(string newName)
    {
        ValidateRuleName(newName);
        RuleName = newName;
    }

    /// <summary>
    /// Validates that the risk score is within the 0–100 range.
    /// </summary>
    private static void ValidateRiskScore(int score)
    {
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(nameof(score), score, "Risk score must be between 0 and 100.");
    }

    /// <summary>
    /// Validates that the rule name is not null, empty, or whitespace.
    /// </summary>
    private static void ValidateRuleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name cannot be null, empty, or whitespace.", nameof(name));
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current FraudRule.
    /// Two rules are equal if and only if they have the same FraudRuleId.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is FraudRule other && Id.Equals(other.Id);

    /// <summary>
    /// Returns the hash code of this FraudRule based on its FraudRuleId.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Determines whether two FraudRule instances are equal.
    /// </summary>
    public static bool operator ==(FraudRule? left, FraudRule? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two FraudRule instances are not equal.
    /// </summary>
    public static bool operator !=(FraudRule? left, FraudRule? right) =>
        !Equals(left, right);
}
