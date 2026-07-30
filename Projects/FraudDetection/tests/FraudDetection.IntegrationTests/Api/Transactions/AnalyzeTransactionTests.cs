using System.Net;
using System.Net.Http.Json;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FraudDetection.IntegrationTests.Api.Transactions;

public class AnalyzeTransactionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AnalyzeTransactionTests(WebApplicationFactory<Program> factory)
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
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal(transactionId, result!.TransactionId);
        Assert.Equal(TransactionStatus.Approved, result.Status);
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
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal(transactionId, result!.TransactionId);
        Assert.Equal(TransactionStatus.UnderReview, result.Status);
        Assert.Equal(50, result.TotalRiskScore);
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
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

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
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

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
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

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
            Currency = "INVALID"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

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
            Currency = string.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ResponseDoesNotExposeDomainEntities()
    {
        // Arrange
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 50000,
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions/analyze", command);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("transactionId", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("totalRiskScore", content, StringComparison.OrdinalIgnoreCase);
        // Domain entities must not leak into the API response
        Assert.DoesNotContain("matchedRules", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fraudRule", content, StringComparison.OrdinalIgnoreCase);
    }
}
