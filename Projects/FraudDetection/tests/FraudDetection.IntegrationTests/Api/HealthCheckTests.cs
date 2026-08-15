using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FraudDetection.Api.Health;
using FraudDetection.IntegrationTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FraudDetection.IntegrationTests.Api;

/// <summary>
/// Health probe contract tests (ADR-059):
/// - /health/live — liveness only: never evaluates dependencies, always 200.
/// - /health/ready — readiness: evaluates the real dependencies (SQL Server +
///   Kafka); 200 only when ALL are Healthy, 503 otherwise. JSON carries
///   per-dependency detail (name, status, durationMs, optional description).
/// - /health — backwards-compatible alias of /health/ready.
///
/// The test factory replaces the real SqlServer/Kafka checks with
/// always-healthy fakes (the test host has neither dependency), so the
/// default assertions here run against the "all dependencies up" state; the
/// failure test injects an Unhealthy fake to exercise the 503 path.
/// </summary>
public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // /health is the backwards-compatible alias of /health/ready
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertReadyBody(response);
    }

    [Fact]
    public async Task LiveProbe_ReturnsOkAndHealthy()
    {
        // Act — the liveness probe has no dependencies by design
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", content.GetProperty("status").GetString());
        // Liveness selects NO checks: the same response shape with an empty
        // checks array — honest about what was (not) evaluated.
        Assert.Empty(content.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task ReadinessProbe_Returns200_WithDetailedJson()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert — 200 when all (fake) dependencies report Healthy
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertReadyBody(response);
    }

    [Fact]
    public async Task ReadinessProbe_And_LivenessProbe_AreSeparateEndpoints()
    {
        // The readiness probe /health/ready evaluates dependencies; the
        // liveness probe /health/live must never fail because of a
        // dependency outage.

        // Act
        var readiness = await _client.GetAsync("/health/ready");
        var liveness = await _client.GetAsync("/health/live");

        // Assert — both return 200 against a healthy host
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
    }

    [Fact]
    public async Task ReadinessProbe_WhenDependencyIsDown_Returns503_WithErrorDescription()
    {
        // A failing SqlServer dependency must produce 503 with the failure
        // spelled out in the JSON (description field) — the honest failure
        // contract of the custom ResponseWriter.
        using var factory = new CustomWebApplicationFactory(services =>
        {
            services.RemoveAll<IConfigureOptions<HealthCheckServiceOptions>>();
            services.AddHealthChecks()
                .AddCheck(
                    instance: new FakeHealthCheck(
                        HealthCheckResult.Unhealthy("SQL Server is unreachable")),
                    name: HealthCheckNames.SqlServer,
                    tags: new[] { HealthCheckTags.Ready })
                .AddCheck(
                    instance: new FakeHealthCheck(HealthCheckResult.Healthy()),
                    name: HealthCheckNames.Kafka,
                    tags: new[] { HealthCheckTags.Ready });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unhealthy", content.GetProperty("status").GetString());
        Assert.True(content.TryGetProperty("totalDurationMs", out _));

        var checks = content.GetProperty("checks");
        Assert.Equal(2, checks.GetArrayLength());

        var sqlServer = checks.EnumerateArray()
            .First(check => check.GetProperty("name").GetString() == HealthCheckNames.SqlServer);
        Assert.Equal("Unhealthy", sqlServer.GetProperty("status").GetString());
        Assert.Equal("SQL Server is unreachable", sqlServer.GetProperty("description").GetString());

        var kafka = checks.EnumerateArray()
            .First(check => check.GetProperty("name").GetString() == HealthCheckNames.Kafka);
        Assert.Equal("Healthy", kafka.GetProperty("status").GetString());
    }

    /// <summary>
    /// Asserts the documented readiness JSON contract (ADR-059):
    /// status Healthy + checks array with sqlserver and kafka entries, each
    /// carrying status and durationMs, plus a top-level totalDurationMs.
    /// Healthy checks carry no description field.
    /// </summary>
    private static async Task AssertReadyBody(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Healthy", content.GetProperty("status").GetString());
        Assert.True(content.TryGetProperty("totalDurationMs", out _));

        var checks = content.GetProperty("checks");
        Assert.Equal(2, checks.GetArrayLength());

        var names = checks.EnumerateArray()
            .Select(check => check.GetProperty("name").GetString())
            .OrderBy(name => name)
            .ToArray();
        Assert.Equal(
            new[] { HealthCheckNames.Kafka, HealthCheckNames.SqlServer },
            names);

        foreach (var check in checks.EnumerateArray())
        {
            Assert.Equal("Healthy", check.GetProperty("status").GetString());
            Assert.True(check.GetProperty("durationMs").GetInt64() >= 0);
            // Healthy checks must NOT carry the failure-only description field
            Assert.False(check.TryGetProperty("description", out _));
        }
    }
}