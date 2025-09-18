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
        // Universal HTTP client with caching
        services.AddHttpClientWithCache(options =>
        {
            options.DefaultExpiry = TimeSpan.FromMinutes(10);
        });

        // Configure the HttpClient
        services.AddHttpClient(c =>
        {
            c.BaseAddress = new Uri("https://api.crm.com");
            c.DefaultRequestHeaders.Add("Authorization", "Bearer token");
        })
        .AddResilience(HttpClientPresets.SlowExternalApi());
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

## Product Catalog Service

**Business Context**: High-performance e-commerce catalog serving thousands of product requests per second.

**Caching Strategy**: Type-safe generic caching for known product types with optimized serialization.

```csharp
using Reliable.HttpClient.Caching.Generic;
using Reliable.HttpClient.Caching.Generic.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Product API - high performance with type-safe caching
        services.AddHttpClient<ProductApiClient>()
            .AddResilience(HttpClientPresets.FastInternalApi())
            .AddGenericMemoryCache<Product>(options =>
            {
                options.DefaultExpiry = TimeSpan.FromMinutes(15);
                options.MaxCacheSize = 10000; // Large cache for popular products
            });

        // Category API - longer cache for stable data
        services.AddGenericHttpClientCaching<Category>(options =>
        {
            options.DefaultExpiry = TimeSpan.FromHours(2);
        });

        // Inventory API - short cache for dynamic data
        services.AddGenericHttpClientCaching<InventoryLevel>(options =>
        {
            options.DefaultExpiry = TimeSpan.FromMinutes(1);
        });
    }
}

// Type-safe product service
public class ProductService
{
    private readonly CachedHttpClient<Product> _productClient;
    private readonly CachedHttpClient<Category> _categoryClient;
    private readonly CachedHttpClient<InventoryLevel> _inventoryClient;

    public ProductService(
        CachedHttpClient<Product> productClient,
        CachedHttpClient<Category> categoryClient,
        CachedHttpClient<InventoryLevel> inventoryClient)
    {
        _productClient = productClient;
        _categoryClient = categoryClient;
        _inventoryClient = inventoryClient;
    }

    public async Task<ProductDetails> GetProductDetailsAsync(int productId)
    {
        // All requests are cached with optimal serialization
        var product = await _productClient.GetFromJsonAsync($"/products/{productId}");
        var category = await _categoryClient.GetFromJsonAsync($"/categories/{product.CategoryId}");
        var inventory = await _inventoryClient.GetFromJsonAsync($"/inventory/{productId}");

        return new ProductDetails(product, category, inventory);
    }
}

// Well-defined DTOs for optimal caching
public record Product(int Id, string Name, decimal Price, int CategoryId, string Description);
public record Category(int Id, string Name, string Description);
public record InventoryLevel(int ProductId, int Available, int Reserved);
public record ProductDetails(Product Product, Category Category, InventoryLevel Inventory);
```

**Key Benefits**:

- **Type Safety**: Compile-time checking for all cached responses
- **Performance**: Optimized serialization without boxing/unboxing
- **Memory Efficiency**: Type-specific cache storage
- **Scalability**: Handles high-traffic scenarios efficiently

---

## Configuration Service

**Business Context**: Centralized configuration service providing application settings with fallback support.

**Caching Strategy**: Universal caching for flexible configuration types with long cache durations.

```csharp
using Reliable.HttpClient.Caching;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Configuration API - universal caching for multiple config types
        services.AddHttpClientWithCache(options =>
        {
            options.DefaultExpiry = TimeSpan.FromMinutes(30); // Long cache for stable config
        });

        services.AddSingleton<IConfigurationService, ConfigurationService>();
    }
}

// Universal configuration service
public class ConfigurationService : IConfigurationService
{
    private readonly IHttpClientWithCache _client;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(IHttpClientWithCache client, ILogger<ConfigurationService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<T> GetConfigAsync<T>(string key, T fallback = default) where T : class
    {
        try
        {
            return await _client.GetAsync<T>($"/config/{key}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get config {Key}, using fallback", key);
            return fallback ?? throw new ConfigurationException($"No fallback for {key}");
        }
    }

    public async Task<bool> UpdateConfigAsync<T>(string key, T value) where T : class
    {
        try
        {
            await _client.PostAsync<T, object>($"/config/{key}", value);

            // Invalidate cache after update
            await _client.InvalidateCacheAsync($"*{key}*");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update config {Key}", key);
            return false;
        }
    }
}

// Usage in business services
public class EmailService
{
    private readonly IConfigurationService _config;

    public EmailService(IConfigurationService config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Configuration cached for 30 minutes
        var smtpConfig = await _config.GetConfigAsync<SmtpConfig>("smtp", new SmtpConfig
        {
            Host = "localhost",
            Port = 587,
            EnableSsl = false
        });

        // Use configuration...
    }
}

public record SmtpConfig(string Host, int Port, bool EnableSsl, string Username, string Password);
public record DatabaseConfig(string ConnectionString, int CommandTimeout, int MaxRetries);
public record FeatureFlags(bool EnableNewUi, bool EnableBetaFeatures, string ApiVersion);
```

