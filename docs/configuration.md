# Configuration Reference

Complete reference for all configuration options in Reliable.HttpClient.

## Overview

Reliable.HttpClient provides sensible defaults that work well for most scenarios.
You only need to configure options that differ from these defaults.

## Configuration Methods

### 1. Traditional Configuration

Simple object property setting:

```csharp
.AddResilience(options =>
{
    // Configure retry settings
    options.Retry.MaxRetries = 5;

    // Configure circuit breaker settings
    options.CircuitBreaker.FailuresBeforeOpen = 10;
});
```

### 2. Fluent Builder Pattern

Strongly-typed, IntelliSense-friendly configuration:

```csharp
.AddResilience(builder => builder
    .WithBaseUrl("https://api.example.com")
    .WithTimeout(TimeSpan.FromMinutes(1))
    .WithUserAgent("MyApp/1.0")
    .WithRetry(retry => retry
        .WithMaxRetries(5)
        .WithBaseDelay(TimeSpan.FromSeconds(2))
        .WithJitter(0.3))
    .WithCircuitBreaker(cb => cb
        .WithFailureThreshold(10)
        .WithOpenDuration(TimeSpan.FromMinutes(2))));
```

### 3. Ready-made Presets

Pre-configured for common scenarios:

```csharp
// Use preset as-is
.AddResilience(HttpClientPresets.SlowExternalApi())

// Use preset with customization
.AddResilience(HttpClientPresets.FileDownload(), options =>
{
    options.BaseUrl = "https://cdn.example.com";
    options.UserAgent = "MyApp/2.0";
})
```

## Built-in Presets

| Preset                    | Timeout | Retries | Base Delay | Circuit Breaker    | Use Case                    |
|---------------------------|---------|---------|------------|--------------------|-----------------------------|
| `FastInternalApi()`       | 10s     | 5       | 200ms      | 3 failures, 30s   | Internal microservices      |
| `SlowExternalApi()`       | 120s    | 2       | 2s         | 8 failures, 5m    | Third-party APIs            |
| `FileDownload()`          | 30m     | 3       | 1s         | Disabled           | File/blob downloads         |
| `RealTimeApi()`           | 5s      | 1       | 100ms      | 10 failures, 10s  | Low-latency APIs            |
| `AuthenticationApi()`     | 15s     | 2       | 500ms      | 5 failures, 2m    | Auth/token endpoints        |
| `Webhook()`               | 30s     | 1       | 1s         | Disabled           | Webhook deliveries          |

### Using Presets

```csharp
// For external APIs with high latency tolerance
services.AddHttpClient<ExternalApiClient>()
    .AddResilience(HttpClientPresets.SlowExternalApi());

// For internal microservices
services.AddHttpClient<InternalApiClient>()
    .AddResilience(HttpClientPresets.FastInternalApi());

// For file downloads
services.AddHttpClient<FileDownloadClient>()
    .AddResilience(HttpClientPresets.FileDownload());
```

### Customizing Presets

You can customize presets by passing an additional configuration action:

```csharp
services.AddHttpClient<MyApiClient>()
    .AddResilience(HttpClientPresets.SlowExternalApi(), options =>
    {
        // Override specific settings
        options.Retry.MaxRetries = 5;
        options.UserAgent = "MyApp/1.0";
    });
```

## Main Configuration Class

### HttpClientOptions

