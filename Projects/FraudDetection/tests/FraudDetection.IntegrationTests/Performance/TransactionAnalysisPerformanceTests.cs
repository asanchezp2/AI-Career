using System.Diagnostics;
using System.Net.Http.Json;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

namespace FraudDetection.IntegrationTests.Performance;

/// <summary>
/// Performance tests verifying response times stay under 100ms.
/// These run against SQLite in-memory (not SQL Server), so results are
/// indicative but not production guarantees.
/// </summary>
public class TransactionAnalysisPerformanceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionAnalysisPerformanceTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnalyzeTransaction_CompletesUnder100ms()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 15000m,
            Currency = "USD",
            Timestamp = DateTime.UtcNow,
            Country = "US"
        };

        // Act
        var sw = Stopwatch.StartNew();
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);
        sw.Stop();

        // Assert
        Assert.True(response.IsSuccessStatusCode,
            $"Request failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Request took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task AnalyzeTransaction_VelocityScenario_CompletesUnder100ms()
    {
        // Arrange — seed 5 transactions for the same customer
        var customerId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            var seedCommand = new AnalyzeTransactionCommand
            {
                TransactionId = Guid.NewGuid(),
                CustomerId = customerId,
                Amount = 100m,
                Currency = "USD",
                Timestamp = DateTime.UtcNow,
                Country = "US"
            };
            await _client.PostAsJsonAsync("/api/v1/transactions/analyze", seedCommand);
        }

        // Act — 6th transaction triggers velocity check and queries history
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId,
            Amount = 100m,
            Currency = "USD",
            Timestamp = DateTime.UtcNow,
            Country = "US"
        };

        var sw = Stopwatch.StartNew();
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);
        sw.Stop();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Velocity scenario took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task HealthCheck_CompletesUnder100ms()
    {
        // Act
        var sw = Stopwatch.StartNew();
        var response = await _client.GetAsync("/health");
        sw.Stop();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Health check took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task GetTransaction_CompletesUnder100ms()
    {
        // Arrange — first create a transaction
        var transactionId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Timestamp = DateTime.UtcNow,
            Country = "US"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);
        var result = await createResponse.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);

        // Act — measure GET
        var sw = Stopwatch.StartNew();
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{result!.TransactionId}");
        sw.Stop();

        // Assert
        Assert.True(getResponse.IsSuccessStatusCode);
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"GET took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }
}
