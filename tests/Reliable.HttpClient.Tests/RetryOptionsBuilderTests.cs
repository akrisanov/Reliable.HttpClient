using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class RetryOptionsBuilderTests
{
    [Fact]
    public void WithMaxRetries_SetsMaxRetriesCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithMaxRetries(5))
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void WithMaxRetries_WithValidValues_SetsCorrectly(int maxRetries)
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithMaxRetries(maxRetries))
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(maxRetries);
    }

    [Fact]
    public void WithBaseDelay_SetsBaseDelayCorrectly()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(2);

        // Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithBaseDelay(delay))
            .Build();

        // Assert
        options.Retry.BaseDelay.Should().Be(delay);
    }

    [Fact]
    public void WithMaxDelay_SetsMaxDelayCorrectly()
    {
        // Arrange
        var delay = TimeSpan.FromMinutes(2);

        // Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithMaxDelay(delay))
            .Build();

        // Assert
        options.Retry.MaxDelay.Should().Be(delay);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void WithJitter_WithValidFactor_SetsJitterFactorCorrectly(double factor)
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithJitter(factor))
            .Build();

        // Assert
        options.Retry.JitterFactor.Should().Be(factor);
    }

    [Fact]
    public void WithoutJitter_SetsJitterFactorToZero()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithJitter(0.5) // Set some initial value
                .WithoutJitter()) // Then disable jitter
            .Build();

        // Assert
        options.Retry.JitterFactor.Should().Be(0.0);
    }

    [Fact]
    public void FluentInterface_ChainsMethodsCorrectly()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithMaxRetries(5)
                .WithBaseDelay(TimeSpan.FromSeconds(2))
                .WithMaxDelay(TimeSpan.FromMinutes(1))
                .WithJitter(0.3))
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(5);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromMinutes(1));
        options.Retry.JitterFactor.Should().Be(0.3);
    }

    [Fact]
    public void WithJitter_WithBoundaryValues_WorksCorrectly()
    {
        // Act & Assert - Minimum boundary
        HttpClientOptions options1 = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithJitter(0.0))
            .Build();
        options1.Retry.JitterFactor.Should().Be(0.0);

        // Act & Assert - Maximum boundary
        HttpClientOptions options2 = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithJitter(1.0))
            .Build();
        options2.Retry.JitterFactor.Should().Be(1.0);
    }

    [Fact]
    public void WithMaxRetries_ChainedCalls_LastCallWins()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithMaxRetries(3)
                .WithMaxRetries(5)
                .WithMaxRetries(7))
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(7);
    }

    [Fact]
    public void WithBaseDelay_WithDifferentTimeSpanFormats_WorksCorrectly()
    {
        // Act & Assert - Milliseconds
        HttpClientOptions options1 = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithBaseDelay(TimeSpan.FromMilliseconds(500)))
            .Build();
        options1.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(500));

        // Act & Assert - Seconds
        HttpClientOptions options2 = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry.WithBaseDelay(TimeSpan.FromSeconds(2.5)))
            .Build();
        options2.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(2.5));

        // Act & Assert - Minutes (but less than max delay)
        HttpClientOptions options3 = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithBaseDelay(TimeSpan.FromSeconds(10))
                .WithMaxDelay(TimeSpan.FromMinutes(2))) // Increase max delay first
            .Build();
        options3.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(10));
        options3.Retry.MaxDelay.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void RetryOptionsBuilder_PreservesExistingValues_WhenNotOverridden()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithMaxRetries(7) // Only change MaxRetries
                                   // Leave other properties as defaults
            )
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(7);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(1)); // default value
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromSeconds(30)); // default value
        options.Retry.JitterFactor.Should().Be(0.25); // default value
    }

    [Fact]
    public void RetryOptionsBuilder_CanOverrideMultipleProperties()
    {
        // Arrange & Act
        HttpClientOptions options = new HttpClientOptionsBuilder()
            .WithRetry(retry => retry
                .WithMaxRetries(10)
                .WithBaseDelay(TimeSpan.FromSeconds(3))
                .WithMaxDelay(TimeSpan.FromMinutes(5))
                .WithJitter(0.8))
            .Build();

        // Assert
        options.Retry.MaxRetries.Should().Be(10);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(3));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromMinutes(5));
        options.Retry.JitterFactor.Should().Be(0.8);
    }
}
