using FraudDetection.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudDetection.Infrastructure.Persistence.Converters;

public sealed class TransactionStatusConverter : ValueConverter<TransactionStatus, string>
{
    public TransactionStatusConverter()
        : base(status => status.ToString(), value => Enum.Parse<TransactionStatus>(value))
    {
    }
}
