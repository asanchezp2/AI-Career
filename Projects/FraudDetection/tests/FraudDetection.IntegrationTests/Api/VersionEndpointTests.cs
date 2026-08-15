using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FraudDetection.IntegrationTests.Api;

/// <summary>
/// Contract tests for GET /api/v1/version (ADR-059): reports the running
/// build's version metadata without any domain/application involvement.
/// </summary>
public class VersionEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VersionEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task VersionEndpoint_Returns200_WithExpectedShape()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(string.IsNullOrWhiteSpace(content.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(content.GetProperty("informationalVersion").GetString()));
        Assert.Equal("Testing", content.GetProperty("environment").GetString());

        // "commit" is present ONLY when the assembly carries a
        // SourceRevisionId (a build made with -p:SourceRevisionId=...).
        // Local and Docker builds do not — so it must be omitted from the
        // JSON entirely; when a build DOES inject it, it must be non-empty.
        // The omit/present contract itself is unit-tested deterministically
        // (VersionResponseTests with dynamic assemblies).
        if (content.TryGetProperty("commit", out var commit))
            Assert.False(string.IsNullOrWhiteSpace(commit.GetString()));
    }
}