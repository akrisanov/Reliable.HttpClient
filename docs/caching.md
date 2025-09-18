# HTTP Response Caching

Learn how to add intelligent HTTP response caching to your resilient HttpClient with Reliable.HttpClient.Caching.

## Overview

Reliable.HttpClient.Caching extends the core resilience features with intelligent HTTP response caching capabilities. It provides automatic response caching, cache invalidation, and custom cache providers.

## 🏗️ Caching Architecture

We provide two distinct caching approaches to meet different needs:

### 🎯 Generic Caching (Type-Safe)
- **Namespace**: `Reliable.HttpClient.Caching.Generic`
- **Best for**: Known response types, compile-time safety
- **Key class**: `CachedHttpClient<TResponse>`

### 🌐 Universal Caching (Multi-Type)
- **Namespace**: `Reliable.HttpClient.Caching`
- **Best for**: Multiple response types, flexible scenarios
- **Key class**: `HttpClientWithCache`

> 📖 **[Choosing the Right Approach →](choosing-approach.md)** - Detailed comparison and decision guide

## Installation

```bash
# Install the core package first
dotnet add package Reliable.HttpClient

# Add the caching extension
dotnet add package Reliable.HttpClient.Caching
```

## Quick Start

### 🎯 Generic Caching (Type-Safe) - Recommended for Known Types

Perfect when you have well-defined response types:

```csharp
using Reliable.HttpClient.Caching.Generic;
using Reliable.HttpClient.Caching.Generic.Extensions;

// Register generic caching for specific type
services.AddHttpClient<WeatherApiClient>()
    .AddResilienceWithGenericMediumTermCache<WeatherResponse>(); // 10 minutes cache

// Or just caching without resilience
services.AddGenericHttpClientCaching<WeatherResponse>();

// Use in your service
public class WeatherService(CachedHttpClient<WeatherResponse> client)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await client.GetFromJsonAsync($"/weather?city={city}");
}
```

### 🌐 Universal Caching (Multi-Type) - For Flexible Scenarios

Perfect when you work with multiple response types:

```csharp
using Reliable.HttpClient.Caching;

// Register universal caching
services.AddHttpClientWithCache();

// Use in your service
public class ApiService(IHttpClientWithCache client)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await client.GetAsync<WeatherResponse>($"/weather?city={city}");

    public async Task<UserProfile> GetUserAsync(int id) =>
        await client.GetAsync<UserProfile>($"/users/{id}");
}
```

### 🚀 Preset-Based Configuration (Both Approaches)

Choose from ready-made presets for common scenarios:

```csharp
// Generic caching presets
services.AddHttpClient<StockPriceClient>()
    .AddShortTermCache<StockPrice>();               // 1 minute

services.AddHttpClient<NewsClient>()
    .AddMediumTermCache<NewsArticle>();             // 10 minutes

services.AddHttpClient<CountryClient>()
    .AddLongTermCache<Country>();                   // 1 hour

services.AddHttpClient<PopularApiClient>()
    .AddHighPerformanceCache<ApiResponse>();        // 5 minutes, large cache

services.AddHttpClient<ConfigClient>()
    .AddConfigurationCache<AppConfig>();            // 30 minutes
```

### Combined Resilience + Caching Presets

```csharp
// Resilience with preset caching (Generic approach)
services.AddHttpClient<ApiClient>()
    .AddResilienceWithShortTermCache<ApiResponse>(); // 1 minute

services.AddHttpClient<ApiClient>()
    .AddResilienceWithLongTermCache<ApiResponse>(); // 1 hour

// Custom resilience with preset caching
services.AddHttpClient<ApiClient>()
    .AddResilienceWithCaching<ApiResponse>(
        HttpClientPresets.SlowExternalApi(), // Resilience preset
        CachePresets.MediumTerm               // Cache preset
    );
```

### Legacy/Advanced Configuration

For existing code or when you need full control:

