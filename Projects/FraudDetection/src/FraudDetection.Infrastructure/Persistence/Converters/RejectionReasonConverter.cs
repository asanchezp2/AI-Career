using FraudDetection.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudDetection.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts RejectionReason to/from its LOWERCASE string representation in the
/// database ("highvalue"/"dailyaccumulated") — consistent with the status
/// column and the JSON wire format. Reading is case-insensitive.
/// </summary>
public sealed class RejectionReasonConverter : ValueConverter<RejectionReason, string>
{
    public RejectionReasonConverter()
        : base(
            reason => reason.ToString().ToLowerInvariant(),
            value => Enum.Parse<RejectionReason>(value, true))
    {
    }
}