using FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

namespace FraudDetection.Api.Endpoints;

/// <summary>
/// Maps the AnalyzeTransaction endpoint.
/// </summary>
public static class AnalyzeTransactionEndpoint
{
    /// <summary>
    /// Maps the POST /api/transactions/analyze endpoint.
    /// </summary>
    public static void MapAnalyzeTransaction(this WebApplication app)
    {
        app.MapPost("/api/transactions/analyze", async (
            AnalyzeTransactionCommand command,
            AnalyzeTransactionValidator validator,
            AnalyzeTransactionHandler handler) =>
        {
            var validationResult = await validator.ValidateAsync(command);

            if (!validationResult.IsValid)
                return Results.ValidationProblem(
                    validationResult.ToDictionary());

            var result = handler.Handle(command);

            return Results.Ok(result);
        })
        .WithName("AnalyzeTransaction")
        .Produces<AnalyzeTransactionResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithDescription("Submits a transaction for fraud analysis.")
        .WithOpenApi();
    }
}
