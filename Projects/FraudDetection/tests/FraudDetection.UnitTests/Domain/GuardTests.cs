using FraudDetection.Domain;

namespace FraudDetection.UnitTests.Domain;

public class GuardTests
{
    // AgainstNull (reference type) ---------------------------------------

    [Fact]
    public void AgainstNull_NullReference_ThrowsArgumentNullException()
    {
        string? value = null;
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value!, "value"));
    }

    [Fact]
    public void AgainstNull_NonNullReference_DoesNotThrow()
    {
        var value = "hello";
        var exception = Record.Exception(() => Guard.AgainstNull(value, nameof(value)));
        Assert.Null(exception);
    }

    // AgainstNullOrWhiteSpace --------------------------------------------

    [Fact]
    public void AgainstNullOrWhiteSpace_Null_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(null!, "value"));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(string.Empty, "value"));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_Whitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace("   ", "value"));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_ValidString_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstNullOrWhiteSpace("USD", "currency"));
        Assert.Null(exception);
    }

    // AgainstOutOfRange (int) --------------------------------------------

    [Fact]
    public void AgainstOutOfRange_IntBelowMin_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(-1, 0, 100, "value"));
    }

    [Fact]
    public void AgainstOutOfRange_IntAboveMax_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(101, 0, 100, "value"));
    }

    [Fact]
    public void AgainstOutOfRange_IntInRange_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstOutOfRange(50, 0, 100, "value"));
        Assert.Null(exception);
    }

    [Fact]
    public void AgainstOutOfRange_IntAtMin_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstOutOfRange(0, 0, 100, "value"));
        Assert.Null(exception);
    }

    [Fact]
    public void AgainstOutOfRange_IntAtMax_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstOutOfRange(100, 0, 100, "value"));
        Assert.Null(exception);
    }

    // AgainstEmptyGuid ---------------------------------------------------

    [Fact]
    public void AgainstEmptyGuid_EmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty, "value"));
    }

    [Fact]
    public void AgainstEmptyGuid_ValidGuid_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstEmptyGuid(Guid.NewGuid(), "value"));
        Assert.Null(exception);
    }

    // AgainstNegative ----------------------------------------------------

    [Fact]
    public void AgainstNegative_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegative(-1m, "value"));
    }

    [Fact]
    public void AgainstNegative_Zero_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstNegative(0m, "value"));
        Assert.Null(exception);
    }

    [Fact]
    public void AgainstNegative_Positive_DoesNotThrow()
    {
        var exception = Record.Exception(() => Guard.AgainstNegative(100m, "value"));
        Assert.Null(exception);
    }
}
