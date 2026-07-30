using System.Text.RegularExpressions;
using FluentValidation;

namespace FraudDetection.Application.Features.Transactions.AnalyzeTransaction;

/// <summary>
/// Validates the AnalyzeTransactionCommand input.
/// </summary>
public class AnalyzeTransactionValidator : AbstractValidator<AnalyzeTransactionCommand>
{
    private static readonly Regex CountryCodeRegex = new("^[A-Z]{2}$", RegexOptions.Compiled);

    public AnalyzeTransactionValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required.");

        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Amount must be greater than or equal to zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters.")
            .Must(currency => currency == currency.ToUpperInvariant())
            .WithMessage("Currency must be uppercase.");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required.");

        RuleFor(x => x.Country)
            .Must(country => country is null || CountryCodeRegex.IsMatch(country))
            .WithMessage("Country must be a valid ISO 3166-1 alpha-2 code (2 uppercase letters).");
    }
}
