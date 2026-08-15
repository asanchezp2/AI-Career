namespace FraudDetection.Application.Configuration;

/// <summary>
/// Configuration options for API rate limiting, bound from the "RateLimit" section.
/// Lives in the Application layer because the limits are a cross-cutting API policy
/// with config-driven values (project convention: no hardcoded business numbers).
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Maximum number of requests allowed within the window for the limited endpoint.
    /// </summary>
    public int PermitLimit { get; set; } = 30;

    /// <summary>
    /// Length of the fixed window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
