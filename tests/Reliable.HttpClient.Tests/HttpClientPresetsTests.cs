using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientPresetsTests
{
    [Fact]
    public void FastInternalApi_ReturnsCorrectConfiguration()
    {
        // Act
        HttpClientOptions options = HttpClientPresets.FastInternalApi();

        // Assert
        options.Should().NotBeNull();
        options.TimeoutSeconds.Should().Be(10);

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(5);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(200));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromSeconds(5));

        // Circuit breaker configuration
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(3);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void SlowExternalApi_ReturnsCorrectConfiguration()
    {
        // Act
        HttpClientOptions options = HttpClientPresets.SlowExternalApi();

        // Assert
        options.Should().NotBeNull();
        options.TimeoutSeconds.Should().Be(120);

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(2);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.Retry.JitterFactor.Should().Be(0.5);

        // Circuit breaker configuration
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(8);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void FileDownload_ReturnsCorrectConfiguration()
    {
        // Act
        HttpClientOptions options = HttpClientPresets.FileDownload();

        // Assert
        options.Should().NotBeNull();
        options.TimeoutSeconds.Should().Be(1800); // 30 minutes in seconds

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(3);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(1));
        options.Retry.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));

        // Circuit breaker should be disabled
        options.CircuitBreaker.Enabled.Should().BeFalse();
    }

    [Fact]
    public void RealTimeApi_ReturnsCorrectConfiguration()
    {
        // Act
        HttpClientOptions options = HttpClientPresets.RealTimeApi();

        // Assert
        options.Should().NotBeNull();
        options.TimeoutSeconds.Should().Be(5);

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(1);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.Retry.JitterFactor.Should().Be(0.0); // No jitter

        // Circuit breaker configuration
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(10);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void AuthenticationApi_ReturnsCorrectConfiguration()
    {
        // Act
        HttpClientOptions options = HttpClientPresets.AuthenticationApi();

        // Assert
        options.Should().NotBeNull();
        options.TimeoutSeconds.Should().Be(15);

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(2);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(500));

        // Circuit breaker configuration
        options.CircuitBreaker.Enabled.Should().BeTrue();
        options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5);
        options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void Webhook_ReturnsCorrectConfiguration()
    {
        // Act
        HttpClientOptions options = HttpClientPresets.Webhook();

        // Assert
        options.Should().NotBeNull();
        options.TimeoutSeconds.Should().Be(30);

        // Retry configuration
        options.Retry.MaxRetries.Should().Be(1);
        options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(1));

        // Circuit breaker should be disabled
        options.CircuitBreaker.Enabled.Should().BeFalse();
    }

    [Fact]
    public void AllPresets_ProduceValidConfigurations()
    {
        // Act & Assert - All presets should validate without throwing
        HttpClientPresets.FastInternalApi().Invoking(o => o.Validate()).Should().NotThrow();
        HttpClientPresets.SlowExternalApi().Invoking(o => o.Validate()).Should().NotThrow();
        HttpClientPresets.FileDownload().Invoking(o => o.Validate()).Should().NotThrow();
        HttpClientPresets.RealTimeApi().Invoking(o => o.Validate()).Should().NotThrow();
        HttpClientPresets.AuthenticationApi().Invoking(o => o.Validate()).Should().NotThrow();
        HttpClientPresets.Webhook().Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void EachPreset_ReturnsNewInstance()
    {
        // Act
        HttpClientOptions options1 = HttpClientPresets.FastInternalApi();
        HttpClientOptions options2 = HttpClientPresets.FastInternalApi();

        // Assert
        options1.Should().NotBeSameAs(options2);
        options1.Should().BeEquivalentTo(options2); // Same values but different instances
    }

    [Fact]
    public void Presets_HaveDifferentTimeouts()
    {
        // Act
        HttpClientOptions fastApi = HttpClientPresets.FastInternalApi();
        HttpClientOptions slowApi = HttpClientPresets.SlowExternalApi();
        HttpClientOptions fileDownload = HttpClientPresets.FileDownload();
        HttpClientOptions realTime = HttpClientPresets.RealTimeApi();
        HttpClientOptions auth = HttpClientPresets.AuthenticationApi();
        HttpClientOptions webhook = HttpClientPresets.Webhook();

        // Assert - All should have different timeout values
        var timeouts = new[]
        {
            fastApi.TimeoutSeconds,
            slowApi.TimeoutSeconds,
            fileDownload.TimeoutSeconds,
            realTime.TimeoutSeconds,
            auth.TimeoutSeconds,
            webhook.TimeoutSeconds,
        };

        timeouts.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Presets_HaveDifferentRetryStrategies()
    {
        // Act
        HttpClientOptions fastApi = HttpClientPresets.FastInternalApi();
        HttpClientOptions slowApi = HttpClientPresets.SlowExternalApi();
        HttpClientOptions realTime = HttpClientPresets.RealTimeApi();

        // Assert - Different retry strategies
        fastApi.Retry.MaxRetries.Should().NotBe(slowApi.Retry.MaxRetries);
        fastApi.Retry.MaxRetries.Should().NotBe(realTime.Retry.MaxRetries);
        slowApi.Retry.MaxRetries.Should().NotBe(realTime.Retry.MaxRetries);

        fastApi.Retry.BaseDelay.Should().NotBe(slowApi.Retry.BaseDelay);
        fastApi.Retry.BaseDelay.Should().NotBe(realTime.Retry.BaseDelay);
        slowApi.Retry.BaseDelay.Should().NotBe(realTime.Retry.BaseDelay);
    }

    [Fact]
    public void CircuitBreakerDisabledPresets_AreCorrect()
    {
        // Act
        HttpClientOptions fileDownload = HttpClientPresets.FileDownload();
        HttpClientOptions webhook = HttpClientPresets.Webhook();

        // Assert
        fileDownload.CircuitBreaker.Enabled.Should().BeFalse();
        webhook.CircuitBreaker.Enabled.Should().BeFalse();

        // But configuration values should still be preserved (just disabled)
        fileDownload.CircuitBreaker.FailuresBeforeOpen.Should().BeGreaterThan(0);
        fileDownload.CircuitBreaker.OpenDuration.Should().BeGreaterThan(TimeSpan.Zero);
        webhook.CircuitBreaker.FailuresBeforeOpen.Should().BeGreaterThan(0);
        webhook.CircuitBreaker.OpenDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Presets_CanBeCustomizedFurther()
    {
        // Act - Take a preset and customize it further
        HttpClientOptions customized = new HttpClientOptionsBuilder()
            .WithBaseUrl("https://api.example.com")
            .WithHeader("Authorization", "Bearer token")
            .WithRetry(retry => retry
                .WithMaxRetries(HttpClientPresets.FastInternalApi().Retry.MaxRetries) // Use preset value
                .WithBaseDelay(TimeSpan.FromMilliseconds(100))) // Override with custom value
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(HttpClientPresets.FastInternalApi().CircuitBreaker.FailuresBeforeOpen) // Use preset value
                .WithOpenDuration(TimeSpan.FromMinutes(1))) // Override with custom value
            .Build();

        // Assert
        customized.BaseUrl.Should().Be("https://api.example.com");
        customized.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        customized.Retry.MaxRetries.Should().Be(HttpClientPresets.FastInternalApi().Retry.MaxRetries);
        customized.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        customized.CircuitBreaker.FailuresBeforeOpen.Should().Be(HttpClientPresets.FastInternalApi().CircuitBreaker.FailuresBeforeOpen);
        customized.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(1));
    }
}
