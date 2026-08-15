using FraudDetection.Domain;

namespace FraudDetection.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessIsTrue()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_IsFailureIsTrue()
    {
        // Act
        var result = Result.Failure("Something went wrong.");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong.", result.Error);
    }
}