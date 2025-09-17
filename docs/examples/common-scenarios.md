# Common Business Scenarios

Real-world business examples showing when and how to use Reliable.HttpClient for different domains and use cases.

## Quick Reference

### Business Domain Examples

- [E-commerce Platform](#e-commerce-platform) - Payment, inventory, and recommendation APIs
- [Microservices Architecture](#microservices-architecture) - Service-to-service communication
- [External API Integration](#external-api-integration) - Third-party APIs with rate limits
- [Legacy System Integration](#legacy-system-integration) - Unreliable legacy systems
- [Product Catalog Service](#product-catalog-service) - High-performance catalog with caching
- [Configuration Service](#configuration-service) - Centralized config with fallback

> 💡 **Configuration Details**: For technical configuration patterns, see [Configuration Examples](configuration-examples.md)

---

## E-commerce Platform

**Business Context**: Online store with payment processing, inventory management, and personalized recommendations.

**Resilience Strategy**: Different reliability requirements for different business functions.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Payment API - mission critical, use aggressive resilience
        services.AddHttpClient<PaymentApiClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.payments.com");
            c.DefaultRequestHeaders.Add("Authorization", "Bearer token");
        })
        .AddResilience(HttpClientPresets.SlowExternalApi(), options =>
        {
            // Customize for payments - even more aggressive
            options.Retry.MaxRetries = 5;
            options.CircuitBreaker.FailuresBeforeOpen = 3;
            options.CircuitBreaker.OpenDuration = TimeSpan.FromMinutes(5);
        });

        // Inventory API - important but less critical
        services.AddHttpClient<InventoryApiClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.inventory.com");
        })
        .AddResilience(HttpClientPresets.FastInternalApi());

        // Recommendation API - optional feature, minimal resilience
        services.AddHttpClient<RecommendationApiClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.recommendations.com");
        })
        .AddResilience(HttpClientPresets.RealTimeApi());
    }
}
```

**Key Insights**:

- **Payment API**: Maximum resilience - business can't afford payment failures
- **Inventory API**: Balanced approach - important but not mission critical
- **Recommendation API**: Fast-fail - won't block checkout if unavailable

## Microservices Architecture

**Business Context**: Order processing system communicating with user, notification, and analytics services.

**Resilience Strategy**: Service criticality determines resilience level.

```csharp
public class ServiceConfiguration
{
    public static void ConfigureHttpClients(IServiceCollection services, IConfiguration config)
    {
        // User service - authentication critical
        services.AddHttpClient("user-service", c =>
        {
            c.BaseAddress = new Uri(config["Services:UserService:BaseUrl"]);
        })
        .AddResilience(HttpClientPresets.FastInternalApi());

        // Notification service - important but can fail gracefully
        services.AddHttpClient("notification-service", c =>
        {
            c.BaseAddress = new Uri(config["Services:NotificationService:BaseUrl"]);
        })
        .AddResilience(builder => builder
            .WithRetry(retry => retry.WithMaxRetries(2))
            .WithCircuitBreaker(cb => cb.WithFailureThreshold(8)));

        // Analytics service - fire-and-forget
        services.AddHttpClient("analytics-service", c =>
        {
            c.BaseAddress = new Uri(config["Services:AnalyticsService:BaseUrl"]);
        })
        .AddResilience(HttpClientPresets.RealTimeApi());
    }
}
```

**Key Insights**:

- **User Service**: Must work - authentication blocks everything
- **Notification Service**: Should work - but order can complete without it
- **Analytics Service**: Nice to have - fire-and-forget pattern

## External API Integration

**Business Context**: Integrating with third-party APIs that have rate limits and varying reliability.

**Resilience Strategy**: Handle rate limits gracefully with longer delays and more tolerance.

```csharp
// Rate-limited external API configuration
services.AddHttpClient<ExternalApiClient>(c =>
{
    c.BaseAddress = new Uri("https://api.external.com");
    c.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
})
.AddResilience(HttpClientPresets.SlowExternalApi(), options =>
{
    // Customize for rate limits
    options.Retry.BaseDelay = TimeSpan.FromSeconds(5); // Longer delays
    options.CircuitBreaker.FailuresBeforeOpen = 8; // More tolerant
    options.CircuitBreaker.OpenDuration = TimeSpan.FromMinutes(10);
});
```

**Key Insights**:

- **Rate Limits**: Longer delays prevent hitting rate limits repeatedly
- **Circuit Breaker**: More tolerant threshold accounts for rate limit responses
- **Recovery Time**: Longer circuit breaker duration allows rate limits to reset

## Legacy System Integration

**Business Context**: Working with old, unreliable internal systems that can't be easily replaced.

**Resilience Strategy**: Maximum patience with graceful degradation using cache fallback.

```csharp
services.AddHttpClient<LegacySystemClient>(c =>
{
    c.BaseAddress = new Uri("http://legacy-system.internal");
})
.AddResilience(builder => builder
    .WithTimeout(TimeSpan.FromSeconds(45)) // Legacy systems are slow
    .WithRetry(retry => retry
        .WithMaxRetries(6) // More retries for flaky system
        .WithBaseDelay(TimeSpan.FromSeconds(3)))
    .WithCircuitBreaker(cb => cb
        .WithFailureThreshold(15) // Very tolerant
        .WithOpenDuration(TimeSpan.FromMinutes(15))));