```csharp
// Manual registration (legacy approach)
services.AddMemoryCache();

// Configure HttpClient with resilience and caching
services.AddHttpClient<WeatherApiClient>()
    .AddResilience()  // Retry + Circuit breaker
    .AddMemoryCache<WeatherResponse>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromMinutes(5);
        options.CacheOnlySuccessfulResponses = true;
    });
```

## Using the Cached Client

```csharp
public class WeatherApiClient
{
    private readonly CachedHttpClient<WeatherResponse> _cachedClient;

    public WeatherApiClient(CachedHttpClient<WeatherResponse> cachedClient)
    {
        _cachedClient = cachedClient;
    }

    public async Task<WeatherResponse> GetWeatherAsync(string city)
    {
        // This call will be cached automatically
        var response = await _cachedClient.GetAsync($"/weather?city={city}");
        return response;
    }

    public async Task InvalidateCacheAsync(string city)
    {
        // Manually invalidate specific cache entries
        await _cachedClient.InvalidateAsync($"/weather?city={city}");
    }
}
```

## Cache Presets Reference

| Preset | Duration | Max Size | Best For | Example Use Cases |
|--------|----------|----------|----------|-------------------|
| `ShortTerm` | 1 minute | 500 | Real-time data | Stock prices, live scores, current weather |
| `MediumTerm` | 10 minutes | 1,000 | Regular updates | News feeds, social posts, search results |
| `LongTerm` | 1 hour | 2,000 | Stable data | Product catalogs, user profiles, reference lists |
| `HighPerformance` | 5 minutes | 5,000 | High-traffic | Popular API endpoints, trending content |
| `Configuration` | 30 minutes | 100 | App settings | Feature flags, configuration data |
| `FileDownload` | 2 hours | 50 | Large files | Documents, images, downloads |

### HttpCacheOptions

| Property                        | Type         | Default      | Description                                   |
|---------------------------------|--------------|--------------|-----------------------------------------------|
| `DefaultExpiry`                 | `TimeSpan`   | `5 minutes`  | Default cache expiration time                 |
| `MaxCacheSize`                  | `int?`       | `null`       | Maximum number of cached entries              |
| `CacheOnlySuccessfulResponses`  | `bool`       | `true`       | Only cache 2xx responses                      |
| `RespectCacheControlHeaders`    | `bool`       | `true`       | Honor HTTP Cache-Control headers              |
| `VaryByHeaders`                 | `string[]`   | `[]`         | Additional headers to include in cache key    |

### Advanced Configuration

```csharp
services.AddHttpClient<ApiClient>()
    .AddResilience()
    .AddMemoryCache<ApiResponse>(options =>
    {
        // Basic settings
        options.DefaultExpiry = TimeSpan.FromMinutes(10);
        options.MaxCacheSize = 1000;

        // Cache behavior
        options.CacheOnlySuccessfulResponses = true;
        options.RespectCacheControlHeaders = true;

        // Cache key generation
        options.VaryByHeaders = new[] { "Authorization", "Accept-Language" };

        // Custom cache key generator (optional)
        options.CacheKeyGenerator = new CustomCacheKeyGenerator();
    });
```

## Cache Key Generation

### Default Behavior

The default cache key generator creates secure, collision-resistant keys using SHA256 hashing:

```text
SHA256(HTTP_METHOD + URI + QUERY_PARAMS + VARY_HEADERS)
```

### Custom Cache Key Generator

```csharp
public class CustomCacheKeyGenerator : ICacheKeyGenerator
{
    public string GenerateKey(HttpRequestMessage request, string[] varyByHeaders)
    {
        var keyBuilder = new StringBuilder();

        // Include method and URI
        keyBuilder.Append(request.Method.Method);
        keyBuilder.Append(':');
        keyBuilder.Append(request.RequestUri?.ToString());

        // Include custom headers
        foreach (var header in varyByHeaders)
        {
            if (request.Headers.TryGetValues(header, out var values))
            {
                keyBuilder.Append(':');
                keyBuilder.Append(string.Join(",", values));
            }
        }

        // Return SHA256 hash for security
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
        return Convert.ToBase64String(hash);
    }
}

// Registration
services.AddSingleton<ICacheKeyGenerator, CustomCacheKeyGenerator>();
```

