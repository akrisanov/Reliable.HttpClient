using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientOptionsBuilderTests
{
    [Fact]
    public void WithBaseUrl_SetsBaseUrlCorrectly()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();
        const string baseUrl = "https://api.example.com";

        // Act
        HttpClientOptions options = builder
            .WithBaseUrl(baseUrl)
            .Build();

        // Assert
        options.BaseUrl.Should().Be(baseUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithBaseUrl_WithNullOrWhitespace_ThrowsArgumentException(string baseUrl)
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithBaseUrl(baseUrl))
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(baseUrl));
    }

    [Fact]
    public void WithBaseUrl_WithNull_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithBaseUrl(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("baseUrl");
    }

    [Fact]
    public void WithTimeout_SetsTimeoutCorrectly()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();
        var timeout = TimeSpan.FromSeconds(45);

        // Act
        HttpClientOptions options = builder
            .WithTimeout(timeout)
            .Build();

        // Assert
        options.TimeoutSeconds.Should().Be(45);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithTimeout_WithInvalidTimeout_ThrowsArgumentOutOfRangeException(int seconds)
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();
        var timeout = TimeSpan.FromSeconds(seconds);

        // Act & Assert
        builder.Invoking(b => b.WithTimeout(timeout))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout");
    }

    [Fact]
    public void WithUserAgent_SetsUserAgentCorrectly()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();
        const string userAgent = "MyApp/1.0";

        // Act
        HttpClientOptions options = builder
            .WithUserAgent(userAgent)
            .Build();

        // Assert
        options.UserAgent.Should().Be(userAgent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithUserAgent_WithNullOrWhitespace_ThrowsArgumentException(string userAgent)
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithUserAgent(userAgent))
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(userAgent));
    }

    [Fact]
    public void WithUserAgent_WithNull_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithUserAgent(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("userAgent");
    }

    [Fact]
    public void WithRetry_ConfiguresRetryOptions()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithRetry(retry => retry
                .WithMaxRetries(5)
                .WithBaseDelay(TimeSpan.FromSeconds(2))
                .WithJitter(0.5))
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(5);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        options.Retry.JitterFactor.Should().Be(0.5);
    }

    [Fact]
    public void WithRetry_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithRetry(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithCircuitBreaker_ConfiguresCircuitBreakerOptions()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(3)
                .WithOpenDuration(TimeSpan.FromSeconds(30)))
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(3);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.CircuitBreaker.Enabled.Should().BeTrue();
    }

    [Fact]
    public void WithCircuitBreaker_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithCircuitBreaker(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithoutCircuitBreaker_DisablesCircuitBreaker()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithoutCircuitBreaker()
            .Build();

        // Assert
        options.CircuitBreaker.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Build_ValidatesOptionsBeforeReturning()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert - Should not throw with valid default options
        builder.Invoking(b => b.Build())
            .Should().NotThrow();

        // Act & Assert - Should throw with invalid timeout during WithTimeout call
        builder.Invoking(b => b.WithTimeout(TimeSpan.Zero))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImplicitOperator_ConvertsBuilderToOptions()
    {
        // Arrange
        HttpClientOptionsBuilder builder = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithTimeout(TimeSpan.FromSeconds(30));

        // Act
        HttpClientOptions options = builder; // implicit conversion

        // Assert
        options.Should().NotBeNull();
        options.BaseUrl.Should().Be("https://api.example.com");
        options.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void FluentInterface_ChainsMethodsCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithTimeout(TimeSpan.FromSeconds(60))
            .WithUserAgent("TestClient/2.0")
            .WithHeader("Authorization", "Bearer token")
            .WithRetry(retry => retry
                .WithMaxRetries(3)
                .WithBaseDelay(TimeSpan.FromMilliseconds(500)))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(5)
                .WithOpenDuration(TimeSpan.FromMinutes(1)))
            .Build();

        // Assert
        options.BaseUrl.Should().Be("https://api.example.com");
        options.TimeoutSeconds.Should().Be(60);
        options.UserAgent.Should().Be("TestClient/2.0");
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.Retry.MaxRetries.Should().Be(3);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(1));
        options.CircuitBreaker.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Build_ReturnsConfiguredOptions()
    {
        // Arrange
        HttpClientOptionsBuilder builder1 = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com");

        HttpClientOptionsBuilder builder2 = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithTimeout(TimeSpan.FromSeconds(60));

        // Act
        HttpClientOptions options1 = builder1.Build();
        HttpClientOptions options2 = builder2.Build();

        // Assert
        options1.Should().NotBeSameAs(options2);
        options1.TimeoutSeconds.Should().Be(30); // default value
        options2.TimeoutSeconds.Should().Be(60); // modified value
    }
}