```

**Key Insights**:

- **Long Timeouts**: Legacy systems need time to respond
- **Many Retries**: Flaky systems need multiple attempts
- **High Tolerance**: Circuit breaker rarely opens
- **Cache Fallback**: Always have backup data ready

## Product Catalog Service

**Business Context**: E-commerce product catalog with high traffic and performance requirements.

**Caching Strategy**: Different cache durations based on data volatility.

```csharp
services.AddMemoryCache();

// Individual products - longer cache (products don't change often)
services.AddHttpClient<ProductCatalogService>("products")
    .AddResilience(HttpClientPresets.FastInternalApi())
    .AddMemoryCache<Product>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromHours(1);
        options.VaryByHeaders = new[] { "Accept-Language", "Currency" };
        options.MaxCacheSize = 5000;
    });

// Product lists - shorter cache (inventory changes more frequently)
services.AddHttpClient<ProductCatalogService>("catalog")
    .AddResilience(HttpClientPresets.FastInternalApi())
    .AddMemoryCache<ProductList>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromMinutes(10);
        options.VaryByHeaders = new[] { "Accept-Language" };
    });
```

**Key Insights**:

- **Product Data**: Cache longer - product details rarely change
- **Product Lists**: Cache shorter - inventory levels change frequently
- **Localization**: Cache varies by language and currency
- **Cache Invalidation**: Manual invalidation for immediate updates

## Configuration Service

**Business Context**: Centralized configuration service with fallback for system resilience.

**Resilience Strategy**: Cache successful responses as emergency fallback data.

```csharp
services.AddMemoryCache();
services.AddHttpClient<ConfigurationService>()
    .AddResilience(HttpClientPresets.FastInternalApi())
    .AddMemoryCache<AppConfig>(options =>
    {
        options.DefaultExpiry = TimeSpan.FromMinutes(30);
        options.RespectCacheControlHeaders = true;
    });