## Cache Providers

### Memory Cache Provider (Default)

Uses `IMemoryCache` for in-memory caching:

```csharp
services.AddMemoryCache();
services.AddHttpClient<ApiClient>()
    .AddMemoryCache<ApiResponse>();
```

### Custom Cache Provider

Implement `IHttpResponseCache<T>` for custom caching solutions:

```csharp
public class RedisCacheProvider<T> : IHttpResponseCache<T>
{
    private readonly IDatabase _database;

    public RedisCacheProvider(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
    }

    public async Task SetAsync(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task ClearAsync()
    {
        // Implementation depends on your Redis setup
        var server = _database.Multiplexer.GetServer("localhost:6379");
        await server.FlushDatabaseAsync();
    }
}

// Registration
services.AddSingleton<IConnectionMultiplexer>(provider =>
    ConnectionMultiplexer.Connect("localhost:6379"));
services.AddSingleton<IHttpResponseCache<ApiResponse>, RedisCacheProvider<ApiResponse>>();
```

## Cache Control Headers

### Respecting HTTP Headers

When `RespectCacheControlHeaders` is enabled (default), the caching system honors standard HTTP caching headers:

```csharp
// Server response headers
Cache-Control: max-age=300, public
Expires: Mon, 01 Jan 2024 12:00:00 GMT

// These will override DefaultExpiry setting
```

### Cache-Control Directives Supported

| Directive  | Behavior                        |
|------------|---------------------------------|
| `max-age`  | Sets cache expiration time      |
| `no-cache` | Forces cache validation         |
| `no-store` | Prevents caching entirely       |
| `public`   | Allows caching (default behavior) |
| `private`  | Prevents shared caching         |

## Manual Cache Management

### Cache Invalidation

```csharp
public class ProductService
{
    private readonly CachedHttpClient<Product> _cachedClient;

    public async Task<Product> GetProductAsync(int id)
    {
        return await _cachedClient.GetAsync($"/products/{id}");
    }

    public async Task UpdateProductAsync(int id, Product product)
    {
        // Update the product
        await _httpClient.PutAsJsonAsync($"/products/{id}", product);

        // Invalidate the cached entry
        await _cachedClient.InvalidateAsync($"/products/{id}");
    }
}
```

### Cache Clearing

```csharp
// Clear all cached entries
await _cachedClient.ClearCacheAsync();

// Clear entries matching a pattern (if supported by cache provider)
await _cachedClient.InvalidatePatternAsync("/products/*");
```

## Performance Considerations

### Memory Usage

```csharp
services.AddHttpClient<ApiClient>()
    .AddMemoryCache<ApiResponse>(options =>
    {
        // Limit cache size to prevent memory issues
        options.MaxCacheSize = 1000;

        // Shorter expiry for frequently changing data
        options.DefaultExpiry = TimeSpan.FromMinutes(2);
    });
```

### Cache Hit Monitoring

```csharp
public class MonitoredCacheProvider<T> : IHttpResponseCache<T>
{
    private readonly IHttpResponseCache<T> _innerCache;
    private readonly ILogger<MonitoredCacheProvider<T>> _logger;

    public async Task<T?> GetAsync(string key)
    {
        var result = await _innerCache.GetAsync(key);

        if (result is not null)
        {
            _logger.LogInformation("Cache hit for key: {Key}", key);
        }
        else
        {
            _logger.LogInformation("Cache miss for key: {Key}", key);
        }

        return result;
    }

    // ... other methods
}
```

## Testing with Caching

### Unit Testing

