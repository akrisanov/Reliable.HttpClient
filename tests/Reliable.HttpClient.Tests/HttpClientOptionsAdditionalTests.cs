using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientOptionsAdditionalTests
{
    [Fact]
    public void DefaultHeaders_IsCaseInsensitive()
    {
        // Arrange
        var options = new HttpClientOptions();

        // Act
        options.DefaultHeaders["authorization"] = "Bearer token";

        // Assert
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.DefaultHeaders["AUTHORIZATION"].Should().Be("Bearer token");
        options.DefaultHeaders.Should().ContainKey("authorization");
        options.DefaultHeaders.Should().ContainKey("Authorization");
        options.DefaultHeaders.Should().ContainKey("AUTHORIZATION");
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new HttpClientOptions();

        // Assert
        options.BaseUrl.Should().BeEmpty();
        options.TimeoutSeconds.Should().Be(30);
        options.UserAgent.Should().Be("Reliable.HttpClient/1.1.0");
        options.DefaultHeaders.Should().NotBeNull();
        options.DefaultHeaders.Should().BeEmpty();
        options.Retry.Should().NotBeNull();
        options.CircuitBreaker.Should().NotBeNull();
    }

    [Fact]
    public void Retry_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new HttpClientOptions();

        // Assert
        options.Retry.MaxRetries.Should().Be(3);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(1_000));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromMilliseconds(30_000));
        options.Retry.JitterFactor.Should().Be(0.25);
    }

    [Fact]
    public void CircuitBreaker_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new HttpClientOptions();

        // Assert
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMilliseconds(60_000));
    }

    [Fact]
    public void Validate_CallsChildObjectValidation()
    {
        // Arrange
        var options = new HttpClientOptions
        {
            Retry = new RetryOptions { MaxRetries = -1 }, // Invalid retry configuration
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.MaxRetries));
    }

    [Fact]
    public void Validate_CallsCircuitBreakerValidation()
    {
        // Arrange
        var options = new HttpClientOptions
        {
            CircuitBreaker = new CircuitBreakerOptions { FailuresBeforeOpen = 0 }, // Invalid circuit breaker configuration
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(CircuitBreakerOptions.FailuresBeforeOpen));
    }

    [Fact]
    public void DefaultHeaders_CanBeModified()
    {
        // Arrange
        var options = new HttpClientOptions();

        // Act
        options.DefaultHeaders["X-API-Key"] = "test-key";
        options.DefaultHeaders["Authorization"] = "Bearer token";

        // Assert
        options.DefaultHeaders.Should().HaveCount(2);
        options.DefaultHeaders["X-API-Key"].Should().Be("test-key");
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
    }

    [Fact]
    public void DefaultHeaders_CanOverwriteExistingValues()
    {
        // Arrange
        var options = new HttpClientOptions();
        options.DefaultHeaders["Authorization"] = "Bearer old-token";

        // Act
        options.DefaultHeaders["Authorization"] = "Bearer new-token";

        // Assert
        options.DefaultHeaders.Should().HaveCount(1);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer new-token");
    }

    [Fact]
    public void DefaultHeaders_CanBeCleared()
    {
        // Arrange
        var options = new HttpClientOptions();
        options.DefaultHeaders["Header1"] = "Value1";
        options.DefaultHeaders["Header2"] = "Value2";

        // Act
        options.DefaultHeaders.Clear();

        // Assert
        options.DefaultHeaders.Should().BeEmpty();
    }

    [Fact]
    public void DefaultHeaders_CanRemoveSpecificHeaders()
    {
        // Arrange
        var options = new HttpClientOptions();
        options.DefaultHeaders["Header1"] = "Value1";
        options.DefaultHeaders["Header2"] = "Value2";
        options.DefaultHeaders["Header3"] = "Value3";

        // Act
        var removed = options.DefaultHeaders.Remove("Header2");

        // Assert
        removed.Should().BeTrue();
        options.DefaultHeaders.Should().HaveCount(2);
        options.DefaultHeaders.Should().ContainKey("Header1");
        options.DefaultHeaders.Should().ContainKey("Header3");
        options.DefaultHeaders.Should().NotContainKey("Header2");
    }

    [Theory]
    [InlineData("https://api.example.com/v1/")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://subdomain.example.com:443/api")]
    public void Validate_WithVariousValidBaseUrls_ShouldNotThrow(string baseUrl)
    {
        // Arrange
        var options = new HttpClientOptions { BaseUrl = baseUrl };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithComplexValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var options = new HttpClientOptions
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 45,
            UserAgent = "TestClient/2.0",
            Retry = new RetryOptions
            {
                MaxRetries = 5,
                BaseDelay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromMinutes(1),
                JitterFactor = 0.5,
            },
            CircuitBreaker = new CircuitBreakerOptions
            {
                Enabled = true,
                FailuresBeforeOpen = 3,
                OpenDuration = TimeSpan.FromSeconds(30),
            },
        };
        options.DefaultHeaders["Authorization"] = "Bearer token";
        options.DefaultHeaders["X-API-Key"] = "api-key";

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Properties_CanBeSetIndependently()
    {
        // Arrange
        var options = new HttpClientOptions
        {
            // Act
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 60,
            UserAgent = "Custom/1.0",
        };

        // Assert
        options.BaseUrl.Should().Be("https://api.example.com");
        options.TimeoutSeconds.Should().Be(60);
        options.UserAgent.Should().Be("Custom/1.0");
        // Other properties should remain default
        options.Retry.MaxRetries.Should().Be(3); // default
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5); // default
    }

    [Fact]
    public void ChildObjects_AreNotNull_ByDefault()
    {
        // Arrange & Act
        var options = new HttpClientOptions();

        // Assert
        options.Retry.Should().NotBeNull();
        options.CircuitBreaker.Should().NotBeNull();
        options.DefaultHeaders.Should().NotBeNull();
    }

    [Fact]
    public void ChildObjects_CanBeReplaced()
    {
        // Arrange
        var options = new HttpClientOptions();
        var customRetry = new RetryOptions { MaxRetries = 10 };
        var customCircuitBreaker = new CircuitBreakerOptions { FailuresBeforeOpen = 2 };

        // Act
        options.Retry = customRetry;
        options.CircuitBreaker = customCircuitBreaker;

        // Assert
        options.Retry.Should().BeSameAs(customRetry);
        options.CircuitBreaker.Should().BeSameAs(customCircuitBreaker);
        options.Retry.MaxRetries.Should().Be(10);
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(2);
    }
}
