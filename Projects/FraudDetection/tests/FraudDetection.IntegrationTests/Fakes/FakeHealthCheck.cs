using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraudDetection.IntegrationTests.Fakes;

/// <summary>
/// Deterministic <see cref="IHealthCheck"/> used by the test factory to
/// replace the real SqlServer and Kafka checks (the test host has neither
/// SQL Server nor a Kafka broker, so the real checks would always fail —
/// see ADR-059 for the test-environment strategy).
///
/// The result is fixed at construction time, so a test can inject a failing
/// result to exercise the 503 + error-description path of /health/ready.
/// </summary>
public sealed class FakeHealthCheck : IHealthCheck
{
    private readonly HealthCheckResult _result;

    public FakeHealthCheck()
        : this(HealthCheckResult.Healthy())
    {
    }

    public FakeHealthCheck(HealthCheckResult result)
    {
        _result = result;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(_result);
}