namespace FraudDetection.Infrastructure.Configuration;

/// <summary>
/// Configuration options for fraud rule parameters, bound from the "FraudRules" section.
/// </summary>
public class FraudRuleOptions
{
    public const string SectionName = "FraudRules";

    /// <summary>
    /// Transaction amounts at or above this value trigger the HighAmount rule.
    /// </summary>
    public decimal HighAmountThreshold { get; set; } = 10000m;

    /// <summary>
    /// Maximum number of transactions allowed within the velocity window.
    /// </summary>
    public int VelocityMaxTransactions { get; set; } = 5;

    /// <summary>
    /// Length of the velocity detection window in minutes.
    /// </summary>
    public int VelocityWindowMinutes { get; set; } = 60;

    /// <summary>
    /// ISO 3166-1 alpha-2 country codes considered high-risk.
    /// </summary>
    public string[] HighRiskCountries { get; set; } = { "IR", "KP", "SY", "VE" };
}
