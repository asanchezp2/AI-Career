using System.Reflection;
using System.Text.Json.Serialization;

namespace FraudDetection.Api.Endpoints;

/// <summary>
/// Maps GET /api/v1/version — a composition-root-only endpoint (no domain or
/// application involvement) that reports the running build's version metadata
/// for operational diagnostics (ADR-059).
/// </summary>
public static class VersionEndpoint
{
    /// <summary>
    /// Maps the GET /api/v1/version endpoint.
    /// </summary>
    public static void MapVersion(this WebApplication app)
    {
        app.MapGet("/api/v1/version", (IWebHostEnvironment environment) =>
            Results.Ok(VersionResponse.FromAssembly(
                typeof(VersionEndpoint).Assembly,
                environment.EnvironmentName)))
        .WithName("GetVersion")
        .Produces<VersionResponse>(StatusCodes.Status200OK)
        .WithDescription("Returns the API assembly version, its informational version, and " +
                         "the current hosting environment name. A \"commit\" field with the " +
                         "SourceRevisionId is included when the assembly was built with " +
                         "-p:SourceRevisionId=&lt;sha&gt; — local and Docker builds omit it " +
                         "because the git folder lies outside the Docker build context (ADR-059).")
        .WithOpenApi();
    }
}

/// <summary>
/// Version metadata for GET /api/v1/version, serialized camelCase:
/// <c>{ "version", "informationalVersion", "environment", "commit"? }</c>.
/// </summary>
public sealed record VersionResponse(
    string Version,
    string InformationalVersion,
    string Environment,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Commit = null)
{
    /// <summary>
    /// Maps assembly version metadata to the wire contract. Pure function —
    /// unit-tested in FraudDetection.UnitTests with dynamic assemblies.
    /// </summary>
    public static VersionResponse FromAssembly(Assembly assembly, string environment)
    {
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? version;
        var commit = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute =>
                string.Equals(attribute.Key, "SourceRevisionId", StringComparison.Ordinal))
            ?.Value;

        return new VersionResponse(version, informationalVersion, environment, commit);
    }
}