**Key Benefits**:

- **Flexibility**: Works with any configuration type
- **Fallback Support**: Graceful degradation when config service is unavailable
- **Cache Invalidation**: Automatic cache refresh on configuration updates
- **Simple Integration**: Easy to use across all application services

---

## OAuth API Client

**Business Context**: API client that needs to handle OAuth authentication with token refresh and per-request authorization.

**Challenge**: Different requests need different tokens (user tokens, service tokens) and request-specific headers for tracing.

```csharp
public interface IOAuthTokenProvider
{
    Task<string> GetUserTokenAsync(string userId);
    Task<string> GetServiceTokenAsync();
    Task<string> RefreshTokenAsync(string refreshToken);
}

public class UserApiClient
{
    private readonly IHttpClientAdapter _client;
    private readonly IOAuthTokenProvider _tokenProvider;
    private readonly ILogger<UserApiClient> _logger;

    public UserApiClient(IHttpClientAdapter client, IOAuthTokenProvider tokenProvider, ILogger<UserApiClient> logger)
    {
        _client = client;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<UserProfile> GetUserProfileAsync(string userId, string? requestId = null)
    {
        var token = await _tokenProvider.GetUserTokenAsync(userId);
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {token}" },
            { "X-Request-Id", requestId ?? Guid.NewGuid().ToString() },
            { "X-User-Context", userId }
        };

        _logger.LogInformation("Fetching user profile for {UserId}", userId);
        return await _client.GetAsync<UserProfile>($"/users/{userId}/profile", headers);
    }

    public async Task<UserProfile> UpdateUserProfileAsync(string userId, UpdateProfileRequest request)
    {
        var token = await _tokenProvider.GetUserTokenAsync(userId);
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {token}" },
            { "X-Request-Id", Guid.NewGuid().ToString() },
            { "X-User-Context", userId },
            { "X-Action", "profile-update" }
        };

        return await _client.PutAsync<UpdateProfileRequest, UserProfile>($"/users/{userId}/profile", request, headers);
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string query, int page = 1, int pageSize = 20)
    {
        // Service-to-service call with service token
        var serviceToken = await _tokenProvider.GetServiceTokenAsync();
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {serviceToken}" },
            { "X-Request-Id", Guid.NewGuid().ToString() },
            { "X-Page", page.ToString() },
            { "X-Page-Size", pageSize.ToString() }
        };

        var response = await _client.GetAsync<SearchResponse<User>>($"/users/search?q={query}&page={page}&size={pageSize}", headers);
        return response.Results;
    }
}

// Registration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOAuthTokenProvider, OAuthTokenProvider>();

        services.AddHttpClient<UserApiClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.users.com");
        })
        .AddResilience(builder => builder
            .WithHeader("Accept", "application/json")
            .WithHeader("X-Client-Id", "my-service")
            .WithUserAgent("MyService/1.0")
            .WithRetry(retry => retry
                .WithMaxRetries(3)
                .WithBaseDelay(TimeSpan.FromSeconds(1)))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(5)
                .WithOpenDuration(TimeSpan.FromMinutes(2))));

        services.AddScoped<IHttpClientAdapter, HttpClientAdapter>();
    }
}
```

**Key Benefits**:
- Dynamic token handling per request
- Request tracing with unique IDs
- User context preservation
- Service vs user token separation

---

## Multi-Tenant API Client

**Business Context**: SaaS application serving multiple tenants with tenant-specific configurations and headers.

