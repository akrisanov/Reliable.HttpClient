# Generic HTTP Caching

Type-safe HTTP caching for specific response types with compile-time safety and optimized performance.

## Quick Start

```csharp
using Reliable.HttpClient.Caching.Generic;
using Reliable.HttpClient.Caching.Generic.Extensions;

// Register
services.AddGenericHttpClientCaching<WeatherResponse>();

// Use
public class WeatherService(CachedHttpClient<WeatherResponse> client)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await client.GetFromJsonAsync($"/weather?city={city}");
}
```

## When to Use Generic Caching

✅ **Perfect for:**

- Well-defined response types (DTOs, POCOs)
- Compile-time type safety required
- High-performance scenarios
- APIs with consistent response schemas

❌ **Consider Universal Caching instead when:**

- Multiple different response types in single client
- Dynamic or unknown response types
- Simple, flexible caching needs

> 🎯 **Need help choosing?** See our [Choosing Guide](../../docs/choosing-approach.md)

## Documentation

📖 **[Complete Caching Guide](../../docs/caching.md)** - Architecture, examples, and advanced usage
🎯 **[Choosing the Right Approach](../../docs/choosing-approach.md)** - Decision guide
📝 **[Common Scenarios](../../docs/examples/common-scenarios.md)** - Real-world examples
