using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using FraudDetection.Api.Endpoints;

namespace FraudDetection.UnitTests.Api;

/// <summary>
/// Unit tests for <see cref="VersionResponse.FromAssembly"/> — the pure
/// mapping from assembly metadata to the GET /api/v1/version wire contract
/// (ADR-059). Dynamic assemblies keep the tests hermetic: the real API
/// assembly's attributes change with the build environment (e.g. the SDK
/// appends the git hash to the informational version when building inside
/// a git work tree).
/// </summary>
public class VersionResponseTests
{
    [Fact]
    public void FromAssembly_MapsVersionInformationalVersionAndEnvironment()
    {
        // Arrange
        var assembly = BuildTestAssembly(
            informationalVersion: "2.0.0-beta+abc123",
            sourceRevisionId: null);

        // Act
        var response = VersionResponse.FromAssembly(assembly, "Production");

        // Assert
        Assert.Equal("1.2.3.4", response.Version);
        Assert.Equal("2.0.0-beta+abc123", response.InformationalVersion);
        Assert.Equal("Production", response.Environment);
        Assert.Null(response.Commit);
    }

    [Fact]
    public void FromAssembly_WithoutInformationalVersion_FallsBackToAssemblyVersion()
    {
        // Arrange — no informational version attribute on the assembly
        var assembly = BuildTestAssembly(informationalVersion: null, sourceRevisionId: null);

        // Act
        var response = VersionResponse.FromAssembly(assembly, "Development");

        // Assert
        Assert.Equal("1.2.3.4", response.InformationalVersion);
        Assert.Equal("Development", response.Environment);
    }

    [Fact]
    public void FromAssembly_WithSourceRevisionId_IncludesCommit()
    {
        // Arrange — the graceful path: a build made with
        // -p:SourceRevisionId=<sha> carries the AssemblyMetadata attribute
        var sha = "8f3a1c2b9d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a";
        var assembly = BuildTestAssembly(informationalVersion: "1.0.0", sourceRevisionId: sha);

        // Act
        var response = VersionResponse.FromAssembly(assembly, "Production");

        // Assert
        Assert.Equal(sha, response.Commit);
    }

    [Fact]
    public void SerializedJson_FollowsTheDocumentedWireContract()
    {
        // Arrange
        var sha = "abc123def456";
        var response = VersionResponse.FromAssembly(
            BuildTestAssembly("1.0.0", sha),
            "Production");

        // Act
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert — camelCase keys, commit present
        using var document = JsonDocument.Parse(json);
        Assert.Equal("1.2.3.4", document.RootElement.GetProperty("version").GetString());
        Assert.Equal("1.0.0", document.RootElement.GetProperty("informationalVersion").GetString());
        Assert.Equal("Production", document.RootElement.GetProperty("environment").GetString());
        Assert.Equal(sha, document.RootElement.GetProperty("commit").GetString());
    }

    [Fact]
    public void SerializedJson_OmitsCommit_WhenAbsent()
    {
        // Arrange
        var response = VersionResponse.FromAssembly(
            BuildTestAssembly("1.0.0", sourceRevisionId: null),
            "Production");

        // Act
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert — the "commit" key is absent, not null, when the build has
        // no SourceRevisionId
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("commit", out _));
    }

    private static Assembly BuildTestAssembly(string? informationalVersion, string? sourceRevisionId)
    {
        var assemblyName = new AssemblyName("FakeVersionAssembly")
        {
            Version = new Version(1, 2, 3, 4)
        };
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName, AssemblyBuilderAccess.Run);

        if (informationalVersion is not null)
        {
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(AssemblyInformationalVersionAttribute)
                    .GetConstructor(new[] { typeof(string) })!,
                new object[] { informationalVersion }));
        }

        if (sourceRevisionId is not null)
        {
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(AssemblyMetadataAttribute)
                    .GetConstructor(new[] { typeof(string), typeof(string) })!,
                new object[] { "SourceRevisionId", sourceRevisionId }));
        }

        return assembly;
    }
}