using System.Net;
using System.Net.Http.Json;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Application.Features.Transactions.GetTransaction;

namespace FraudDetection.IntegrationTests.Api.Transactions;

/// <summary>
/// Integration tests for the GET /api/v1/transactions/{id} endpoint.
/// Transactions are created through the analyze endpoint, then retrieved by id.
/// </summary>
public class GetTransactionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetTransactionTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTransaction_ReturnsTransaction_WhenExists()
    {
        // Arrange — create a transaction via the analyze endpoint
        var transactionId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var amount = 250.50m;
        var postResponse = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = customerId,
            Amount = amount,
            Currency = "USD",
            Country = "US",
            Timestamp = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{transactionId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<GetTransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal(transactionId, result!.TransactionId);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(amount, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("US", result.Country);
        Assert.Equal("Approved", result.Status);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddMinutes(-5),
            $"CreatedAt {result.CreatedAt} should be recent");
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public async Task GetTransaction_ReturnsNotFound_WhenMissing()
    {
        // Arrange — a random GUID that was never created
        var missingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/transactions/{missingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_Returns404_WhenInvalidId()
    {
        // Act — "not-a-guid" does not match the {id:guid} route constraint
        var response = await _client.GetAsync("/api/v1/transactions/not-a-guid");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_ReturnsMetadata_WhenTransactionHasMetadata()
    {
        // Arrange — create a transaction with metadata
        var transactionId = Guid.NewGuid();
        var metadata = new Dictionary<string, string>
        {
            ["device"] = "mobile",
            ["channel"] = "web"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow,
            Metadata = metadata
        });
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{transactionId}");

        // Assert — metadata round-trips through persistence and JSON serialization
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<GetTransactionResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Metadata);
        Assert.Equal(2, result.Metadata.Count);
        Assert.Equal("mobile", result.Metadata["device"]);
        Assert.Equal("web", result.Metadata["channel"]);
    }

    [Fact]
    public async Task GetTransaction_IncludesCountry_WhenProvided()
    {
        // Arrange — create a transaction with a country
        var transactionId = Guid.NewGuid();
        var postResponse = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            Country = "US",
            Timestamp = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{transactionId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<GetTransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal("US", result!.Country);
    }
}
