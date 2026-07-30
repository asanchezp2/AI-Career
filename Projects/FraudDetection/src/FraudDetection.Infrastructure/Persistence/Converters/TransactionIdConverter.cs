using FraudDetection.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudDetection.Infrastructure.Persistence.Converters;

public sealed class TransactionIdConverter : ValueConverter<TransactionId, Guid>
{
    public TransactionIdConverter()
        : base(id => id.Value, value => TransactionId.From(value))
    {
    }
}