```

**Key Insights**:

- **Configuration**: Cache for 30 minutes with HTTP header respect
- **Fallback Strategy**: Store successful responses for emergency use
- **Auto-refresh**: Respect Cache-Control headers from server
- **Multiple Environments**: Different cache keys per environment

---

## Universal REST API Client

**Business Context**: CRM system that handles multiple entity types
(Leads, Contacts, Companies, Orders, Products, etc.) through a REST API.

**Challenge**: Traditional approach requires separate handler registrations for each entity type,
leading to "Generic Hell" with 15+ DI registrations.

**Solution**: Use universal response handlers to eliminate boilerplate and simplify architecture.

### Before: Generic Hell

```csharp
// Traditional approach - lots of registrations
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Need separate handler for each entity type
        services.AddSingleton<IHttpResponseHandler<Lead>, JsonResponseHandler<Lead>>();
        services.AddSingleton<IHttpResponseHandler<Contact>, JsonResponseHandler<Contact>>();
        services.AddSingleton<IHttpResponseHandler<Company>, JsonResponseHandler<Company>>();
        services.AddSingleton<IHttpResponseHandler<Order>, JsonResponseHandler<Order>>();
        services.AddSingleton<IHttpResponseHandler<Product>, JsonResponseHandler<Product>>();
        services.AddSingleton<IHttpResponseHandler<User>, JsonResponseHandler<User>>();
        services.AddSingleton<IHttpResponseHandler<Invoice>, JsonResponseHandler<Invoice>>();
        services.AddSingleton<IHttpResponseHandler<Campaign>, JsonResponseHandler<Campaign>>();
        services.AddSingleton<IHttpResponseHandler<Deal>, JsonResponseHandler<Deal>>();
        services.AddSingleton<IHttpResponseHandler<Task>, JsonResponseHandler<Task>>();
        // ... 15+ registrations total

        services.AddHttpClient<CrmApiClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.crm.com");
            c.DefaultRequestHeaders.Add("Authorization", "Bearer token");
        })
        .AddResilience(HttpClientPresets.SlowExternalApi());
    }
}

// Overloaded constructor with many dependencies
public class CrmApiClient
{
    public CrmApiClient(
        HttpClient httpClient,
        IHttpResponseHandler<Lead> leadHandler,
        IHttpResponseHandler<Contact> contactHandler,
        IHttpResponseHandler<Company> companyHandler,
        IHttpResponseHandler<Order> orderHandler,
        IHttpResponseHandler<Product> productHandler,
        IHttpResponseHandler<User> userHandler,
        IHttpResponseHandler<Invoice> invoiceHandler,
        IHttpResponseHandler<Campaign> campaignHandler,
        IHttpResponseHandler<Deal> dealHandler,
        IHttpResponseHandler<Task> taskHandler,
        // ... more handlers
        ILogger<CrmApiClient> logger)
    {
        // Constructor becomes unmanageable
    }
}
```

### After: Universal Response Handlers

```csharp
// Clean approach - single registration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Universal HTTP client with caching and resilience
        services.AddResilientHttpClientWithCache(
            "crm-api",
            HttpClientPresets.SlowExternalApi(),
            defaultCacheDuration: TimeSpan.FromMinutes(10));

        // Configure the named client
        services.AddHttpClient("crm-api", c =>
        {
            c.BaseAddress = new Uri("https://api.crm.com");
            c.DefaultRequestHeaders.Add("Authorization", "Bearer token");
        });

        // Register API client
        services.AddScoped<ICrmApiClient, CrmApiClient>();
    }
}

// Clean constructor with minimal dependencies
public interface ICrmApiClient
{
    Task<Lead> GetLeadAsync(int id);
    Task<Contact> GetContactAsync(int id);
    Task<Company> GetCompanyAsync(int id);
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task<Product> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteLeadAsync(int id);
    Task ClearLeadCacheAsync();
}

public class CrmApiClient : ICrmApiClient
{
    private readonly IHttpClientWithCache _httpClient;
    private readonly ILogger<CrmApiClient> _logger;

