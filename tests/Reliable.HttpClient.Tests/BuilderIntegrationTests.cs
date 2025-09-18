using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class BuilderIntegrationTests
{
    [Fact]
    public void FullConfigurationWorkflow_WithAllBuilders_WorksCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com/v1")
            .WithTimeout(TimeSpan.FromSeconds(45))
            .WithUserAgent("IntegrationTestClient/1.0")
            .WithHeader("Authorization", "Bearer integration-token")
            .WithHeader("X-API-Version", "v1")
            .WithHeaders(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Client-Id", "test-client" },
                { "Accept", "application/json" },
            })
            .WithRetry(retry => retry
                .WithMaxRetries(7)
                .WithBaseDelay(TimeSpan.FromMilliseconds(500))
                .WithMaxDelay(TimeSpan.FromSeconds(30))
                .WithJitter(0.4))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(8)
                .WithOpenDuration(TimeSpan.FromMinutes(2)))
            .Build();

        // Assert
        // Main options
        options.BaseUrl.Should().Be("https://api.example.com/v1");
        options.TimeoutSeconds.Should().Be(45);
        options.UserAgent.Should().Be("IntegrationTestClient/1.0");

        // Headers
        options.DefaultHeaders.Should().HaveCount(4);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer integration-token");
        options.DefaultHeaders["X-API-Version"].Should().Be("v1");
        options.DefaultHeaders["X-Client-Id"].Should().Be("test-client");
        options.DefaultHeaders["Accept"].Should().Be("application/json");

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(7);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.Retry.JitterFactor.Should().Be(0.4);

        // Circuit breaker configuration
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(8);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void BuilderChaining_WithHeaderManipulation_WorksCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithHeader("Initial-Header", "initial-value")
            .WithHeaders(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Header1", "value1" },
                { "Header2", "value2" },
                { "Header3", "value3" },
            })
            .WithHeader("Override-Header", "original")
            .WithHeader("Override-Header", "overridden") // Should override
            .WithoutHeader("Header2") // Should remove
            .WithHeader("Final-Header", "final-value")
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(5);
        options.DefaultHeaders["Initial-Header"].Should().Be("initial-value");
        options.DefaultHeaders["Header1"].Should().Be("value1");
        options.DefaultHeaders["Header3"].Should().Be("value3");
        options.DefaultHeaders["Override-Header"].Should().Be("overridden");
        options.DefaultHeaders["Final-Header"].Should().Be("final-value");
        options.DefaultHeaders.Should().NotContainKey("Header2");
    }

    [Fact]
    public void BuilderChaining_WithCircuitBreakerToggling_WorksCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(3)
                .WithOpenDuration(TimeSpan.FromSeconds(30)))
            .WithoutCircuitBreaker() // Disable after configuration
            .Build();

        // Assert
        options.CircuitBreaker.Enabled.Should().BeFalse();
        // Configuration should still be preserved
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(3);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void BuilderChaining_WithMultipleRetryConfigurations_LastConfigurationWins()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithMaxRetries(3)
                .WithBaseDelay(TimeSpan.FromSeconds(1)))
            .WithRetry(retry => retry
                .WithMaxRetries(5) // Should override previous
                .WithJitter(0.6))   // Should add to previous configuration
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(5);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(1)); // Should be preserved
        options.Retry.JitterFactor.Should().Be(0.6);
    }

    [Fact]
    public void ImplicitConversion_WorksInDifferentContexts()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://test.com")
            .WithTimeout(TimeSpan.FromSeconds(30));

        // Act & Assert - Implicit conversion in method parameter
        ValidateOptions(builder); // Should work without explicit conversion

        // Act & Assert - Implicit conversion in assignment
        HttpClientOptions options = builder;
        options.BaseUrl.Should().Be("https://test.com");
        options.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void BuilderReuse_AccumulatesChanges()
    {
        // Arrange
        HttpClientOptionsBuilder builder1 = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithRetry(retry => retry.WithMaxRetries(3));

        HttpClientOptionsBuilder builder2 = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithTimeout(TimeSpan.FromSeconds(60))
            .WithRetry(retry => retry.WithMaxRetries(5));

        // Act
        HttpClientOptions options1 = builder1.Build();
        HttpClientOptions options2 = builder2.Build();

        // Assert
        options1.Should().NotBeSameAs(options2);
        options1.TimeoutSeconds.Should().Be(30); // default
        options1.Retry.MaxRetries.Should().Be(3);

        options2.TimeoutSeconds.Should().Be(60); // modified
        options2.Retry.MaxRetries.Should().Be(5); // modified

        // Both should have the same base URL
        options1.BaseUrl.Should().Be("https://api.example.com");
        options2.BaseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    public void ComplexWorkflow_WithValidationErrors_ThrowsAtBuild()
    {
        // Arrange
        HttpClientOptionsBuilder builder = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithRetry(retry => retry
                .WithMaxRetries(5)
                .WithBaseDelay(TimeSpan.FromSeconds(10))
                .WithMaxDelay(TimeSpan.FromSeconds(5))); // Invalid: base > max

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(RetryOptions.BaseDelay));
    }

    [Fact]
    public void MinimalConfiguration_WithOnlyRequiredSettings_WorksCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://minimal.example.com")
            .Build();

        // Assert
        options.BaseUrl.Should().Be("https://minimal.example.com");
        // All other values should be defaults
        options.TimeoutSeconds.Should().Be(30);
        options.UserAgent.Should().Be("Reliable.HttpClient/1.2.0");
        options.DefaultHeaders.Should().BeEmpty();
        options.Retry.MaxRetries.Should().Be(3);
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5);
    }

    [Fact]
    public void HeadersClearingWorkflow_WorksCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithHeaders(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Header1", "value1" },
                { "Header2", "value2" },
                { "Header3", "value3" },
            })
            .WithoutHeaders() // Clear all headers
            .WithHeader("NewHeader", "new-value") // Add after clearing
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(1);
        options.DefaultHeaders["NewHeader"].Should().Be("new-value");
        options.DefaultHeaders.Should().NotContainKey("Header1");
        options.DefaultHeaders.Should().NotContainKey("Header2");
        options.DefaultHeaders.Should().NotContainKey("Header3");
    }

    private static void ValidateOptions(HttpClientOptions options)
    {
        options.Should().NotBeNull();
        options.BaseUrl.Should().Be("https://test.com");
        options.TimeoutSeconds.Should().Be(30);
    }
}
