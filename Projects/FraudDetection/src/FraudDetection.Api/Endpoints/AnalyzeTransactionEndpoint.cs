using FraudDetection.Application.Abstractions;
using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.Api.Endpoints;

/// <summary>
/// Maps transaction-related API endpoints.
/// </summary>
public static class AnalyzeTransactionEndpoint
{
    /// <summary>
    /// Maps the POST /api/v1/transactions/analyze and GET /api/v1/transactions/{id} endpoints.
    /// </summary>
    public static void MapAnalyzeTransaction(this WebApplication app)
    {
        app.MapPost("/api/v1/transactions/analyze", async (
            AnalyzeTransactionCommand command,
            AnalyzeTransactionValidator validator,
            AnalyzeTransactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(
                    validationResult.ToDictionary());

            var result = await handler.Handle(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("AnalyzeTransaction")
        .Produces<AnalyzeTransactionResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithDescription("Submits a transaction for fraud analysis.")
        .WithOpenApi();

        app.MapGet("/api/v1/transactions/{id:guid}", async (Guid id, ITransactionRepository repository) =>
        {
            var transactionId = TransactionId.From(id);
            var transaction = await repository.GetByIdAsync(transactionId);
            if (transaction is null)
                return Results.NotFound();

            return Results.Ok(new
            {
                id = transaction.Id.Value,
                customerId = transaction.CustomerId.Value,
                amount = transaction.Amount.Amount,
                currency = transaction.Amount.Currency,
                country = transaction.Country,
                status = transaction.Status.ToString(),
                createdAt = transaction.CreatedAt,
                metadata = transaction.Metadata
            });
        })
        .WithName("GetTransaction")
        .WithDescription("Retrieves a transaction by its unique identifier.")
        .WithOpenApi();
    }
}
