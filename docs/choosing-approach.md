# Choosing the Right Approach

This guide helps you choose the best approach for your specific use case.

## Quick Decision Tree

### Single Entity Type API → Use Traditional Approach

If you work with one or few entity types and need maximum control:

```csharp
// Recommended for single/few entity types
services.AddHttpClient<WeatherApiClient>()
    .AddResilience()
    .AddMemoryCache<WeatherResponse>();

public class WeatherApiClient(HttpClient client, CachedHttpClient<WeatherResponse> cachedClient)
{
    public async Task<WeatherResponse> GetWeatherAsync(string city) =>
        await cachedClient.GetAsync($"/weather?city={city}");
}
```

### Multi-Entity REST API → Use Universal Approach

If you work with many entity types (5+ types) from a REST API:

```csharp
// Recommended for REST APIs with many entity types
services.AddResilientHttpClientWithCache("crm-api", HttpClientPresets.SlowExternalApi());

public class CrmApiClient(IHttpClientWithCache client)
{
    public async Task<Lead> GetLeadAsync(int id) =>
        await client.GetAsync<Lead>($"/api/leads/{id}");

    public async Task<Contact> GetContactAsync(int id) =>
        await client.GetAsync<Contact>($"/api/contacts/{id}");
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
services.AddResilientHttpClientWithCache("crm-api");
```

### Phase 3: Consistency (Recommended)

Choose one primary approach per project:

- **Single approach per codebase** reduces confusion
- **Document your choice** in project README
- **Train team on chosen approach**

## Approach Comparison

| Scenario            | Traditional Generic    | Universal Handler      | Cached Client          |
|---------------------|------------------------|------------------------|------------------------|
| **Single Entity**   | ✅ **Best**            | ❌ Overkill            | ❌ Overkill            |
| **2-4 Entities**    | ✅ **Good**            | ✅ Good                | ✅ Good                |
| **5+ Entities**     | ❌ Verbose             | ✅ **Best**            | ✅ **Best**            |
| **Custom Logic**    | ✅ **Best**            | ✅ Good                | ❌ Limited             |
| **Performance**     | ✅ **Best**            | ✅ Good                | ✅ Good                |
| **Caching Needed**  | ✅ Built-in            | ➕ Manual              | ✅ **Built-in**        |
| **DI Complexity**   | ❌ High                | ✅ **Low**             | ✅ **Low**             |

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
