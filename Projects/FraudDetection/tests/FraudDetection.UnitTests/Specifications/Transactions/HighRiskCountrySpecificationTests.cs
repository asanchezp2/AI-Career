using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Specifications.Transactions;
using FraudDetection.Domain.ValueObjects;

namespace FraudDetection.UnitTests.Specifications.Transactions;

public class HighRiskCountrySpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_HighRiskCountry_ReturnsTrue()
    {
        // Arrange
        var specification = new HighRiskCountrySpecification(new[] { "IR", "KP" });
        var transaction = CreateTransaction(country: "IR");

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_SafeCountry_ReturnsFalse()
    {
        // Arrange
        var specification = new HighRiskCountrySpecification(new[] { "IR", "KP" });
        var transaction = CreateTransaction(country: "US");

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_EmptyList_ReturnsFalse()
    {
        // Arrange
        var specification = new HighRiskCountrySpecification(Array.Empty<string>());
        var transaction = CreateTransaction(country: "IR");

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_CaseInsensitiveMatching_ReturnsTrue()
    {
        // Arrange
        var specification = new HighRiskCountrySpecification(new[] { "ir", "kp" }); // lowercase input
        var transaction = CreateTransaction(country: "IR"); // uppercase in transaction

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSatisfiedBy_NullCountry_ReturnsFalse()
    {
        // Arrange
        var specification = new HighRiskCountrySpecification(new[] { "IR", "KP" });
        var transaction = CreateTransaction(country: null);

        // Act
        var result = specification.IsSatisfiedBy(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSatisfiedBy_NullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var specification = new HighRiskCountrySpecification(Array.Empty<string>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => specification.IsSatisfiedBy(null!));
    }

    [Fact]
    public void Constructor_NullCountryCodes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new HighRiskCountrySpecification(null!));
    }

    private static Transaction CreateTransaction(string? country = "US")
    {
        return new Transaction(
            TransactionId.New(),
            CustomerId.New(),
            new Money(100, "USD"),
            DateTime.UtcNow,
            country: country);
    }
}
