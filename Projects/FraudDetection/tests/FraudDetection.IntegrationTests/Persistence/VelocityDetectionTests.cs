using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetection.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for velocity-based fraud detection through the full API stack.
/// Uses the CustomWebApplicationFactory with a shared in-memory SQLite database.
/// Each test isolates data by using unique CustomerId values.
/// </summary>
public class VelocityDetectionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VelocityDetectionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task NoRecentTransactions_ReturnsApproved()
    {
        // Arrange — no seeding for this customer
        var customerId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId,
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
        Assert.Equal("Approved", result!.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task BelowThreshold_NotRejectedByVelocity()
    {
        // Arrange — seed 3 transactions (below velocity threshold of 5)
        var customerId = CustomerId.New();
        await SeedTransactions(customerId, 3);

        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId.Value,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert — 3 < 5, so velocity does NOT trigger
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        // Velocity threshold is 5, we seeded 3, so no rules match -> Approved
        Assert.Equal("Approved", result!.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task ExactlyAtThreshold_VelocityTriggersRejection()
    {
        // Arrange — seed exactly 5 transactions (hits velocity threshold)
        var customerId = CustomerId.New();
        await SeedTransactions(customerId, 5);

        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId.Value,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert — 5 >= 5, velocity triggers match with action Reject
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal("Rejected", result!.Status);
        Assert.Equal(70, result.TotalRiskScore);
        Assert.Contains("Velocity", result.MatchedRules);
    }

    [Fact]
    public async Task AboveThreshold_VelocityTriggersRejection()
    {
        // Arrange — seed 7 transactions (above velocity threshold)
        var customerId = CustomerId.New();
        await SeedTransactions(customerId, 7);

        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId.Value,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert — 7 >= 5, velocity triggers
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal("Rejected", result!.Status);
        Assert.Equal(70, result.TotalRiskScore);
        Assert.Contains("Velocity", result.MatchedRules);
    }

    [Fact]
    public async Task TransactionsOutsideTimeWindow_NotCounted()
    {
        // Arrange — seed 5 transactions but set CreatedAt to 2 hours ago (outside 1-hour window)
        var customerId = CustomerId.New();
        await SeedTransactions(customerId, 5, age: TimeSpan.FromHours(2));

        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId.Value,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert — transactions are older than 1 hour, so not counted
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);
        Assert.Equal("Approved", result!.Status);
        Assert.Equal(0, result.TotalRiskScore);
    }

    [Fact]
    public async Task DifferentCustomersDontAffectEachOther()
    {
        // Arrange — seed 5 for Customer A, none for Customer B
        var customerAId = CustomerId.New();
        var customerBId = CustomerId.New();
        await SeedTransactions(customerAId, 5);

        // Act — analyze Customer A (should trigger velocity)
        var commandA = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerAId.Value,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };
        var responseA = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", commandA);

        // Act — analyze Customer B (should NOT trigger velocity)
        var commandB = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerBId.Value,
            Amount = 100,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };
        var responseB = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", commandB);

        // Assert — Customer A: Rejected (velocity triggered)
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        var resultA = await responseA.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(resultA);
        Assert.Equal("Rejected", resultA!.Status);
        Assert.Contains("Velocity", resultA.MatchedRules);

        // Assert — Customer B: Approved (no velocity)
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        var resultB = await responseB.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(resultB);
        Assert.Equal("Approved", resultB!.Status);
        Assert.Equal(0, resultB.TotalRiskScore);
    }

    [Fact]
    public async Task FullEndToEnd_PostAnalyzeThenGetById()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = transactionId,
            CustomerId = customerId,
            Amount = 250.50m,
            Currency = "USD",
            Country = "US",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "api"
            }
        };

        // Act — POST analyze
        var postResponse = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var postResult = await postResponse.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(postResult);
        Assert.Equal(transactionId, postResult!.TransactionId);

        // Act — GET by ID
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{transactionId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Assert — GET returns the persisted transaction with correct data
        var getContent = await getResponse.Content.ReadAsStringAsync();
        Assert.Contains(transactionId.ToString(), getContent);
        Assert.Contains("Approved", getContent);
        Assert.Contains("US", getContent);
        Assert.Contains("source", getContent);
        Assert.Contains("api", getContent);
    }

    [Fact]
    public async Task BlacklistRule_LoadedAndDoesNotMatchNormalCustomer()
    {
        // Arrange — a normal customer (not blacklisted) with a low-value transaction
        var customerId = Guid.NewGuid();
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = customerId,
            Amount = 50,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/analyze", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AnalyzeTransactionResult>();
        Assert.NotNull(result);

        // The Blacklist rule exists in seeded data (risk 100, action Reject),
        // but the blacklisted customers list is empty, so it should not match.
        // The transaction should be Approved with no rules matched.
        Assert.Equal("Approved", result!.Status);
        Assert.Equal(0, result.TotalRiskScore);
        Assert.DoesNotContain("Blacklist", result.MatchedRules);
    }

    /// <summary>
    /// Seeds transactions for the given customer directly in the database.
    /// Optionally sets CreatedAt to a past time using reflection for time-window tests.
    /// </summary>
    private async Task SeedTransactions(CustomerId customerId, int count, TimeSpan? age = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();

        for (int i = 0; i < count; i++)
        {
            var tx = new Transaction(
                TransactionId.New(),
                customerId,
                new Money(100m, "USD"),
                DateTime.UtcNow,
                country: "US",
                metadata: new Dictionary<string, string>());

            // If an age is specified, use reflection to set CreatedAt to a past time
            if (age.HasValue)
            {
                var property = typeof(Transaction).GetProperty(
                    "CreatedAt",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                property!.SetValue(tx, DateTime.UtcNow - age.Value);
            }

            context.Transactions.Add(tx);
        }

        await context.SaveChangesAsync();
    }
}
