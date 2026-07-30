using System.Net;
using System.Net.Http.Json;

namespace FraudDetection.IntegrationTests.Api;

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
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(content);
        Assert.True(content!.ContainsKey("status"));
        Assert.Equal("healthy", content["status"]?.ToString());
        Assert.True(content.ContainsKey("timestamp"));
    }
}
