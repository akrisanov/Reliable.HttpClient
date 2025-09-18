#pragma warning disable MA0048 // File name must match type name

using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientOptionsValidationTests
{
    [Fact]
    public void HttpClientOptions_Validate_WithDefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = new HttpClientOptions();

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void HttpClientOptions_Validate_WithInvalidTimeoutSeconds_ShouldThrow(int timeoutSeconds)
    {
        // Arrange
        var options = new HttpClientOptions { TimeoutSeconds = timeoutSeconds };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(HttpClientOptions.TimeoutSeconds))
            .WithMessage("TimeoutSeconds must be greater than 0*");
    }

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("relative/path")]
    public void HttpClientOptions_Validate_WithInvalidBaseUrl_ShouldThrow(string baseUrl)
    {
        // Arrange
        var options = new HttpClientOptions { BaseUrl = baseUrl };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(HttpClientOptions.BaseUrl))
            .WithMessage("BaseUrl must be a valid absolute URI when specified*");
    }

    [Theory]
    [InlineData("ftp://invalid-scheme.com")]
    [InlineData("file:///local/path")]
    public void HttpClientOptions_Validate_WithNonHttpScheme_ShouldThrow(string baseUrl)
    {
        // Arrange
        var options = new HttpClientOptions { BaseUrl = baseUrl };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(HttpClientOptions.BaseUrl))
            .WithMessage("BaseUrl must use HTTP or HTTPS scheme*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://api.example.com")]
    [InlineData("http://localhost:8080")]
    public void HttpClientOptions_Validate_WithValidBaseUrl_ShouldNotThrow(string baseUrl)
    {
        // Arrange
        var options = new HttpClientOptions { BaseUrl = baseUrl };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class RetryOptionsValidationTests
{
    [Fact]
    public void RetryOptions_Validate_WithDefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = new RetryOptions();

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void RetryOptions_Validate_WithNegativeMaxRetries_ShouldThrow(int maxRetries)
    {
        // Arrange
        var options = new RetryOptions { MaxRetries = maxRetries };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.MaxRetries))
            .WithMessage("MaxRetries cannot be negative*");
    }

    [Fact]
    public void RetryOptions_Validate_WithZeroMaxRetries_ShouldNotThrow()
    {
        // Arrange
        var options = new RetryOptions { MaxRetries = 0 };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void RetryOptions_Validate_WithInvalidBaseDelay_ShouldThrow(int delayMilliseconds)
    {
        // Arrange
        var options = new RetryOptions { BaseDelay = TimeSpan.FromMilliseconds(delayMilliseconds) };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.BaseDelay))
            .WithMessage("BaseDelay must be greater than zero*");
    }

    [Fact]
    public void RetryOptions_Validate_WithZeroMaxDelay_ShouldThrow()
    {
        // Arrange
        var options = new RetryOptions { MaxDelay = TimeSpan.Zero };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.MaxDelay))
            .WithMessage("MaxDelay must be greater than zero*");
    }

    [Fact]
    public void RetryOptions_Validate_WithBaseDelayGreaterThanMaxDelay_ShouldThrow()
    {
        // Arrange
        var options = new RetryOptions
        {
            BaseDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.BaseDelay))
            .WithMessage("BaseDelay cannot be greater than MaxDelay*");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void RetryOptions_Validate_WithInvalidJitterFactor_ShouldThrow(double jitterFactor)
    {
        // Arrange
        var options = new RetryOptions { JitterFactor = jitterFactor };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.JitterFactor))
            .WithMessage("JitterFactor must be between 0.0 and 1.0*");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void RetryOptions_Validate_WithValidJitterFactor_ShouldNotThrow(double jitterFactor)
    {
        // Arrange
        var options = new RetryOptions { JitterFactor = jitterFactor };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class CircuitBreakerOptionsValidationTests
{
    [Fact]
    public void CircuitBreakerOptions_Validate_WithDefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = new CircuitBreakerOptions();

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CircuitBreakerOptions_Validate_WithInvalidFailuresBeforeOpen_ShouldThrow(int failuresBeforeOpen)
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailuresBeforeOpen = failuresBeforeOpen };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(CircuitBreakerOptions.FailuresBeforeOpen))
            .WithMessage("FailuresBeforeOpen must be greater than 0*");
    }

    [Fact]
    public void CircuitBreakerOptions_Validate_WithZeroOpenDuration_ShouldThrow()
    {
        // Arrange
        var options = new CircuitBreakerOptions { OpenDuration = TimeSpan.Zero };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(CircuitBreakerOptions.OpenDuration))
            .WithMessage("OpenDuration must be greater than zero*");
    }

    [Fact]
    public void CircuitBreakerOptions_Validate_WithNegativeOpenDuration_ShouldThrow()
    {
        // Arrange
        var options = new CircuitBreakerOptions { OpenDuration = TimeSpan.FromMilliseconds(-100) };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(CircuitBreakerOptions.OpenDuration))
            .WithMessage("OpenDuration must be greater than zero*");
    }
}

#pragma warning restore MA0048
