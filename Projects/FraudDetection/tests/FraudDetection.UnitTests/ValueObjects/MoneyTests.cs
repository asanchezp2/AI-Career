using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_ValidArguments_CreatesMoney()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.Equal(amount, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Money_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var amount = -1m;
        var currency = "USD";

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, currency));
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Money_NullCurrency_ThrowsArgumentException()
    {
        // Arrange
        var amount = 100m;
        string? currency = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Money(amount, currency!));
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Money_EmptyCurrency_ThrowsArgumentException()
    {
        // Arrange
        var amount = 100m;
        var currency = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Money(amount, currency));
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Money_WhitespaceCurrency_ThrowsArgumentException()
    {
        // Arrange
        var amount = 100m;
        var currency = "   ";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Money(amount, currency));
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Money_LowercaseCurrency_ConvertedToUppercase()
    {
        // Arrange
        var amount = 100m;
        var currency = "usd";

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Money_CurrencyLengthNotThree_ThrowsArgumentException()
    {
        // Arrange
        var amount = 100m;
        var currency = "US";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Money(amount, currency));
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Money_CurrencyLengthMoreThanThree_ThrowsArgumentException()
    {
        // Arrange
        var amount = 100m;
        var currency = "USDX";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Money(amount, currency));
        Assert.Equal("currency", exception.ParamName);
    }

    [Fact]
    public void Money_ZeroAmount_CreatesMoney()
    {
        // Arrange
        var amount = 0m;
        var currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.Equal(amount, money.Amount);
    }

    [Fact]
    public void Money_EqualValues_AreEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Act & Assert
        Assert.Equal(money1, money2);
    }
}
