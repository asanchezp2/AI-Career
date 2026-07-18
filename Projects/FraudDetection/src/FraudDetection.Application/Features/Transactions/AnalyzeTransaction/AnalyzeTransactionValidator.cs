using FluentValidation;

namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Validates the AnalyzeTransactionCommand input.
/// </summary>
public class AnalyzeTransactionValidator : AbstractValidator<AnalyzeTransactionCommand>
{
    public AnalyzeTransactionValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required.");

        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters.")
            .Must(currency => currency == currency.ToUpperInvariant())
            .WithMessage("Currency must be uppercase.");
    }
}
