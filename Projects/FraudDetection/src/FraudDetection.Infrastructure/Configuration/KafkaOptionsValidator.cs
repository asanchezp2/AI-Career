using Microsoft.Extensions.Options;

namespace FraudDetection.Infrastructure.Configuration;

/// <summary>
/// Validates the bound <see cref="KafkaOptions"/> at application startup.
/// Registered with <c>ValidateOnStart</c> (both in the API and the Worker) so a
/// misconfigured deployment fails fast instead of a publisher/consumer that
/// silently produces or consumes nothing. Consistent with the Application-layer
/// option validators (RateLimitOptionsValidator).
/// </summary>
public sealed class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
{
    private static readonly string[] AllowedAutoOffsetResetValues = { "Earliest", "Latest" };

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, KafkaOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
            errors.Add($"{nameof(KafkaOptions.BootstrapServers)} must not be empty.");

        if (string.IsNullOrWhiteSpace(options.GroupId))
            errors.Add($"{nameof(KafkaOptions.GroupId)} must not be empty.");

        if (!AllowedAutoOffsetResetValues.Contains(options.AutoOffsetReset))
            errors.Add(
                $"{nameof(KafkaOptions.AutoOffsetReset)} must be one of: " +
                string.Join(", ", AllowedAutoOffsetResetValues) + ".");

        if (options.Topics is null
            || string.IsNullOrWhiteSpace(options.Topics.TransactionCreated)
            || string.IsNullOrWhiteSpace(options.Topics.TransactionEvaluated))
        {
            errors.Add(
                $"{nameof(KafkaOptions.Topics)} must define both " +
                $"{nameof(KafkaTopicOptions.TransactionCreated)} and " +
                $"{nameof(KafkaTopicOptions.TransactionEvaluated)}.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}