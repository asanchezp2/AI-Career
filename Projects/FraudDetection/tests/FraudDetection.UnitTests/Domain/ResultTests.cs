using FraudDetection.Domain;

namespace FraudDetection.UnitTests.Domain;

public class ResultTests
{
    // Result (non-generic) -----------------------------------------------

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

    // Result<T> (generic) ------------------------------------------------

    [Fact]
    public void GenericSuccess_HasValue()
    {
        // Act
        var result = Result<int>.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ValueIsDefault()
    {
        // Act
        var result = Result<int>.Failure("An error occurred.");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(default, result.Value);
        Assert.Equal("An error occurred.", result.Error);
    }
}
