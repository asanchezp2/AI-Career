using FraudDetection.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudDetection.Infrastructure.Persistence.Converters;

public sealed class CustomerIdConverter : ValueConverter<CustomerId, Guid>
{
    public CustomerIdConverter()
        : base(id => id.Value, value => CustomerId.From(value))
    {
    }
}
