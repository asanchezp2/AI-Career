using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraudDetection.Api.Health;

/// <summary>
/// JSON response writer for the health check endpoints (wire contract, ADR-059):
///
/// <code>
/// {
///   "status": "Healthy",
///   "checks": [
///     { "name": "sqlserver", "status": "Healthy", "durationMs": 12 },
///     { "name": "kafka", "status": "Healthy", "durationMs": 45 }
///   ],
///   "totalDurationMs": 57
/// }
/// </code>
///
/// A check that failed carries an extra <c>"description"</c> field with the
/// error message (or the underlying exception message); the field is omitted
/// for healthy checks. /health/live (which selects no checks) produces the
/// same shape with an empty <c>checks</c> array.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// ASP.NET HealthCheckOptions.ResponseWriter entry point: serializes the
    /// report using the documented contract.
    /// </summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(
            BuildResponse(report),
            JsonOptions,
            context.RequestAborted);
    }

    /// <summary>
    /// Maps a <see cref="HealthReport"/> to the documented wire contract.
    /// Pure function — unit-tested in FraudDetection.UnitTests.
    /// </summary>
    public static HealthReportResponse BuildResponse(HealthReport report) => new(
        report.Status.ToString(),
        report.Entries
            .Select(entry => new HealthCheckResponse(
                entry.Key,
                entry.Value.Status.ToString(),
                (long)Math.Ceiling(entry.Value.Duration.TotalMilliseconds),
                entry.Value.Description ?? entry.Value.Exception?.Message))
            .ToList(),
        (long)Math.Ceiling(report.TotalDuration.TotalMilliseconds));
}

/// <summary>
/// Top-level shape of the health check JSON response.
/// </summary>
public sealed record HealthReportResponse(
    string Status,
    IReadOnlyList<HealthCheckResponse> Checks,
    long TotalDurationMs);

/// <summary>
/// Per-dependency entry of the health check JSON response. <see cref="Description"/>
/// carries the error message when the check failed and is omitted otherwise.
/// </summary>
public sealed record HealthCheckResponse(
    string Name,
    string Status,
    long DurationMs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Description = null);