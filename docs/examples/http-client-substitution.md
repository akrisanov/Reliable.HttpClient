# HttpClient.Substitution Example

This example demonstrates how to use `IHttpClientAdapter` to create substitutable HTTP clients that can switch between
regular and cached implementations.

## Problem Solved

As requested by @dterenin-the-dev in [issue #1](https://github.com/akrisanov/Reliable.HttpClient/issues/1#issuecomment-3303748111),
this implementation allows seamless substitution between regular HttpClient and HttpClientWithCache through inheritance.

## Implementation

### Base CRM Client

```csharp
public class CrmClient : ICrmClient
{
    private readonly IHttpClientAdapter _httpClient;
    private readonly IOptions<CrmClientOptions> _options;
    private readonly ILogger<CrmClient> _logger;

    public CrmClient(
        IHttpClientAdapter httpClient,
        IOptions<CrmClientOptions> options,
        ILogger<CrmClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Lead>> AddLeadsAsync(
        SyncLeadsContext ctx,
        CancellationToken cancellationToken = default)
    {
        ValidateContext(ctx);

        Uri requestUri = BuildUri(ctx);
        var request = BuildRequest(ctx);

        // This works with both regular HttpClient and HttpClientWithCache
        LeadsResponse response = await _httpClient.PostAsync<SyncLeadsRequest, LeadsResponse>(
            requestUri.ToString(),
            request,
            cancellationToken);

        // Process response...
        return response.Leads.AsReadOnly();
    }

    private void ValidateContext(SyncLeadsContext ctx) { /* validation logic */ }
    private Uri BuildUri(SyncLeadsContext ctx) => new(_options.Value.BaseUrl + "/leads");
    private SyncLeadsRequest BuildRequest(SyncLeadsContext ctx) => new() { /* build request */ };
}
```

### Cached CRM Client (Through Inheritance)

```csharp
public class CachedCrmClient : CrmClient
{
    private readonly IHttpClientWithCache _cachedHttpClient;

    public CachedCrmClient(
        IHttpClientWithCache httpClient,  // This implements IHttpClientAdapter too!
        IOptions<CrmClientOptions> options,
        ILogger<CachedCrmClient> logger)
        : base(httpClient, options, logger)  // Pass to base as IHttpClientAdapter
    {
        _cachedHttpClient = httpClient;
    }

    public override async Task<IReadOnlyCollection<Lead>> AddLeadsAsync(
        SyncLeadsContext ctx,
        CancellationToken cancellationToken = default)
    {
        var addedLeads = await base.AddLeadsAsync(ctx, cancellationToken);

        // Clear cache after mutation
        await _cachedHttpClient.InvalidateCacheAsync("/leads");

        return addedLeads;
    }

    // Add caching-specific methods
    public async Task<Lead?> GetLeadFromCacheAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _cachedHttpClient.GetAsync<Lead>(
            $"/leads/{id}",
            TimeSpan.FromMinutes(10),
            cancellationToken);
    }
}
```

## Dependency Injection Setup

### For Regular (Non-Cached) Client

```csharp
services.AddHttpClientWithAdapter();
services.AddScoped<ICrmClient, CrmClient>();
```

### For Cached Client

```csharp
services.AddHttpClientWithCache();
services.AddScoped<ICrmClient, CachedCrmClient>();
```

## Key Benefits

1. **Seamless Substitution**: `HttpClientWithCache` implements both `IHttpClientWithCache` and `IHttpClientAdapter`
2. **Inheritance-Friendly**: Base class uses `IHttpClientAdapter`, derived class can access caching features
3. **No Code Duplication**: Shared logic in base class, caching-specific logic in derived class
4. **Easy Testing**: Mock `IHttpClientAdapter` for unit tests
5. **Configuration-Based**: Switch between implementations via DI registration

## Usage Patterns

### Pattern 1: Simple Substitution

```csharp
// Same constructor signature, different DI registration
services.AddScoped<ICrmClient>(provider =>
    useCache
        ? new CachedCrmClient(provider.GetService<IHttpClientWithCache>(), options, logger)
        : new CrmClient(provider.GetService<IHttpClientAdapter>(), options, logger));
```

### Pattern 2: Feature Flags

```csharp
services.AddScoped<ICrmClient>(provider =>
{
    var featureFlags = provider.GetService<IFeatureFlags>();
    return featureFlags.IsEnabled("UseCachedCrmClient")
        ? provider.GetService<CachedCrmClient>()
        : provider.GetService<CrmClient>();
});
```

This approach eliminates the boilerplate code duplication mentioned in the RFC while providing clean inheritance
patterns for cached vs non-cached implementations.
