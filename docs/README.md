# Reliable.HttpClient Documentation

> 📖 **Complete Documentation Hub** - Everything you need to get productive with Reliable.HttpClient

A comprehensive resilience and caching ecosystem for HttpClient with built-in retry policies, circuit breakers, and intelligent response caching.

## 🚀 Getting Started

**New to Reliable.HttpClient?** Start here:

1. **[Getting Started Guide](getting-started.md)** - Step-by-step setup and first implementation
2. **[Choosing Your Approach](choosing-approach.md)** - Which features and patterns fit your use case
3. **[Configuration Reference](configuration.md)** - Complete options and customization

## 📚 Documentation Structure

### Core Guides

- **[Getting Started](getting-started.md)** - Installation, basic setup, first examples
- **[Choosing Your Approach](choosing-approach.md)** - Decision guide for different scenarios
- **[Configuration Reference](configuration.md)** - Complete configuration options
- **[Advanced Usage](advanced-usage.md)** - Complex patterns and customization

### Specialized Topics

- **[HTTP Caching Guide](caching.md)** - Complete caching documentation and architecture
- **[Common Business Scenarios](examples/common-scenarios.md)** - Real-world implementation examples
- **[HttpClient Substitution](examples/http-client-substitution.md)** - Replacement patterns

### Reference Documents

- **[Configuration Examples](examples/configuration-examples.md)** - Ready-to-use configuration snippets

## 🎯 Quick Navigation

**I want to...**

| Goal | Go to |
|------|-------|
| **Get started quickly** | [Getting Started Guide](getting-started.md) |
| **Choose the right approach** | [Choosing Guide](choosing-approach.md) |
| **Add caching to my API** | [HTTP Caching Guide](caching.md) |
| **See real-world examples** | [Common Scenarios](examples/common-scenarios.md) |
| **Configure advanced options** | [Configuration Reference](configuration.md) |
| **Replace HttpClient** | [Substitution Guide](examples/http-client-substitution.md) |

## 💡 Key Concepts

### Core Resilience (Reliable.HttpClient)

Zero-configuration resilience patterns for HttpClient:

- **Retry Policies** - Exponential backoff with jitter
- **Circuit Breaker** - Automatic failure detection and recovery
- **Timeout Management** - Per-request timeout handling

### HTTP Caching (Reliable.HttpClient.Caching)

Intelligent response caching with two approaches:

- **Universal Caching** - Multi-type responses with custom handlers
- **Generic Caching** - Type-safe caching for specific response types

> 📖 **Complete Architecture**: See [HTTP Caching Guide](caching.md) for detailed architecture and decision guidance

### ✨ Universal Response Handlers

Eliminate "Generic Hell" for REST APIs with many entity types:

```csharp
// Before: Multiple registrations per entity type
services.AddSingleton<IHttpResponseHandler<User>, JsonResponseHandler<User>>();
services.AddSingleton<IHttpResponseHandler<Order>, JsonResponseHandler<Order>>();
// ... many more

// After: One registration for all entity types
services.AddHttpClientWithCache();

public class ApiClient(IHttpClientWithCache client)
{
    public async Task<User> GetUserAsync(int id) =>
        await client.GetAsync<User>($"/users/{id}");

    public async Task<Order> GetOrderAsync(int id) =>
        await client.GetAsync<Order>($"/orders/{id}");
    // Works with any entity type!
}
```

### 🔄 HttpClient Substitution Pattern

Seamlessly switch between cached and non-cached implementations:

```csharp
// Base client using adapter interface
public class ApiClient(IHttpClientAdapter client)
{
    public async Task<T> GetAsync<T>(string endpoint) =>
        await client.GetAsync<T>(endpoint);
}

// Cached version inherits everything, adds caching
public class CachedApiClient : ApiClient
{
    public CachedApiClient(IHttpClientWithCache client) : base(client) { }
    // Automatic caching + cache invalidation methods
}
```

## Package Overview

| Package | Purpose | Installation |
|---------|---------|-------------|
| **Reliable.HttpClient** | Core resilience (retry + circuit breaker) | `dotnet add package Reliable.HttpClient` |
| **Reliable.HttpClient.Caching** | HTTP response caching extension | `dotnet add package Reliable.HttpClient.Caching` |

## Quick Start Example

```csharp
// Add to your Program.cs
builder.Services.AddHttpClient<ApiClient>(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddResilience(); // That's it! ✨

// Use anywhere
public class ApiClient(HttpClient client)
{
    public async Task<Data> GetDataAsync() =>
        await client.GetFromJsonAsync<Data>("/endpoint");
}
```

**You now have:** Automatic retries + Circuit breaker + Smart error handling

> 🚀 **Need more details?** Continue with [Getting Started Guide](getting-started.md)

## Why Choose This Ecosystem?

✅ **Zero Configuration** - Works out of the box with sensible defaults
✅ **Complete Solution** - Resilience + Caching in one ecosystem
✅ **Lightweight** - Minimal overhead, maximum reliability
✅ **Production Ready** - Used by companies in production environments
✅ **Easy Integration** - One line of code to add resilience, two lines for caching
✅ **Secure** - SHA256-based cache keys prevent collisions and attacks
✅ **Flexible** - Use core resilience alone or add caching as needed

## Contributing

Contributions are welcome! Please read our [Contributing Guide](../CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

- 📖 [Documentation](README.md) – This documentation site
- 🐛 [Issues](https://github.com/akrisanov/Reliable.HttpClient/issues) – Bug reports and feature requests
- 💬 [Discussions](https://github.com/akrisanov/Reliable.HttpClient/discussions) – Community discussions

---

> **Note**: This documentation is for the latest version. Check the version badge in the main README for compatibility.