    public CrmApiClient(IHttpClientWithCache httpClient, ILogger<CrmApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Clean, elegant methods for any entity type
    public async Task<Lead> GetLeadAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<Lead>(
                $"/api/leads/{id}",
                TimeSpan.FromMinutes(5)); // Cached for 5 minutes
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get lead {LeadId}", id);
            throw;
        }
    }

    public async Task<Contact> GetContactAsync(int id) =>
        await _httpClient.GetAsync<Contact>($"/api/contacts/{id}", TimeSpan.FromMinutes(5));

    public async Task<Company> GetCompanyAsync(int id) =>
        await _httpClient.GetAsync<Company>($"/api/companies/{id}", TimeSpan.FromMinutes(10));

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // POST requests automatically invalidate related cache entries
        return await _httpClient.PostAsync<CreateOrderRequest, Order>("/api/orders", request);
    }

    public async Task<Product> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        // PUT requests also invalidate cache
        return await _httpClient.PutAsync<UpdateProductRequest, Product>($"/api/products/{id}", request);
    }

    public async Task<bool> DeleteLeadAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync<ApiResponse>($"/api/leads/{id}");
            return response.Success;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return true; // Already deleted
        }
    }

    public async Task ClearLeadCacheAsync()
    {
        await _httpClient.InvalidateCacheAsync("/api/leads");
    }
}
```

### Entity Models

```csharp
// Standard DTOs - no special attributes needed
public record Lead(int Id, string Name, string Email, string Company, string Status);
public record Contact(int Id, string FirstName, string LastName, string Email, string Phone);
public record Company(int Id, string Name, string Website, string Industry);
public record Order(int Id, int CustomerId, decimal Amount, DateTime OrderDate, string Status);
public record Product(int Id, string Name, decimal Price, string Category, bool InStock);

public record CreateOrderRequest(int CustomerId, OrderItem[] Items);
public record UpdateProductRequest(string Name, decimal Price, bool InStock);
public record ApiResponse(bool Success, string Message);
```

### Usage in Business Logic

```csharp
public class LeadService
{
    private readonly ICrmApiClient _crmClient;

    public LeadService(ICrmApiClient crmClient)
    {
        _crmClient = crmClient;
    }

    public async Task<LeadSummary> GetLeadSummaryAsync(int leadId)
    {
        // All these calls benefit from caching and resilience automatically
        var lead = await _crmClient.GetLeadAsync(leadId);
        var company = await _crmClient.GetCompanyAsync(lead.CompanyId);
        var orders = await _crmClient.GetLeadOrdersAsync(leadId);

        return new LeadSummary(lead, company, orders);
    }

    public async Task<Order> ConvertLeadToOrderAsync(int leadId, CreateOrderRequest request)
    {
        // This will automatically invalidate lead cache
        var order = await _crmClient.CreateOrderAsync(request);

        // Clear lead cache since conversion changes lead status
        await _crmClient.ClearLeadCacheAsync();

        return order;
    }
}
```

**Key Benefits**:

- **Reduced Lines of Code**: From 1000+ to ~300 lines (-70%)
- **Fewer DI Registrations**: From 15+ to 1 registration (-93%)
- **Simpler Constructor**: From 7+ dependencies to 2 dependencies (-70%)
- **Easier Testing**: From 15+ mocks to 1-2 mocks (-80%)
- **Automatic Caching**: GET requests cached, mutations invalidate cache
- **Universal Pattern**: Works with any REST API and any entity types

**Key Insights**:

- **Scalability**: Pattern works for any number of entity types
- **Cache Strategy**: GET operations cached, POST/PUT/DELETE operations invalidate
- **Error Handling**: Centralized error handling with specific business logic
- **Testing**: Much simpler unit testing with fewer mocks
- **Migration**: 100% backward compatible with existing `IHttpResponseHandler<T>`

---

## Summary

Each business scenario requires different resilience and caching strategies:

| Scenario                | Primary Concern      | Recommended Preset              | Key Customization           |
|-------------------------|----------------------|---------------------------------|-----------------------------|
| **E-commerce Payments** | Zero downtime        | `SlowExternalApi()`             | Higher retry count          |
| **Microservices**       | Service isolation    | `FastInternalApi()`             | Vary by criticality         |
| **External APIs**       | Rate limit handling  | `SlowExternalApi()`             | Longer delays               |
| **Legacy Systems**      | Maximum patience     | Custom builder                  | Very high tolerance         |
| **Product Catalog**     | Performance          | `FastInternalApi()` + Caching   | Tiered cache strategy       |
| **Configuration**       | System stability     | `FastInternalApi()` + Caching   | Fallback data               |
| **Universal REST API**  | Maintainability      | Universal handlers              | Single registration pattern |

> 💡 **Next Steps**: See [Configuration Examples](configuration-examples.md) for detailed configuration patterns and techniques.
