using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.ValueObjects;

public class TransactionIdTests
{
    [Fact]
    public void New_GeneratesValidNonEmptyGuid()
    {
        // Act
        var transactionId = TransactionId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, transactionId.Value);
    }

    [Fact]
    public void From_ValidGuid_CreatesCorrectly()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();

        // Act
        var transactionId = TransactionId.From(expectedGuid);

        // Assert
        Assert.Equal(expectedGuid, transactionId.Value);
    }

    [Fact]
    public void From_EmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TransactionId.From(Guid.Empty));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var transactionId1 = TransactionId.From(guid);
        var transactionId2 = TransactionId.From(guid);

        // Act & Assert
        Assert.Equal(transactionId1, transactionId2);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        // Arrange
        var transactionId1 = TransactionId.New();
        var transactionId2 = TransactionId.New();

        // Act & Assert
        Assert.NotEqual(transactionId1, transactionId2);
    }
}
