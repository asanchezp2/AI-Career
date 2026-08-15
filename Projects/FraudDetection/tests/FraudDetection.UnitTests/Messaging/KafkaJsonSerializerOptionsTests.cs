using System.Text.Json;
using FraudDetection.Application.Events;
using FraudDetection.Domain.Enums;
using FraudDetection.Infrastructure.Messaging;

namespace FraudDetection.UnitTests.Messaging;

/// <summary>
/// Verifies the Kafka wire contract: enum values are serialized as LOWERCASE
/// strings ("pending"/"approved"/"rejected", "highvalue"/"dailyaccumulated"),
/// matching the HTTP wire format and the database storage — and can be
/// deserialized back case-insensitively.
/// </summary>
public class KafkaJsonSerializerOptionsTests
{
    private static readonly Guid ExternalId = Guid.Parse("9c8b7a6f-5e4d-4c3b-8a9f-0e1d2c3b4a59");

    [Fact]
    public void Serialize_RejectedHighValueEvent_EmitsLowercaseEnums()
    {
        var json = JsonSerializer.Serialize(
            new TransactionEvaluatedEvent(ExternalId, TransactionStatus.Rejected, RejectionReason.HighValue),
            KafkaJsonSerializerOptions.Default);

        Assert.Contains("\"status\":\"rejected\"", json);
        Assert.Contains("\"rejectionReason\":\"highvalue\"", json);
        Assert.DoesNotContain("\"highValue\"", json);
        Assert.DoesNotContain("\"Rejected\"", json);
    }

    [Fact]
    public void Serialize_ApprovedEvent_EmitsLowercaseEnumWithNullReason()
    {
        var json = JsonSerializer.Serialize(
            new TransactionEvaluatedEvent(ExternalId, TransactionStatus.Approved, null),
            KafkaJsonSerializerOptions.Default);

        Assert.Contains("\"status\":\"approved\"", json);
        Assert.Contains("\"rejectionReason\":null", json);
    }

    [Fact]
    public void Serialize_DailyAccumulatedRejection_EmitsLowercaseReason()
    {
        var json = JsonSerializer.Serialize(
            new TransactionEvaluatedEvent(ExternalId, TransactionStatus.Rejected, RejectionReason.DailyAccumulated),
            KafkaJsonSerializerOptions.Default);

        Assert.Contains("\"rejectionReason\":\"dailyaccumulated\"", json);
        Assert.DoesNotContain("\"DailyAccumulated\"", json);
    }

    [Fact]
    public void RoundTrip_RejectedEvent_DeserializesLowercaseEnums()
    {
        var json = JsonSerializer.Serialize(
            new TransactionEvaluatedEvent(ExternalId, TransactionStatus.Rejected, RejectionReason.HighValue),
            KafkaJsonSerializerOptions.Default);

        var back = JsonSerializer.Deserialize<TransactionEvaluatedEvent>(json, KafkaJsonSerializerOptions.Default);

        Assert.NotNull(back);
        Assert.Equal(ExternalId, back!.TransactionExternalId);
        Assert.Equal(TransactionStatus.Rejected, back.Status);
        Assert.Equal(RejectionReason.HighValue, back.RejectionReason);
    }

    [Fact]
    public void RoundTrip_ApprovedEventWithNullReason_RoundTrips()
    {
        var json = JsonSerializer.Serialize(
            new TransactionEvaluatedEvent(ExternalId, TransactionStatus.Approved, null),
            KafkaJsonSerializerOptions.Default);

        var back = JsonSerializer.Deserialize<TransactionEvaluatedEvent>(json, KafkaJsonSerializerOptions.Default);

        Assert.NotNull(back);
        Assert.Equal(ExternalId, back!.TransactionExternalId);
        Assert.Equal(TransactionStatus.Approved, back.Status);
        Assert.Null(back.RejectionReason);
    }
}