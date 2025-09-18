using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class CircuitBreakerOptionsBuilderTests
{
    [Fact]
    public void WithFailureThreshold_SetsFailuresBeforeOpenCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb.WithFailureThreshold(3))
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(3);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void WithFailureThreshold_WithValidValues_SetsCorrectly(int threshold)
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb.WithFailureThreshold(threshold))
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(threshold);
    }

    [Fact]
    public void WithOpenDuration_SetsOpenDurationCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(2);

        // Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb.WithOpenDuration(duration))
            .Build();

        // Assert
        options.CircuitBreaker.OpenDuration.Should().Be(duration);
    }

    [Fact]
    public void FluentInterface_ChainsMethodsCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(7)
                .WithOpenDuration(TimeSpan.FromMinutes(3)))
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(7);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(3));
        options.CircuitBreaker.Enabled.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreakerOptionsBuilder_PreservesExistingValues_WhenNotOverridden()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(10) // Only change FailuresBeforeOpen
                                          // Leave OpenDuration as default
            )
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(10);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(1)); // default value
        options.CircuitBreaker.Enabled.Should().BeTrue(); // default value
    }

    [Fact]
    public void WithFailureThreshold_ChainedCalls_LastCallWins()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(3)
                .WithFailureThreshold(5)
                .WithFailureThreshold(8))
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(8);
    }

    [Fact]
    public void WithOpenDuration_WithDifferentTimeSpanFormats_WorksCorrectly()
    {
        // Act & Assert - Seconds
        HttpClientOptions options1 = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb.WithOpenDuration(TimeSpan.FromSeconds(45)))
            .Build();
        options1.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(45));

        // Act & Assert - Minutes
        HttpClientOptions options2 = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb.WithOpenDuration(TimeSpan.FromMinutes(2)))
            .Build();
        options2.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(2));

        // Act & Assert - Milliseconds
        HttpClientOptions options3 = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb.WithOpenDuration(TimeSpan.FromMilliseconds(30000)))
            .Build();
        options3.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMilliseconds(30000));
    }

    [Fact]
    public void CircuitBreakerOptions_EnabledByDefault_WhenConfigured()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(5)
                .WithOpenDuration(TimeSpan.FromSeconds(30)))
            .Build();

        // Assert
        options.CircuitBreaker.Enabled.Should().BeTrue();
    }

    [Fact]
    public void WithoutCircuitBreaker_OverridesWithCircuitBreaker()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(3)
                .WithOpenDuration(TimeSpan.FromMinutes(1)))
            .WithoutCircuitBreaker() // Disable after configuration
            .Build();

        // Assert
        options.CircuitBreaker.Enabled.Should().BeFalse();
        // Configuration values should still be set
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(3);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CircuitBreakerOptionsBuilder_CanOverrideMultipleProperties()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(15)
                .WithOpenDuration(TimeSpan.FromMinutes(5)))
            .Build();

        // Assert
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(15);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(5));
        options.CircuitBreaker.Enabled.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreakerOptionsBuilder_WorksWithOtherBuilders()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithMaxRetries(3))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(5)
                .WithOpenDuration(TimeSpan.FromSeconds(45)))
            .WithHeader("Authorization", "Bearer token")
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(3);
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(45));
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
    }
}
