using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace FraudDetection.IntegrationTests.Api;

public class RateLimitTests
{
    private sealed record TransactionPayload(
        Guid SourceAccountId,
        Guid TargetAccountId,
        int TransferTypeId,
        decimal Value);

    [Fact]
    public async Task Post_ThirdRequestWithinWindow_Returns429WithRetryAfter()
    {
        using var factory = new CustomWebApplicationFactory(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:PermitLimit"] = "2",
                ["RateLimit:WindowSeconds"] = "60"
            }));

        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/transactions", CreatePayload());
        var second = await client.PostAsJsonAsync("/api/v1/transactions", CreatePayload());
        var third = await client.PostAsJsonAsync("/api/v1/transactions", CreatePayload());

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Contains("application/problem+json", third.Content.Headers.ContentType!.ToString());
        Assert.NotNull(third.Headers.RetryAfter);
    }

    private static TransactionPayload CreatePayload() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 1, 120m);
}