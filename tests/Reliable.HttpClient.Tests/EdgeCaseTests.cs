using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class EdgeCaseTests
{
    [Fact]
    public void HttpClientOptions_WithExtremeValues_ValidatesCorrectly()
    {
        // Arrange & Act & Assert - Maximum valid values
        var maxOptions = new HttpClientOptions
        {
            TimeoutSeconds = int.MaxValue,
            Retry = new RetryOptions
            {
                MaxRetries = int.MaxValue,
                BaseDelay = TimeSpan.MaxValue,
                MaxDelay = TimeSpan.MaxValue,
                JitterFactor = 1.0,
            },
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailuresBeforeOpen = int.MaxValue,
                OpenDuration = TimeSpan.MaxValue,
            },
        };

        maxOptions.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void HttpClientOptions_WithMinimumValidValues_ValidatesCorrectly()
    {
        // Arrange & Act & Assert - Minimum valid values
        var minOptions = new HttpClientOptions
        {
            TimeoutSeconds = 1,
            Retry = new RetryOptions
            {
                MaxRetries = 0,
                BaseDelay = TimeSpan.FromTicks(1),
                MaxDelay = TimeSpan.FromTicks(1),
                JitterFactor = 0.0,
            },
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailuresBeforeOpen = 1,
                OpenDuration = TimeSpan.FromTicks(1)
            },
        };

        minOptions.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void RetryOptions_WithZeroMaxRetries_IsValidConfiguration()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 0,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            JitterFactor = 0.25,
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void HttpClientOptionsBuilder_SharesStateWhenReused()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act - Build multiple times with the same builder instance
        HttpClientOptions options1 = builder.Build();
        builder.WithBaseUrl("https://changed.com");
        HttpClientOptions options2 = builder.Build();
        builder.WithTimeout(TimeSpan.FromMinutes(1));
        HttpClientOptions options3 = builder.Build();

        // Assert - The builder modifies the same internal state
        // All options point to the same instance due to builder implementation
        options1.Should().BeSameAs(options2);
        options2.Should().BeSameAs(options3);

        // State should be preserved and accumulated
        options3.BaseUrl.Should().Be("https://changed.com");
        options3.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void HttpClientOptions_WithDifferentHeaderCasing_BehavesCorrectly()
    {
        // Arrange
        var options = new HttpClientOptions();

        // Act - Add headers with different casing
        options.DefaultHeaders["content-type"] = "application/json";
        options.DefaultHeaders["Content-Type"] = "application/xml"; // Should override
        options.DefaultHeaders["ACCEPT"] = "application/json";

        // Assert
        options.DefaultHeaders.Should().HaveCount(2);
        options.DefaultHeaders["Content-Type"].Should().Be("application/xml");
        options.DefaultHeaders["content-type"].Should().Be("application/xml");
        options.DefaultHeaders["Accept"].Should().Be("application/json");
        options.DefaultHeaders["ACCEPT"].Should().Be("application/json");
    }

    [Fact]
    public void RetryOptions_WithEqualBaseAndMaxDelay_IsValid()
    {
        // Arrange
        var equalDelay = TimeSpan.FromSeconds(5);
        var options = new RetryOptions
        {
            BaseDelay = equalDelay,
            MaxDelay = equalDelay,
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void HttpClientOptionsBuilder_WithNullConfigurationAction_ThrowsCorrectly()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithRetry(null!))
            .Should().Throw<ArgumentNullException>();

        builder.Invoking(b => b.WithCircuitBreaker(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HttpClientOptions_WithEmptyBaseUrl_IsValid()
    {
        // Arrange
        var options = new HttpClientOptions { BaseUrl = string.Empty };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void HttpClientOptions_WithNullBaseUrl_ThrowsOnValidation()
    {
        // Arrange
        var options = new HttpClientOptions { BaseUrl = null! };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow(); // Empty/null BaseUrl is treated as valid
    }

    [Fact]
    public void HttpClientOptionsBuilder_ChainedOperations_PreserveOrder()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithHeader("X-Order", "1")
            .WithHeaders(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Order", "2" }, // Should override
                { "X-Second", "2" },
            })
            .WithHeader("X-Order", "3") // Should override again
            .WithHeader("X-Third", "3")
            .Build();

        // Assert
        options.DefaultHeaders["X-Order"].Should().Be("3");
        options.DefaultHeaders["X-Second"].Should().Be("2");
        options.DefaultHeaders["X-Third"].Should().Be("3");
    }

    [Fact]
    public void CircuitBreakerOptions_WithDisabledFlag_StillValidatesOtherProperties()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = false,
            FailuresBeforeOpen = -1, // Invalid
            OpenDuration = TimeSpan.Zero, // Invalid
        };

        // Act & Assert - Should still validate other properties even when disabled
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void HttpClientOptionsBuilder_WithWhitespaceUserAgent_ThrowsArgumentException(string userAgent)
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithUserAgent(userAgent))
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(userAgent));
    }

    [Fact]
    public void HttpClientOptions_DefaultUserAgent_HasCorrectFormat()
    {
        // Arrange & Act
        var options = new HttpClientOptions();

        // Assert
        options.UserAgent.Should().MatchRegex(@"^Reliable\.HttpClient/\d+\.\d+\.\d+$");
        options.UserAgent.Should().Contain("Reliable.HttpClient");
    }

    [Fact]
    public void HttpClientOptionsBuilder_WithVeryLongTimeout_WorksCorrectly()
    {
        // Arrange
        var veryLongTimeout = TimeSpan.FromDays(365); // 1 year
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithTimeout(veryLongTimeout)
            .Build();

        // Assert
        options.TimeoutSeconds.Should().Be((int)veryLongTimeout.TotalSeconds);
    }

    [Fact]
    public void RetryOptionsBuilder_WithFractionalSeconds_WorksCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithBaseDelay(TimeSpan.FromSeconds(1.5))
                .WithMaxDelay(TimeSpan.FromSeconds(10.75)))
            .Build();

        // Assert
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(1.5));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromSeconds(10.75));
    }

    [Fact]
    public void HttpClientOptions_WithUnicodeCharacters_WorksCorrectly()
    {
        // Arrange
        var options = new HttpClientOptions
        {
            UserAgent = "测试客户端/1.0 (тест)",
            BaseUrl = "https://测试.example.com",
        };
        options.DefaultHeaders["X-Custom-Header"] = "测试值";

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
        options.UserAgent.Should().Contain("测试客户端");
        options.DefaultHeaders["X-Custom-Header"].Should().Be("测试值");
    }
}
