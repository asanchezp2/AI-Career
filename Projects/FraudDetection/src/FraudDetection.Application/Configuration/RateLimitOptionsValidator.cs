using Microsoft.Extensions.Options;

namespace FraudDetection.Application.Configuration;

/// <summary>
/// Validates the bound <see cref="RateLimitOptions"/> at application startup.
/// Registered with <c>ValidateOnStart</c> so a misconfigured deployment fails fast
/// instead of rejecting every request at runtime.
/// </summary>
public sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        var errors = new List<string>();

        if (options.PermitLimit < 1)
            errors.Add($"{nameof(RateLimitOptions.PermitLimit)} must be at least 1.");

        if (options.WindowSeconds <= 0)
            errors.Add($"{nameof(RateLimitOptions.WindowSeconds)} must be greater than 0.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
