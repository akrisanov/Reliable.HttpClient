# Getting Started Guide

Detailed setup guide for Reliable.HttpClient with step-by-step explanations.

> 🚀 **Quick Start**: For fastest setup, see [README Quick Start](../README.md#quick-start)

## Installation

Choose the packages you need:

### Core Resilience Only

```bash
dotnet add package Reliable.HttpClient
```

**Includes**: Retry policies, circuit breaker, smart error handling

### Core + Caching

```bash
dotnet add package Reliable.HttpClient
dotnet add package Reliable.HttpClient.Caching
```

**Includes**: Everything above + HTTP response caching

## Step-by-Step Setup

### Step 1: Basic Resilience

```csharp
// Minimal setup - works out of the box
builder.Services.AddHttpClient<MyApiClient>(c =>
{
    c.BaseAddress = new Uri("https://api.example.com");
})
.AddResilience(); // Zero configuration needed!
```

**What this gives you:**

- Automatic retries (3 attempts with exponential backoff)
- Circuit breaker (opens after 5 failures)
- Smart error handling (5xx, timeouts, rate limits)

### Step 2: Choose Your Architecture Pattern

Different patterns work better for different scenarios:

#### Option A: Traditional Generic (Best for 1-2 entity types)

```csharp
// Add memory cache service first
services.AddMemoryCache();

// Then add caching to your HttpClient
services.AddHttpClient<MyApiClient>(c =>
{
    c.BaseAddress = new Uri("https://api.example.com");
})
.AddResilience()
.AddMemoryCache<MyResponse>(options =>
{
    options.DefaultExpiry = TimeSpan.FromMinutes(5);
});
```

#### Option B: Universal Handlers (Best for 5+ entity types)

```csharp
// Add universal cached client
services.AddHttpClientWithCache(options =>
{
    options.DefaultExpiry = TimeSpan.FromMinutes(5);
});

// Use with any entity type
public class ApiClient(IHttpClientWithCache client)
{
    public async Task<User> GetUserAsync(int id) =>
        await client.GetAsync<User>($"/users/{id}");

    public async Task<Order> GetOrderAsync(int id) =>
        await client.GetAsync<Order>($"/orders/{id}");
    // ... many more entity types without additional registrations
}
```

#### Option C: Substitution Pattern (Best for inheritance scenarios)

```csharp
// Base client using adapter interface
public class BaseApiClient(IHttpClientAdapter client)
{
    protected readonly IHttpClientAdapter Client = client;

    public virtual async Task<T> GetAsync<T>(string endpoint) =>
        await Client.GetAsync<T>(endpoint);
}

// Cached version through inheritance
public class CachedApiClient : BaseApiClient
{
    private readonly IHttpClientWithCache _cachedClient;

    public CachedApiClient(IHttpClientWithCache client) : base(client)
    {
        _cachedClient = client;
    }

    // Override with caching-specific functionality
    public override async Task<T> GetAsync<T>(string endpoint) =>
        await _cachedClient.GetAsync<T>(endpoint, TimeSpan.FromMinutes(5));
}
```

> 📖 **Need help choosing?** See our [Choosing Guide](choosing-approach.md) for detailed comparison

**Key benefits of each approach:**

- **Traditional**: Maximum type safety and control per entity
- **Universal**: Minimal registration overhead, works with any type
- **Substitution**: Clean inheritance patterns, easy testing with mocks

### Step 3: Custom Configuration (Optional)

Need different settings? Multiple options available:

```csharp
// Option 1: Simple overrides
.AddResilience(options => options.Retry.MaxRetries = 5)

// Option 2: Fluent builder
.AddResilience(builder => builder.WithRetry(r => r.WithMaxRetries(5)))

// Option 3: Ready-made presets
.AddResilience(HttpClientPresets.SlowExternalApi())
```

> 📖 **Complete Configuration Guide**: See [Configuration Reference](configuration.md) for all options and presets

## Using Your Client

## Example: Complete Service

```csharp
public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient<WeatherApiClient>();
    }

    public async Task<WeatherData> GetWeatherAsync(string city)
    {
        // This call now has retry, circuit breaker, and caching!
        var response = await _httpClient.GetAsync($"/weather?city={city}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WeatherData>();
    }
}
```

**That's it!** Your HTTP client is now production-ready with resilience and caching.

## What's Included

- **3 retry attempts** with exponential backoff + jitter
- **Circuit breaker** opens after 5 failures for 1 minute
- **Smart error handling** for 5xx, timeouts, rate limits
- **Response caching** with 5-minute default expiry (if enabled)
- **Configuration validation** at startup
- **Zero overhead** when not needed

## Next Steps

- **Custom Configuration**: See [Configuration Reference](configuration.md)
- **Advanced Patterns**: See [Advanced Usage](advanced-usage.md)
- **Caching Details**: See [HTTP Caching Guide](caching.md)
- **Real Examples**: See [Common Scenarios](examples/common-scenarios.md)
