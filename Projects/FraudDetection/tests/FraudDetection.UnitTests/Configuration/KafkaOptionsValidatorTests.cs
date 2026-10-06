using FraudDetection.Infrastructure.Configuration;

namespace FraudDetection.UnitTests.Configuration;

public class KafkaOptionsValidatorTests
{
    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = new KafkaOptionsValidator().Validate(null, new KafkaOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyEvaluationResultGroupId_Fails()
    {
        var options = new KafkaOptions { EvaluationResultGroupId = " " };

        var result = new KafkaOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("EvaluationResultGroupId"));
    }
}
