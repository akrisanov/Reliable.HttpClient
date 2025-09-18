# Reliable.HttpClient Documentation

Welcome to the comprehensive documentation for Reliable.HttpClient - a complete resilience and caching ecosystem for HttpClient.

## Package Overview

| Package | Purpose | Documentation |
|---------|---------|---------------|
| **Reliable.HttpClient** | Core resilience (retry + circuit breaker) | Core features documented below |
| **Reliable.HttpClient.Caching** | HTTP response caching extension | [Caching Guide](caching.md) |

## What's New in v1.1+

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

### ✨ Fluent Configuration API

Easy, strongly-typed configuration with validation:

```csharp
services.AddHttpClient<ApiClient>()
    .AddResilience(options =>
    {
        options.Timeout = TimeSpan.FromMinutes(1);
        options.Retry.MaxRetries = 5;
        options.Retry.BaseDelay = TimeSpan.FromSeconds(2);
        options.CircuitBreaker.FailureThreshold = 10;
        options.CircuitBreaker.OpenDuration = TimeSpan.FromMinutes(2);
    });
```

## Table of Contents

### Getting Started

- [Quick Start Guide](getting-started.md) - Get up and running in minutes
- [Choosing the Right Approach](choosing-approach.md) - **NEW!** Which pattern to use when
- [Installation & Setup](getting-started.md#installation) - Package installation and basic configuration
- [First Steps](getting-started.md#basic-setup) - Your first resilient HttpClient

### Architecture Patterns

- [Universal Response Handlers](examples/common-scenarios.md#universal-rest-api-client) - **NEW!** For REST APIs with many entity types
- [HttpClient Substitution](examples/http-client-substitution.md) - **NEW!** Inheritance-friendly patterns
- [Traditional Generic Approach](getting-started.md) - Maximum type safety and control

### Configuration

- [Configuration Reference](configuration.md) - Complete options reference including new Builder API
- [Configuration Examples](examples/configuration-examples.md) - Real-world configuration patterns
- [Retry Policies](configuration.md#retry-configuration) - Configuring retry behavior
- [Circuit Breakers](configuration.md#circuit-breaker-configuration) - Preventing cascading failures
- [Validation Rules](configuration.md#validation) - Understanding configuration validation

### Caching

- [HTTP Response Caching](caching.md) - Complete caching guide
- [Cache Providers](caching.md#cache-providers) - Memory and custom cache providers
- [Cache Configuration](caching.md#configuration-options) - Caching options and settings
- [Security & Best Practices](caching.md#security-considerations) - Secure caching patterns

### Usage Patterns

- [Advanced Usage](advanced-usage.md) - Advanced patterns and techniques
- [Multiple Clients](advanced-usage.md#multiple-named-httpclients) - Managing different service configurations
- [Typed Clients](advanced-usage.md#typed-httpclients) - Using with typed HttpClient pattern
- [Custom Error Handling](advanced-usage.md#custom-error-handling) - Implementing custom retry logic
- [Testing Strategies](advanced-usage.md#testing-with-resilience) - Unit and integration testing approaches

### Examples

- [Common Scenarios](examples/common-scenarios.md) - Real-world usage examples
- [Configuration Examples](examples/configuration-examples.md) - Various configuration patterns
- [E-commerce Integration](examples/common-scenarios.md#scenario-1-e-commerce-api-integration) - Payment and inventory APIs
- [Microservices Communication](examples/common-scenarios.md#scenario-2-microservices-communication) - Service-to-service patterns
- [External APIs](examples/common-scenarios.md#scenario-3-external-api-with-rate-limiting) - Handling rate limits and quotas
- [Legacy Systems](examples/common-scenarios.md#scenario-4-legacy-system-integration) - Working with unreliable systems

### Reference

- [Default Values](configuration.md#overview) - All default configuration values
- [Error Codes](configuration.md#validation) - Understanding validation errors

### Contributing

- [Contributing Guide](../CONTRIBUTING.md) - How to contribute to the project
- [Development Setup](../CONTRIBUTING.md#getting-started) - Setting up the development environment
- [Code Style](../CONTRIBUTING.md#code-style) - Coding standards and conventions
- [Testing Guidelines](../CONTRIBUTING.md#writing-tests) - How to write effective tests

## Quick Reference

### Minimal Setup (Core Resilience)

```csharp
builder.Services.AddHttpClient("api")
    .AddResilience(); // Zero configuration required!
```

### Custom Configuration

```csharp
services.AddHttpClient<ApiClient>()
    .AddResilience(options =>
    {
        options.Timeout = TimeSpan.FromMinutes(2);
        options.Retry.MaxRetries = 4;
        options.Retry.BaseDelay = TimeSpan.FromSeconds(2);
        options.CircuitBreaker.FailureThreshold = 8;
    });
```

### Resilience + Caching (New!)

```csharp
// One-line setup with presets
services.AddHttpClient<ApiClient>()
    .AddResilienceWithMediumTermCache<ApiResponse>(); // 10 minutes cache

// Or choose specific presets
services.AddHttpClient<ConfigClient>()
    .AddResilienceWithLongTermCache<ConfigResponse>(); // 1 hour cache
```

## Key Concepts

### Retry Policies

Automatically retry failed requests with exponential backoff and jitter to handle transient failures gracefully.

### Circuit Breakers

Prevent cascading failures by temporarily stopping requests to failing services and allowing them time to recover.

### HTTP Response Caching

Intelligent caching of HTTP responses with configurable expiration, cache providers, and security features.

### Configuration Validation

All settings are validated at startup to catch configuration errors early and provide clear error messages.

### Zero Configuration

Works out of the box with production-ready defaults, but allows customization when needed.

## Support

- 📖 [Documentation](README.md) – This documentation site
- 🐛 [Issues](https://github.com/akrisanov/Reliable.HttpClient/issues) – Bug reports and feature requests
- 💬 [Discussions](https://github.com/akrisanov/Reliable.HttpClient/discussions) – Community discussions

---

> **Note**: This documentation is for the latest version. Check the version badge in the main README for compatibility.
