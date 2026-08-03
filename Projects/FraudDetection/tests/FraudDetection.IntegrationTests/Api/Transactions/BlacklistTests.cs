using System.Net;
using System.Net.Http.Json;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.ValueObjects;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetection.IntegrationTests.Api.Transactions;

/// <summary>
/// End-to-end tests for the Blacklist fraud rule through the full API stack.
/// The CustomWebApplicationFactory does not run the Program.cs seeding block
/// (Testing environment), so the demo blacklisted customer is seeded here
/// using the factory's service scope, mirroring the factory's fraud rule seeding.
/// </summary>
public class BlacklistTests : IClassFixture<CustomWebApplicationFactory>
{
    /// <summary>
    /// The demo blacklisted customer ID seeded by Program.cs in Development.
    /// Seeded in this fixture's database by <see cref="SeedBlacklistedCustomerAsync"/>.
    /// </summary>
    public static readonly Guid BlacklistedCustomerId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BlacklistTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_BlacklistedCustomer_ReturnsRejected()
    {
        // Arrange — ensure the demo blacklisted customer exists in the test DB
        await SeedBlacklistedCustomerAsync();

        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
            CustomerId = BlacklistedCustomerId,
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
        Assert.Equal("Rejected", result!.Status);
        Assert.Contains("Blacklist", result.MatchedRules);
        Assert.Equal(100, result.TotalRiskScore);
    }

    [Fact]
    public async Task Post_NonBlacklistedCustomer_NotRejectedByBlacklist()
    {
        // Arrange — a fresh random customer is not blacklisted
        var command = new AnalyzeTransactionCommand
        {
            TransactionId = Guid.NewGuid(),
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
        Assert.NotEqual("Rejected", result!.Status);
        Assert.DoesNotContain("Blacklist", result.MatchedRules);
    }

    /// <summary>
    /// Seeds the demo blacklisted customer directly in the SQLite database,
    /// mirroring the Program.cs seeding that runs in Development.
    /// Idempotent — safe when the factory is shared across tests in this class.
    /// </summary>
    private async Task SeedBlacklistedCustomerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();

        if (!context.BlacklistedCustomers.Any(b => b.CustomerId == CustomerId.From(BlacklistedCustomerId)))
        {
            context.BlacklistedCustomers.Add(new BlacklistedCustomer(
                CustomerId.From(BlacklistedCustomerId),
                "Demo blacklisted customer"));
            await context.SaveChangesAsync();
        }
    }
}
