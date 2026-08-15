using FraudDetection.Application.Configuration;
using Microsoft.Extensions.Options;

namespace FraudDetection.UnitTests.Configuration;

public class RateLimitOptionsValidatorTests
{
    private readonly RateLimitOptionsValidator _validator = new();

    private static ValidateOptionsResult Validate(Action<RateLimitOptions> configure)
    {
        var options = new RateLimitOptions();
        configure(options);
        return new RateLimitOptionsValidator().Validate(name: null!, options);
    }

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = Validate(_ => { });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_NegativePermitLimit_Fails()
    {
        var result = Validate(o => o.PermitLimit = -1);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ZeroPermitLimit_Fails()
    {
        var result = Validate(o => o.PermitLimit = 0);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, m => m.Contains("PermitLimit"));
    }

    [Fact]
    public void Validate_ZeroWindowSeconds_Fails()
    {
        var result = Validate(o => o.WindowSeconds = 0);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_NegativeWindowSeconds_Fails()
    {
        var result = Validate(o => o.WindowSeconds = -60);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, m => m.Contains("WindowSeconds"));
    }

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = Validate(o =>
        {
            o.PermitLimit = 2;
            o.WindowSeconds = 60;
        });

        Assert.True(result.Succeeded);
    }
}