```csharp
[Test]
public async Task Should_Return_Cached_Response()
{
    var mockCache = new Mock<IHttpResponseCache<WeatherResponse>>();
    var cachedResponse = new WeatherResponse { Temperature = 25 };

    mockCache.Setup(c => c.GetAsync(It.IsAny<string>()))
           .ReturnsAsync(cachedResponse);

    var cachedClient = new CachedHttpClient<WeatherResponse>(
        httpClient, mockCache.Object, Options.Create(new HttpCacheOptions()));

    var result = await cachedClient.GetAsync("/weather?city=London");

    result.Should().Be(cachedResponse);
    mockCache.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
}
```

### Integration Testing

```csharp
[Test]
public async Task Should_Cache_Successful_Response()
{
    var services = new ServiceCollection();
    services.AddMemoryCache();
    services.AddHttpClient<TestClient>()
        .AddMemoryCache<TestResponse>(options =>
        {
            options.DefaultExpiry = TimeSpan.FromMinutes(1);
        });

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<CachedHttpClient<TestResponse>>();

    // First call hits the server
    var response1 = await client.GetAsync("/test");

    // Second call should be cached
    var response2 = await client.GetAsync("/test");

    response1.Should().BeEquivalentTo(response2);
}
```

## Best Practices

### 1. Choose Appropriate Cache Durations

```csharp
// Fast-changing data
services.AddHttpClient<StockPriceClient>()
    .AddMemoryCache<StockPrice>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromSeconds(30);
    });

// Slow-changing data
services.AddHttpClient<CountryClient>()
    .AddMemoryCache<Country>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromHours(24);
    });
```

### 2. Use Appropriate Vary Headers

```csharp
// Cache per user
services.AddHttpClient<UserProfileClient>()
    .AddMemoryCache<UserProfile>(options =>
    {
        options.VaryByHeaders = new[] { "Authorization" };
    });

// Cache per language
services.AddHttpClient<LocalizedContentClient>()
    .AddMemoryCache<Content>(options =>
    {
        options.VaryByHeaders = new[] { "Accept-Language" };
    });
```

### 3. Handle Cache Warming

```csharp
public class CacheWarmupService : IHostedService
{
    private readonly CachedHttpClient<ConfigResponse> _client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Pre-populate cache with frequently accessed data
        await _client.GetAsync("/config/global");
        await _client.GetAsync("/config/features");
    }
}
```

## Common Scenarios

### E-commerce Product Catalog

```csharp
services.AddHttpClient<ProductCatalogClient>()
    .AddResilience()
    .AddMemoryCache<Product>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromMinutes(15);
        options.MaxCacheSize = 10000;
        options.VaryByHeaders = new[] { "Accept-Language", "Currency" };
    });
```

### Configuration Service

```csharp
services.AddHttpClient<ConfigurationClient>()
    .AddResilience()
    .AddMemoryCache<ConfigurationResponse>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromHours(1);
        options.CacheOnlySuccessfulResponses = true;
    });
```

### Weather API

```csharp
services.AddHttpClient<WeatherClient>()
    .AddResilience()
    .AddMemoryCache<WeatherResponse>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromMinutes(10);
        options.RespectCacheControlHeaders = true;
    });
```

## Security Considerations

### Secure Cache Keys

The default cache key generator uses SHA256 hashing to prevent:

- Cache key collisions
- Cache poisoning attacks
- Information leakage through predictable keys

### Sensitive Data

```csharp
// Don't cache sensitive responses
services.AddHttpClient<AuthClient>()
    .AddMemoryCache<AuthResponse>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromMinutes(1); // Short expiry
        options.CacheOnlySuccessfulResponses = true;
        options.VaryByHeaders = new[] { "Authorization" }; // Isolate per user
    });
```

## Troubleshooting

### Common Issues

1. **Cache not working**: Ensure `AddMemoryCache()` is registered
2. **Excessive memory usage**: Set `MaxCacheSize` limit
3. **Stale data**: Check `DefaultExpiry` and cache control headers
4. **Cache misses**: Verify cache key generation with custom headers

### Debugging Cache Behavior

```csharp
// Enable detailed logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Monitor cache operations
services.Decorate<IHttpResponseCache<MyResponse>, LoggingCacheDecorator<MyResponse>>();
```