```csharp
public class TenantApiClient
{
    private readonly IHttpClientAdapter _client;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantApiClient> _logger;

    public TenantApiClient(IHttpClientAdapter client, ITenantContext tenantContext, ILogger<TenantApiClient> logger)
    {
        _client = client;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TenantData> GetTenantDataAsync(string dataId)
    {
        var tenant = _tenantContext.CurrentTenant;
        var headers = CreateTenantHeaders(tenant);

        return await _client.GetAsync<TenantData>($"/data/{dataId}", headers);
    }

    public async Task<TenantSettings> UpdateTenantSettingsAsync(TenantSettings settings)
    {
        var tenant = _tenantContext.CurrentTenant;
        var headers = CreateTenantHeaders(tenant);
        headers["X-Action"] = "settings-update";
        headers["X-Version"] = settings.Version.ToString();

        return await _client.PutAsync<TenantSettings, TenantSettings>($"/tenants/{tenant.Id}/settings", settings, headers);
    }

    private Dictionary<string, string> CreateTenantHeaders(Tenant tenant)
    {
        return new Dictionary<string, string>
        {
            { "X-Tenant-Id", tenant.Id.ToString() },
            { "X-Tenant-Tier", tenant.Tier.ToString() },
            { "X-Region", tenant.Region },
            { "X-Request-Id", Guid.NewGuid().ToString() },
            { "Authorization", $"Bearer {tenant.ApiKey}" }
        };
    }
}
```

---

## API Versioning Client

**Business Context**: Client that needs to support multiple API versions with version-specific headers and content negotiation.

```csharp
public class VersionedApiClient
{
    private readonly IHttpClientAdapter _client;
    private readonly ILogger<VersionedApiClient> _logger;

    public VersionedApiClient(IHttpClientAdapter client, ILogger<VersionedApiClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ProductV1> GetProductV1Async(int productId)
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/vnd.api.v1+json" },
            { "X-API-Version", "1.0" }
        };

        return await _client.GetAsync<ProductV1>($"/products/{productId}", headers);
    }

    public async Task<ProductV2> GetProductV2Async(int productId)
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/vnd.api.v2+json" },
            { "X-API-Version", "2.0" },
            { "X-Features", "enhanced-metadata,pricing-tiers" }
        };

        return await _client.GetAsync<ProductV2>($"/products/{productId}", headers);
    }

    public async Task<ProductV3> GetProductV3Async(int productId, bool includeRecommendations = false)
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/vnd.api.v3+json" },
            { "X-API-Version", "3.0" }
        };

        if (includeRecommendations)
        {
            headers["X-Include"] = "recommendations,related-products";
        }

        return await _client.GetAsync<ProductV3>($"/products/{productId}", headers);
    }

    // Generic version for testing new API versions
    public async Task<T> GetWithVersionAsync<T>(string endpoint, string version, Dictionary<string, string>? additionalHeaders = null)
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", $"application/vnd.api.v{version}+json" },
            { "X-API-Version", version },
            { "X-Client", "VersionedClient/1.0" }
        };

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                headers[header.Key] = header.Value;
            }
        }

        return await _client.GetAsync<T>(endpoint, headers);
    }
}
```

---

## Summary

Each business scenario requires different resilience and caching strategies:

| Scenario                | Primary Concern      | Recommended Preset              | Caching Approach            |
|-------------------------|----------------------|---------------------------------|-----------------------------|
| **E-commerce Payments** | Zero downtime        | `SlowExternalApi()`             | No caching (critical data)  |
| **Microservices**       | Service isolation    | `FastInternalApi()`             | Vary by service criticality |
| **External APIs**       | Rate limit handling  | `SlowExternalApi()`             | Longer cache (reduce calls) |
| **Legacy Systems**      | Maximum patience     | Custom builder                  | Aggressive caching          |
| **Product Catalog**     | Performance          | `FastInternalApi()` + Generic   | Type-safe, optimized cache  |
| **Configuration**       | System stability     | `FastInternalApi()` + Universal | Flexible, fallback support |
| **Universal REST API**  | Maintainability      | Universal handlers              | Single registration pattern |
| **OAuth API Client**    | Dynamic auth         | Custom with `IHttpClientAdapter` | Token-aware per request    |
| **Multi-Tenant**        | Tenant isolation     | Headers per tenant              | Tenant-specific caching     |
| **API Versioning**      | Version flexibility  | Version-specific headers        | Version-aware cache keys    |

> 💡 **Next Steps**: See [Configuration Examples](configuration-examples.md) for detailed configuration patterns and techniques.
