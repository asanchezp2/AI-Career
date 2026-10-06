namespace FraudDetection.Api.Health;

/// <summary>
/// Tag constants used by the health check registrations and the endpoint
/// predicates (ARCHITECTURE.md). Kept in a single place so Program.cs (which
/// registers the real checks) and the integration test factory (which
/// replaces them with fakes) cannot drift apart.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Tag of the real-dependency checks that the /health/ready predicate
    /// selects (and /health/live intentionally never selects).
    /// </summary>
    public const string Ready = "ready";
}

/// <summary>
/// Check names as they appear in the /health/ready JSON response
/// (ARCHITECTURE.md). Stable public names — the integration tests and the
/// response contract depend on them.
/// </summary>
public static class HealthCheckNames
{
    /// <summary>The SQL Server connectivity check.</summary>
    public const string SqlServer = "sqlserver";

    /// <summary>The Kafka broker connectivity check.</summary>
    public const string Kafka = "kafka";
}