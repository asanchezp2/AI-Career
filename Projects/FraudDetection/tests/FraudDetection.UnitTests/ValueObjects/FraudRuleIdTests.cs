using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.ValueObjects;

public class FraudRuleIdTests
{
    [Fact]
    public void New_GeneratesValidNonEmptyGuid()
    {
        // Act
        var fraudRuleId = FraudRuleId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, fraudRuleId.Value);
    }

    [Fact]
    public void From_ValidGuid_CreatesCorrectly()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var fraudRuleId = FraudRuleId.From(expectedGuid);

        // Assert
        Assert.Equal(expectedGuid, fraudRuleId.Value);
    }

    [Fact]
    public void From_EmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => FraudRuleId.From(Guid.Empty));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var fraudRuleId1 = FraudRuleId.From(guid);
        var fraudRuleId2 = FraudRuleId.From(guid);

        // Act & Assert
        Assert.Equal(fraudRuleId1, fraudRuleId2);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        // Arrange
        var fraudRuleId1 = FraudRuleId.New();
        var fraudRuleId2 = FraudRuleId.New();

        // Act & Assert
        Assert.NotEqual(fraudRuleId1, fraudRuleId2);
    }
}
