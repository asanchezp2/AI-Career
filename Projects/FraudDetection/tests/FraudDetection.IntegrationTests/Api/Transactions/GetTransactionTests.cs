using System.Net;
using System.Text.Json;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;

namespace FraudDetection.IntegrationTests.Api.Transactions;

public class GetTransactionTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateTime FixedCreatedAt = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetTransactionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> InsertPendingRowAsync(decimal value = 100m)
    {
        using var db = _factory.CreateDbContext();
        var transaction = new Transaction(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, value, FixedCreatedAt);
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction.TransactionExternalId;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task Get_ExistingTransaction_Returns200WithExpectedShape()
    {
        var id = await InsertPendingRowAsync(value: 120m);

        var response = await _client.GetAsync($"/api/v1/transactions/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadBodyAsync(response);
        Assert.Equal(id, body.GetProperty("transactionExternalId").GetGuid());
        Assert.Equal(FixedCreatedAt, body.GetProperty("createdAt").GetDateTime());
        Assert.Equal("pending", body.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("rejectionReason").ValueKind);
    }

    [Fact]
    public async Task Get_MissingTransaction_Returns404ProblemDetails()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/transactions/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());

        var body = await ReadBodyAsync(response);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal(missingId, body.GetProperty("transactionExternalId").GetGuid());
        Assert.Contains("not found", body.GetProperty("title").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_InvalidGuid_Returns404()
    {
        // The {id:guid} route constraint rejects non-GUID segments, so no route
        // matches and the framework returns 404.
        var response = await _client.GetAsync("/api/v1/transactions/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReflectsStatusUpdatePersistedByEvaluation()
    {
        var id = await InsertPendingRowAsync();

        var pendingResponse = await _client.GetAsync($"/api/v1/transactions/{id}");
        Assert.Equal("pending", (await ReadBodyAsync(pendingResponse)).GetProperty("status").GetString());

        // Simulate the anti-fraud worker: load the row and apply the Approved
        // transition (the same domain behavior the evaluation handler uses).
        using (var db = _factory.CreateDbContext())
        {
            var row = await db.Transactions.FindAsync(id);
            Assert.True(row!.Approve().IsSuccess);
            await db.SaveChangesAsync();
        }

        var approvedResponse = await _client.GetAsync($"/api/v1/transactions/{id}");
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);
        var body = await ReadBodyAsync(approvedResponse);
        Assert.Equal("approved", body.GetProperty("status").GetString());
    }
}