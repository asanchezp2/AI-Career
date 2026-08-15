using FraudDetection.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudDetection.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts TransactionStatus to/from its LOWERCASE string representation in
/// the database ("pending"/"approved"/"rejected") so the persisted value
/// matches the JSON wire format. Reading is case-insensitive.
/// </summary>
public sealed class TransactionStatusConverter : ValueConverter<TransactionStatus, string>
{
    public TransactionStatusConverter()
        : base(
            status => status.ToString().ToLowerInvariant(),
            value => Enum.Parse<TransactionStatus>(value, true))
    {
    }
}