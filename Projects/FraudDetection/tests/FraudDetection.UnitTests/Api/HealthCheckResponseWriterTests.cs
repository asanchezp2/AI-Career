using System.Text.Json;
using FraudDetection.Api.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraudDetection.UnitTests.Api;

/// <summary>
/// Unit tests for <see cref="HealthCheckResponseWriter.BuildResponse"/> — the
/// pure mapping from HealthReport to the documented JSON contract (ADR-059):
/// status/checks/totalDurationMs with per-check name/status/durationMs, plus
/// a description field ONLY for failed checks.
/// </summary>
public class HealthCheckResponseWriterTests
{
    [Fact]
    public void BuildResponse_HealthyReport_MapsTheDocumentedShape()
    {
        // Arrange
        var report = CreateReport(
            ("sqlserver", HealthyEntry(TimeSpan.FromMilliseconds(12))),
            ("kafka", HealthyEntry(TimeSpan.FromMilliseconds(45))));

        // Act
        var response = HealthCheckResponseWriter.BuildResponse(report);

        // Assert
        Assert.Equal("Healthy", response.Status);
        Assert.True(response.TotalDurationMs > 0);
        Assert.Equal(2, response.Checks.Count);

        var sqlServer = response.Checks[0];
        Assert.Equal("sqlserver", sqlServer.Name);
        Assert.Equal("Healthy", sqlServer.Status);
        Assert.Equal(12, sqlServer.DurationMs);
        Assert.Null(sqlServer.Description);

        Assert.Equal("kafka", response.Checks[1].Name);
        Assert.Equal("Healthy", response.Checks[1].Status);
    }

    [Fact]
    public void BuildResponse_UnhealthyEntry_CarriesDescription()
    {
        // Arrange
        var report = CreateReport(
            ("sqlserver", UnhealthyEntry(TimeSpan.FromMilliseconds(300), "SQL Server is unreachable")),
            ("kafka", HealthyEntry(TimeSpan.FromMilliseconds(5))));

        // Act
        var response = HealthCheckResponseWriter.BuildResponse(report);

        // Assert — a single failing check degrades the overall status
        Assert.Equal("Unhealthy", response.Status);
        Assert.Equal("Unhealthy", response.Checks[0].Status);
        Assert.Equal("SQL Server is unreachable", response.Checks[0].Description);
        Assert.Null(response.Checks[1].Description);
    }

    [Fact]
    public void BuildResponse_ExceptionMessage_BecomesDescription()
    {
        // Arrange — the Kafka check surfaces its exception instead of a description
        var report = CreateReport(
            ("kafka", new HealthReportEntry(
                HealthStatus.Unhealthy,
                description: null,
                TimeSpan.FromMilliseconds(400),
                new InvalidOperationException("Broker transport failure"),
                data: null)));

        // Act
        var response = HealthCheckResponseWriter.BuildResponse(report);

        // Assert
        Assert.Equal("Unhealthy", response.Status);
        Assert.Equal("Broker transport failure", response.Checks[0].Description);
    }

    [Fact]
    public void SerializedJson_HealthyCheckHasNoDescriptionKey()
    {
        // Arrange
        var report = CreateReport(("sqlserver", HealthyEntry(TimeSpan.FromMilliseconds(12))));

        // Act
        var json = Serialize(report);

        // Assert — wire contract: no "description" for healthy checks
        using var document = JsonDocument.Parse(json);
        var check = document.RootElement.GetProperty("checks")[0];
        Assert.False(check.TryGetProperty("description", out _));
    }

    [Fact]
    public void SerializedJson_ContractUsesCamelCaseKeys()
    {
        // Arrange
        var report = CreateReport(("sqlserver", HealthyEntry(TimeSpan.FromMilliseconds(12))));

        // Act
        var json = Serialize(report);

        // Assert — documented property names
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal(12, root.GetProperty("totalDurationMs").GetInt64());
        var check = root.GetProperty("checks")[0];
        Assert.Equal("sqlserver", check.GetProperty("name").GetString());
        Assert.Equal(12, check.GetProperty("durationMs").GetInt64());
    }

    [Fact]
    public void SerializedJson_FailedCheck_IncludesDescription()
    {
        // Arrange
        var report = CreateReport(
            ("kafka", UnhealthyEntry(TimeSpan.FromMilliseconds(40), "Broker is down")));

        // Act
        var json = Serialize(report);

        // Assert
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("status").GetString());
        var check = document.RootElement.GetProperty("checks")[0];
        Assert.Equal("Broker is down", check.GetProperty("description").GetString());
    }

    [Fact]
    public void SerializedJson_EmptyReport_MapsLivenessShape()
    {
        // Arrange — the /health/live case: no checks selected at all
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act
        var response = HealthCheckResponseWriter.BuildResponse(report);

        // Assert
        Assert.Equal("Healthy", response.Status);
        Assert.Empty(response.Checks);
    }

    /// <summary>
    /// Serializes with the same (web defaults) options the writer uses.
    /// </summary>
    private static string Serialize(HealthReport report) =>
        JsonSerializer.Serialize(
            HealthCheckResponseWriter.BuildResponse(report),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static HealthReport CreateReport(params (string Name, HealthReportEntry Entry)[] entries)
    {
        var dictionary = entries.ToDictionary(
            entry => entry.Name,
            entry => entry.Entry);
        var totalDuration = TimeSpan.FromMilliseconds(dictionary.Values.Sum(entry => entry.Duration.TotalMilliseconds));
        return new HealthReport(dictionary, totalDuration);
    }

    private static HealthReportEntry HealthyEntry(TimeSpan duration) =>
        new(HealthStatus.Healthy, description: null, duration, exception: null, data: null);

    private static HealthReportEntry UnhealthyEntry(TimeSpan duration, string description) =>
        new(HealthStatus.Unhealthy, description, duration, exception: null, data: null);
}