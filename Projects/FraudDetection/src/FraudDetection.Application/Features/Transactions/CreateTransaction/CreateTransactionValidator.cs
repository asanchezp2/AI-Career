using FluentValidation;

namespace FraudDetection.Application.Features.Transactions.CreateTransaction;

/// <summary>
/// Validates the CreateTransactionCommand input — shape-level validation only.
/// Business rules (the two fraud rejection criteria) are deliberately NOT here:
/// they are evaluated asynchronously by the anti-fraud worker, never in the
/// request path (see ADR-058).
/// </summary>
public class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required.");

        RuleFor(x => x.TargetAccountId)
            .NotEmpty()
            .WithMessage("Target account ID is required.");

        RuleFor(x => x.TransferTypeId)
            .GreaterThan(0)
            .WithMessage("Transfer type ID must be greater than zero.");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("Value must be greater than zero.");
    }
}