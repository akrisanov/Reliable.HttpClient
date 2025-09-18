# Reliable.HttpClient.Caching

[![NuGet Version](https://img.shields.io/nuget/v/Reliable.HttpClient.Caching)](https://www.nuget.org/packages/Reliable.HttpClient.Caching/)

Intelligent HTTP response caching extension for [Reliable.HttpClient](https://www.nuget.org/packages/Reliable.HttpClient/)
with preset-based configuration, **custom headers support**, and automatic memory management.

## Installation

```bash
dotnet add package Reliable.HttpClient.Caching
```

## Quick Start

```csharp
// Zero configuration - just add resilience with caching
services.AddHttpClient<WeatherApiClient>()
    .AddResilienceWithMediumTermCache<WeatherResponse>(); // 10 minutes cache

// Configure default headers for all requests
services.AddHttpClient<ApiClient>()
    .AddMemoryCache(options => options
        .WithDefaultExpiry(TimeSpan.FromMinutes(5))
        .AddHeader("Authorization", "Bearer token")
        .AddHeader("X-API-Version", "1.0"));

// Use anywhere with per-request header customization
public class WeatherService(CachedHttpClient<WeatherResponse> client)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await client.GetFromJsonAsync($"/weather?city={city}");

    // Add custom headers per request
    public async Task<WeatherResponse> GetWeatherWithTokenAsync(string city, string token)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {token}",
            ["X-Request-ID"] = Guid.NewGuid().ToString()
        };
        return await client.GetFromJsonAsync($"/weather?city={city}", headers);
    }
}
```

## Why This Package?

- **Zero Configuration** – Works out of the box with sensible defaults
- **✨ Custom Headers Support** – Default headers + per-request header customization with smart cache key generation
- **Preset-Based Setup** – 6 ready-made configurations for common scenarios
- **Automatic Dependencies** – No need to manually register `IMemoryCache`
- **Combined APIs** – Resilience + Caching in one method call
- **Performance Optimized** – Smart cache keys, configurable expiry, memory-efficient

## Caching Approaches

This package provides two caching approaches:

### 🌐 Universal Caching

- **Best for**: Multiple response types in a single client
- **Key class**: `HttpClientWithCache`
- **Use case**: Flexible, multi-type scenarios

### 🎯 Generic Caching

- **Best for**: Type-safe caching for specific response types
- **Key class**: `CachedHttpClient<TResponse>`
- **Use case**: High-performance, type-safe scenarios
- **📚 [See Generic Documentation →](Generic/README.md)**

> 🎯 **Need help choosing?** See our [Choosing Guide](../../docs/choosing-approach.md)

## Ready-Made Presets

Choose from preset configurations for common scenarios:

```csharp
// Short-term (1 minute) - frequently changing data
services.AddHttpClient<ApiClient>()
    .AddShortTermCache<ApiResponse>();

// Medium-term (10 minutes) - moderately stable data
services.AddHttpClient<ApiClient>()
    .AddMediumTermCache<ApiResponse>();

// Long-term (1 hour) - stable reference data
services.AddHttpClient<ApiClient>()
    .AddLongTermCache<ApiResponse>();
```

## Documentation

📖 **[Complete Documentation](../../docs/caching.md)** - Full caching guide with examples
🎯 **[Choosing Guide](../../docs/choosing-approach.md)** - Which approach to use when
📝 **[Examples](../../docs/examples/)** - Real-world usage scenarios

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
