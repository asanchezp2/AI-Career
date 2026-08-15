using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;

namespace FraudDetection.IntegrationTests.Api.Transactions;

public class CreateTransactionTests : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record TransactionPayload(
        Guid SourceAccountId,
        Guid TargetAccountId,
        int TransferTypeId,
        decimal Value);

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CreateTransactionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static TransactionPayload CreatePayload(
        decimal value = 120m,
        int transferTypeId = 1,
        Guid? sourceAccountId = null,
        Guid? targetAccountId = null) =>
        new(
            sourceAccountId ?? Guid.NewGuid(),
            targetAccountId ?? Guid.NewGuid(),
            transferTypeId,
            value);

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task Post_ValidTransaction_Returns201CreatedWithExpectedBodyAndLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", CreatePayload(value: 120m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadBodyAsync(response);
        var id = body.GetProperty("transactionExternalId").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.NotEqual(default(DateTime), body.GetProperty("createdAt").GetDateTime());
        Assert.Equal("pending", body.GetProperty("status").GetString());

        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/v1/transactions/{id}", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("tranferTypeId")]   // the challenge document's literal spelling (typo preserved)
    [InlineData("transferTypeId")]  // the correctly-spelled alias, accepted for robustness
    public async Task Post_ChallengePayload_Returns201Created_WithEitherTransferTypeSpelling(
        string fieldName)
    {
        // Contract fidelity: a client following Challenge_BE-LT.docx verbatim sends
        // "tranferTypeId" (no 's'); both spellings must return 201 Created + pending.
        var payload = $$"""
            {
              "sourceAccountId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
              "targetAccountId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
              "{{fieldName}}": 1,
              "value": 120
            }
            """;
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/transactions", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadBodyAsync(response);
        var id = body.GetProperty("transactionExternalId").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("pending", body.GetProperty("status").GetString());
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/v1/transactions/{id}", response.Headers.Location!.OriginalString);

        // The literal challenge transfer type id reached the domain: it was bound
        // to TransferTypeId and persisted as 1.
        using var db = _factory.CreateDbContext();
        var row = await db.Transactions.FindAsync(id);
        Assert.NotNull(row);
        Assert.Equal(1, row!.TransferTypeId);
    }

    [Fact]
    public async Task Post_LiteralChallengePayload_SourceOfTruth_Returns201Created()
    {
        // The exact payload from Challenge_BE-LT.docx — byte-for-byte shape:
        // { "sourceAccountId": "Guid", "targetAccountId": "Guid", "tranferTypeId": 1, "value": 120 }
        const string challengePayload =
            """
            {
              "sourceAccountId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
              "targetAccountId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
              "tranferTypeId": 1,
              "value": 120
            }
            """;
        var content = new StringContent(challengePayload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/transactions", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadBodyAsync(response);
        Assert.Equal("pending", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_ValidTransaction_PersistsPendingRow()
    {
        var payload = CreatePayload(value: 250.50m);
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", payload);
        var id = (await ReadBodyAsync(response)).GetProperty("transactionExternalId").GetGuid();

        using var db = _factory.CreateDbContext();
        var row = await db.Transactions.FindAsync(id);

        Assert.NotNull(row);
        Assert.Equal(250.50m, row!.Value);
        Assert.Equal(payload.SourceAccountId, row.SourceAccountId);
        Assert.Equal(payload.TargetAccountId, row.TargetAccountId);
        Assert.Equal(1, row.TransferTypeId);
        Assert.Equal(TransactionStatus.Pending, row.Status);
        Assert.Null(row.RejectionReason);
    }

    [Fact]
    public async Task Post_ValidTransaction_PublishesTransactionCreatedEvent()
    {
        var before = _factory.EventPublisher.CreatedEvents.Count;
        var payload = CreatePayload(value: 300m);
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", payload);
        var id = (await ReadBodyAsync(response)).GetProperty("transactionExternalId").GetGuid();

        var createdEvents = _factory.EventPublisher.CreatedEvents;
        Assert.Equal(before + 1, createdEvents.Count);

        var created = createdEvents[^1];
        Assert.Equal(id, created.TransactionExternalId);
        Assert.Equal(payload.SourceAccountId, created.SourceAccountId);
        Assert.Equal(payload.TargetAccountId, created.TargetAccountId);
        Assert.Equal(1, created.TransferTypeId);
        Assert.Equal(payload.Value, created.Value);
        Assert.NotEqual(default(DateTime), created.CreatedAt);
    }

    [Fact]
    public async Task Post_HighValue5000_Still201AndPending_ProvesAsyncEvaluation()
    {
        // Core async requirement (ADR-058): a transaction worth 5000 exceeds the
        // high-value rule threshold, yet the API does NOT evaluate it — it is
        // created as pending and returned as 201. Evaluation happens only in the
        // worker (covered by unit tests) and never in the request path.
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", CreatePayload(value: 5000m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadBodyAsync(response);
        Assert.Equal("pending", body.GetProperty("status").GetString());

        // The API publishes only TransactionCreated events — never an evaluation.
        Assert.Empty(_factory.EventPublisher.EvaluatedEvents);
    }

    [Fact]
    public async Task Post_TransferTypeIdZero_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/transactions",
            CreatePayload(transferTypeId: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body);
        Assert.Contains("Transfer type ID must be greater than zero.", body);
    }

    [Fact]
    public async Task Post_ValueZero_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/transactions",
            CreatePayload(value: 0m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Value must be greater than zero.", body);
    }

    [Fact]
    public async Task Post_NegativeValue_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/transactions",
            CreatePayload(value: -100m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task Post_EmptySourceAccountId_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/transactions",
            CreatePayload(sourceAccountId: Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Source account ID is required.", body);
    }

    [Fact]
    public async Task Post_EmptyTargetAccountId_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/transactions",
            CreatePayload(targetAccountId: Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Target account ID is required.", body);
    }

    [Fact]
    public async Task Post_MalformedJson_Returns400()
    {
        var content = new StringContent("{ not valid json !!", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/transactions", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}