using FraudDetection.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudDetection.Infrastructure.Persistence.Converters;

public sealed class FraudRuleIdConverter : ValueConverter<FraudRuleId, Guid>
{
    public FraudRuleIdConverter()
        : base(id => id.Value, value => FraudRuleId.From(value))
    {
    }
}
