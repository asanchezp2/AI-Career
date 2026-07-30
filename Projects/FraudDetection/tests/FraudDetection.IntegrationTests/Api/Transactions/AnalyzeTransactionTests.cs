using System.Net;
using System.Net.Http.Json;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

namespace FraudDetection.IntegrationTests.Api.Transactions;

public class AnalyzeTransactionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnalyzeTransactionTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ValidLowAmountTransaction_Returns200AndApproved()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal(transactionId, result!.TransactionId);
        Assert.Equal("Approved", result.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task Post_ValidHighAmountTransaction_Returns200AndUnderReview()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 50000,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal(transactionId, result!.TransactionId);
        Assert.Equal("UnderReview", result.Status);
        Assert.Equal(50, result.TotalRiskScore);
    }

    [Fact]
    public async Task Post_HighRiskCountryTransaction_Returns200AndUnderReview()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            Country = "IR", // High-risk country
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal(transactionId, result!.TransactionId);
        // HighRiskCountry rule has risk score 30 and action Review -> UnderReview
        Assert.Equal("UnderReview", result.Status);
        Assert.Equal(30, result.TotalRiskScore);
    }

    [Fact]
    public async Task Post_EmptyTransactionId_Returns400()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.Empty,
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyCustomerId_Returns400()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.Empty,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_NegativeAmount_Returns400()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = -100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidCurrencyLength_Returns400()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = "INVALID",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyCurrency_Returns400()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100,
            Currency = string.Empty,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ResponseContainsMatchedRules()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 50000,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("transactionId", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("statusCode", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("totalRiskScore", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("matchedRules", content, StringComparison.OrdinalIgnoreCase);
    }
}
