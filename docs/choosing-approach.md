# Choosing the Right Approach

This guide helps you choose the best approach for your specific use case.

## 🏗️ Caching Architecture Overview

Reliable.HttpClient provides two distinct caching approaches:

### 🎯 Generic Caching (Type-Safe)
- **Best for**: Known response types, compile-time safety
- **Namespace**: `Reliable.HttpClient.Caching.Generic`
- **Key class**: `CachedHttpClient<TResponse>`

### 🌐 Universal Caching (Multi-Type)
- **Best for**: Multiple response types, flexible scenarios
- **Namespace**: `Reliable.HttpClient.Caching`
- **Key class**: `HttpClientWithCache`

## Quick Decision Tree

### Single Entity Type API → Use Generic Caching

If you work with one or few well-defined entity types and need type safety:

```csharp
// Recommended for type-safe caching
using Reliable.HttpClient.Caching.Generic;
using Reliable.HttpClient.Caching.Generic.Extensions;

services.AddHttpClient<WeatherApiClient>()
    .AddResilience()
    .AddGenericMemoryCache<WeatherResponse>();

public class WeatherApiClient(HttpClient client, CachedHttpClient<WeatherResponse> cachedClient)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await cachedClient.GetFromJsonAsync($"/weather?city={city}");
}
```

### Multi-Entity REST API → Use Universal Caching

If you work with many entity types (5+ types) from a REST API:

```csharp
// Recommended for REST APIs with many entity types
using Reliable.HttpClient.Caching;

services.AddHttpClientWithCache();

public class CrmApiClient(IHttpClientWithCache client)
{
    public async Task<Lead> GetLeadAsync(int id) =>
        await client.GetAsync<Lead>($"/api/leads/{id}");

    public async Task<Contact> GetContactAsync(int id) =>
        await client.GetAsync<Contact>($"/api/contacts/{id}");

    public async Task<Account> GetAccountAsync(int id) =>
        await client.GetAsync<Account>($"/api/accounts/{id}");
    // ... many more entity types
}
```

### High Performance/Custom Logic → Use Custom Handler

If you need custom deserialization, error handling, or performance optimization:

```csharp
// Recommended for custom requirements
public class CustomApiHandler : IHttpResponseHandler
{
    public async Task<T> HandleAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        // Custom logic: XML, protobuf, custom error handling, etc.
    }
}

services.AddSingleton<IHttpResponseHandler, CustomApiHandler>();
```

## Migration Path

### Phase 1: Current State

Keep using your existing approach – it's not deprecated.

### Phase 2: Gradual Migration (Optional)

Migrate complex multi-entity APIs to universal approach:

```csharp
// Before: 15+ registrations
services.AddSingleton<IHttpResponseHandler<Lead>, JsonResponseHandler<Lead>>();
services.AddSingleton<IHttpResponseHandler<Contact>, JsonResponseHandler<Contact>>();
// ... many more

// After: 1 registration
services.AddHttpClientWithCache();
```

### Phase 3: Consistency (Recommended)

Choose one primary approach per project:

- **Single approach per codebase** reduces confusion
- **Document your choice** in project README
- **Train team on chosen approach**

## Approach Comparison

| Scenario            | Traditional Generic    | Universal Handler      | Substitution Pattern   |
|---------------------|------------------------|------------------------|------------------------|
| **Single Entity**   | ✅ **Best**            | ❌ Overkill            | ❌ Overkill            |
| **2-4 Entities**    | ✅ **Good**            | ✅ Good                | ✅ Good                |
| **5+ Entities**     | ❌ Verbose             | ✅ **Best**            | ✅ **Best**            |
| **Custom Logic**    | ✅ **Best**            | ✅ Good                | ✅ **Best**            |
| **Performance**     | ✅ **Best**            | ✅ Good                | ✅ Good                |
| **Inheritance**     | ❌ Complex             | ❌ Limited             | ✅ **Best**            |
| **Testing**         | ✅ Good                | ✅ Good                | ✅ **Best** (Mockable) |
| **DI Complexity**   | ❌ High                | ✅ **Low**             | ✅ **Low**             |

## Detailed Comparison: Generic vs Universal Caching

### 🎯 Generic Caching (`CachedHttpClient<T>`)

**When to Use:**
- ✅ Well-defined response types (DTOs, POCOs)
- ✅ Compile-time type safety required
- ✅ High-performance scenarios
- ✅ APIs with consistent response schemas
- ✅ Single or few response types per client

**Example:**
```csharp
// Type-safe, optimized for WeatherResponse
public class WeatherService(CachedHttpClient<WeatherResponse> client)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await client.GetFromJsonAsync($"/weather?city={city}");
}
```

**Benefits:**
- 🚀 Compile-time type checking
- 🚀 Optimized serialization paths
- 🚀 Zero boxing/unboxing overhead
- 🚀 IntelliSense support

### 🌐 Universal Caching (`HttpClientWithCache`)

**When to Use:**
- ✅ Multiple different response types
- ✅ Dynamic or unknown response types
- ✅ Simple, flexible caching needs
- ✅ REST APIs with many endpoints
- ✅ Rapid prototyping

**Example:**
```csharp
// Flexible, handles any response type
public class ApiClient(IHttpClientWithCache client)
{
    public async Task<T> GetAsync<T>(string endpoint) where T : class =>
        await client.GetAsync<T>(endpoint);
}
```

**Benefits:**
- 🌐 Works with any response type
- 🌐 Single client for all endpoints
- 🌐 Minimal DI registration
- 🌐 Easy to use and understand

### Migration Between Approaches

**From Generic to Universal:**
```csharp
// Before: Generic caching
services.AddGenericHttpClientCaching<WeatherResponse>();
private readonly CachedHttpClient<WeatherResponse> _client;

// After: Universal caching
services.AddHttpClientWithCache();
private readonly IHttpClientWithCache _client;
```

**From Universal to Generic:**
```csharp
// Before: Universal caching
services.AddHttpClientWithCache();
public async Task<WeatherResponse> GetWeatherAsync() =>
    await _client.GetAsync<WeatherResponse>("/weather");

// After: Generic caching
services.AddGenericHttpClientCaching<WeatherResponse>();
public async Task<WeatherResponse> GetWeatherAsync() =>
    await _client.GetFromJsonAsync("/weather");
```

## Best Practices

### 1. Be Consistent Within Project

```csharp
// Don't mix approaches in same project without clear reason
public class ApiClient(
    HttpClient client,
    IHttpResponseHandler<Lead> leadHandler,  // Traditional
    IHttpResponseHandler universalHandler)   // Universal
```

### 2. Document Your Choice

```csharp
// Document in your API client
/// <summary>
/// CRM API client using universal response handlers for multi-entity support.
/// Chosen over traditional approach to reduce 15+ DI registrations to 1.
/// </summary>
public class CrmApiClient(IHttpClientWithCache client)
```

### 3. Team Alignment

- **Choose one approach** for new development
- **Document patterns** in team wiki
- **Code review** for consistency

## When NOT to Use Universal Approach

**Avoid universal approach if:**

- Working with 1-2 entity types only
- Need custom error handling per entity type
- Have existing working code (no migration pressure)
- Team unfamiliar with generic constraints

**Use universal approach if:**

- Building REST API clients with 5+ entity types
- Want to reduce DI complexity
- New project starting fresh
- Team comfortable with generics

**Use substitution pattern if:**

- Need inheritance-based architecture
- Want to switch between cached/non-cached at runtime
- Building testable components with mockable interfaces
- Have existing base classes to extend
