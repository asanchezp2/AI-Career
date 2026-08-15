using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.CreateTransaction;
using FraudDetection.Application.Features.Transactions.GetTransaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FraudDetection.Api.Endpoints;

/// <summary>
/// Maps the transaction endpoints of the real challenge:
/// POST /api/v1/transactions (create) and GET /api/v1/transactions/{id} (query state).
/// </summary>
public static class TransactionsEndpoint
{
    /// <summary>
    /// Maps the POST /api/v1/transactions and GET /api/v1/transactions/{id} endpoints.
    /// </summary>
    public static void MapTransactions(this WebApplication app)
    {
        // POST /api/v1/transactions — Resource 1 of the challenge.
        // Creates the transaction in Pending status and returns 201 Created with
        // a Location header; the anti-fraud evaluation happens ASYNCHRONOUSLY via
        // Kafka (the worker updates the state later — see ADR-058). This endpoint
        // never evaluates fraud rules synchronously.
        app.MapPost("/api/v1/transactions", async (
            CreateTransactionCommand command,
            CreateTransactionValidator validator,
            CreateTransactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(
                    validationResult.ToDictionary());

            var result = await handler.Handle(command, cancellationToken);

            return Results.Created(
                $"/api/v1/transactions/{result.TransactionExternalId}",
                result);
        })
        .WithName("CreateTransaction")
        .Produces<CreateTransactionResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .RequireRateLimiting("create-transaction")
        .WithDescription("Creates a transaction in Pending status and queues it for " +
                         "asynchronous anti-fraud evaluation via Kafka. Returns 201 with " +
                         "a Location header; the state transitions to approved or rejected " +
                         "shortly after (query with GET /api/v1/transactions/{id}). " +
                         "Subject to a configurable rate limit (HTTP 429 when exceeded).")
        .WithOpenApi();

        // GET /api/v1/transactions/{id} — Resource 2 of the challenge.
        // Returns the transaction state; 404 ProblemDetails when unknown
        // (RFC 7807, consistent with the exception middleware).
        app.MapGet("/api/v1/transactions/{id:guid}", async (
            Guid id,
            ITransactionRepository repository,
            CancellationToken cancellationToken) =>
        {
            var transaction = await repository.GetByIdAsync(id, cancellationToken);
            if (transaction is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Transaction not found",
                    detail: $"No transaction exists with ID '{id}'.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["transactionExternalId"] = id
                    });
            }

            var response = new GetTransactionResponse(
                transaction.TransactionExternalId,
                transaction.CreatedAt,
                transaction.Status.ToString().ToLowerInvariant(),
                transaction.RejectionReason?.ToString().ToLowerInvariant());
            return Results.Ok(response);
        })
        .WithName("GetTransaction")
        .Produces<GetTransactionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithDescription("Retrieves a transaction by its external identifier, including " +
                         "its current state (pending, approved, or rejected) and — when " +
                         "rejected — the rule that caused the rejection.")
        .WithOpenApi();
    }
}