| Property         | Type                              | Default                    | Description                            |
|------------------|-----------------------------------|----------------------------|----------------------------------------|
| `BaseUrl`        | `string`                          | `""`                       | Base API URL                           |
| `TimeoutSeconds` | `int`                             | `30`                       | Request timeout in seconds             |
| `UserAgent`      | `string`                          | `"Reliable.HttpClient/1.2.0"` | User-Agent header                  |
| `DefaultHeaders` | `IDictionary<string, string>`     | `{}`                       | Default headers for all requests. See [Advanced Usage](advanced-usage.md#header-management) for OAuth and dynamic headers. |
| `Retry`          | `RetryOptions`                    | See below                  | Retry policy configuration             |
| `CircuitBreaker` | `CircuitBreakerOptions`           | See below                  | Circuit breaker configuration          |

```csharp
.AddResilience(options =>
{
    // Basic settings
    options.BaseUrl = "https://api.example.com";
    options.TimeoutSeconds = 60;
    options.UserAgent = "MyApp/1.0";

    // Basic headers (for advanced patterns see Advanced Usage guide)
    options.DefaultHeaders["X-API-Key"] = "your-api-key";

    // Configure retry settings
    options.Retry.MaxRetries = 5;

    // Configure circuit breaker settings
    options.CircuitBreaker.FailuresBeforeOpen = 10;
});
```

## Retry Configuration

### RetryOptions

| Property         | Type                     | Default     | Description                       |
|------------------|--------------------------|-------------|-----------------------------------|
| `MaxRetries`     | `int`                    | `3`         | Maximum number of retry attempts  |
| `BaseDelay`      | `TimeSpan`               | `1 second`. | Base delay between retries        |
| `MaxDelay`       | `TimeSpan`               | `30 seconds`| Maximum delay between retries     |
| `JitterFactor`   | `double`                 | `0.25`      | Randomization factor (0.0 to 1.0) |

#### Retry Behavior

The retry policy uses exponential backoff with jitter by default. The actual delay is calculated
using the base delay, exponential backoff, and jitter factor to prevent thundering herd effects.

#### Retry Configuration Example

```csharp
options.Retry = new RetryOptions
{
    MaxRetries = 5,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(2),
    JitterFactor = 0.3
};
```

## Circuit Breaker Configuration

### CircuitBreakerOptions

| Property             | Type        | Default    | Description                                   |
|----------------------|-------------|------------|-----------------------------------------------|
| `Enabled`            | `bool`      | `true`     | Whether circuit breaker is enabled            |
| `FailuresBeforeOpen` | `int`       | `5`        | Number of consecutive failures before opening |
| `OpenDuration`       | `TimeSpan`  | `1 minute` | How long circuit stays open                   |

#### Circuit Breaker Configuration Example

```csharp
options.CircuitBreaker = new CircuitBreakerOptions
{
    FailuresBeforeOpen = 10,
    OpenDuration = TimeSpan.FromMinutes(5),
    Enabled = true
};
```

## Validation

All configuration is automatically validated when the HttpClient is created.
Invalid configurations throw `ArgumentException` with descriptive error messages.

### Common Validation Rules

- `MaxRetries` must be ≥ 0
- `BaseDelay` must be > 0
- `MaxDelay` must be > 0
- `BaseDelay` cannot be > `MaxDelay`
- `JitterFactor` must be between 0.0 and 1.0
- `FailuresBeforeOpen` must be > 0
- `OpenDuration` must be > 0

## Environment-Specific Configuration

### Development vs Production

```csharp
#if DEBUG
    .AddResilience(options =>
    {
        options.Retry.MaxRetries = 1; // Fast fail in development
        options.CircuitBreaker.Enabled = false;
    });
#else
    .AddResilience(); // Use production defaults
#endif
```

### Configuration from appsettings.json

```json
{
  "HttpClient": {
    "Retry": {
      "MaxRetries": 3,
      "BaseDelay": "00:00:02",
      "MaxDelay": "00:00:30"
    },
    "CircuitBreaker": {
      "FailuresBeforeOpen": 5,
      "OpenDuration": "00:01:00"
    }
  }
}
```

```csharp
var config = configuration.GetSection("HttpClient").Get<HttpClientOptions>();
services.AddHttpClient("api").AddResilience(config);
```

## Advanced Configuration

For advanced scenarios including OAuth tokens, dynamic headers, authentication patterns, and complex API integrations,
see the [Advanced Usage Guide](advanced-usage.md).
