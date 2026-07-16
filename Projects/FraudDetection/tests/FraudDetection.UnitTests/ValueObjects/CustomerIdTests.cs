using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.ValueObjects;

public class CustomerIdTests
{
    [Fact]
    public void New_GeneratesValidNonEmptyGuid()
    {
        // Act
        var customerId = CustomerId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, customerId.Value);
    }

    [Fact]
    public void From_ValidGuid_CreatesCorrectly()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var customerId = CustomerId.From(expectedGuid);

        // Assert
        Assert.Equal(expectedGuid, customerId.Value);
    }

    [Fact]
    public void From_EmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => CustomerId.From(Guid.Empty));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var customerId1 = CustomerId.From(guid);
        var customerId2 = CustomerId.From(guid);

        // Act & Assert
        Assert.Equal(customerId1, customerId2);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        // Arrange
        var customerId1 = CustomerId.New();
        var customerId2 = CustomerId.New();

        // Act & Assert
        Assert.NotEqual(customerId1, customerId2);
    }
}
