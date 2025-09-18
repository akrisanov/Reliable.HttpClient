# Advanced Usage Patterns

Learn advanced techniques and patterns for using Reliable.HttpClient in complex scenarios.

## Header Management

Reliable.HttpClient provides comprehensive header management capabilities for various authentication and API integration scenarios.

### Basic Header Operations

```csharp
services.AddHttpClient("api")
    .AddResilience(builder => builder
        // Add single header
        .WithHeader("Authorization", "Bearer your-token")
        .WithHeader("X-API-Key", "your-api-key")

        // Add multiple headers at once
        .WithHeaders(new Dictionary<string, string>
        {
            { "User-Agent", "MyApp/1.0" },
            { "Accept", "application/json" },
            { "X-Client-Version", "2.1.0" }
        })

        // Remove unwanted headers
        .WithoutHeader("X-Debug")

        // Clear all headers (useful for testing)
        .WithoutHeaders());
```

### OAuth Token Management

For APIs requiring OAuth tokens, you can set authorization headers:

```csharp
// Static token (for service-to-service)
services.AddHttpClient("auth-api")
    .AddResilience(builder => builder
        .WithBaseUrl("https://api.partner.com")
        .WithHeader("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")
        .WithHeader("X-Client-ID", "your-client-id"));

// Dynamic token refresh pattern
public class TokenRefreshService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue("access_token", out string token))
            return token;

        // Refresh token logic
        var response = await _httpClient.PostAsync("/oauth/token", content);
        var tokenData = await response.Content.ReadFromJsonAsync<TokenResponse>();

        _cache.Set("access_token", tokenData.AccessToken, TimeSpan.FromMinutes(50));
        return tokenData.AccessToken;
    }
}

// Usage with dynamic headers (use IHttpClientAdapter for runtime header support)
public class ApiService
{
    private readonly IHttpClientAdapter _client;
    private readonly TokenRefreshService _tokenService;

    public async Task<ApiResponse> GetDataAsync()
    {
        var token = await _tokenService.GetAccessTokenAsync();
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {token}" }
        };

        return await _client.GetAsync<ApiResponse>("/api/data", headers);
    }
}
```

### API Key Patterns

```csharp
// Header-based API key
services.AddHttpClient("header-auth-api")
    .AddResilience(builder => builder
        .WithHeader("X-API-Key", "your-api-key")
        .WithHeader("X-API-Secret", "your-api-secret"));

// Multiple API keys for different environments
public static class ApiConfiguration
{
    public static IHttpClientBuilder AddApiClient(
        this IServiceCollection services,
        string environment)
    {
        var config = GetApiConfig(environment);

        return services.AddHttpClient("api-client")
            .AddResilience(builder => builder
                .WithBaseUrl(config.BaseUrl)
                .WithHeader("X-API-Key", config.ApiKey)
                .WithHeader("X-Environment", environment));
    }

    private static ApiConfig GetApiConfig(string env) => env switch
    {
        "production" => new("https://api.prod.com", "prod-key"),
        "staging" => new("https://api.staging.com", "staging-key"),
        _ => new("https://api.dev.com", "dev-key")
    };
}
```

### Custom Authentication Headers

```csharp
// HMAC signature authentication
public class HmacAuthenticationService
{
    public Dictionary<string, string> CreateAuthHeaders(string method, string uri, string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N")[..16];
        var signature = CreateHmacSignature(method, uri, body, timestamp, nonce);

        return new Dictionary<string, string>
        {
            { "X-Timestamp", timestamp },
            { "X-Nonce", nonce },
            { "X-Signature", signature },
            { "Authorization", "HMAC-SHA256" }
        };
    }
}

// Usage with runtime headers
public async Task<T> SecurePostAsync<T>(string uri, object data)
{
    var json = JsonSerializer.Serialize(data);
    var headers = _hmacService.CreateAuthHeaders("POST", uri, json);

    return await _httpClient.PostAsync<object, T>(uri, data, headers);
}
```

### Multi-tenant Header Management

```csharp
public class TenantAwareApiClient
{
    private readonly IHttpClientAdapter _client;
    private readonly ITenantContext _tenantContext;

    public async Task<T> GetAsync<T>(string endpoint)
    {
        var headers = new Dictionary<string, string>
        {
            { "X-Tenant-ID", _tenantContext.TenantId },
            { "X-User-ID", _tenantContext.UserId },
            { "Authorization", $"Bearer {_tenantContext.AccessToken}" }
        };

        return await _client.GetAsync<T>(endpoint, headers);
    }
}

// Registration
services.AddScoped<ITenantContext, TenantContext>();
services.AddHttpClient()
    .AddHttpClientAdapter()
    .AddResilience();
services.AddScoped<TenantAwareApiClient>();
```

### Conditional Headers

```csharp
public class ConditionalHeadersClient
{
    private readonly IHttpClientAdapter _client;
    private readonly IConfiguration _config;

    public async Task<T> GetAsync<T>(string endpoint, bool includeDebugHeaders = false)
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {GetToken()}" }
        };

        // Add debug headers only in development
        if (includeDebugHeaders && _config.GetValue<bool>("IncludeDebugHeaders"))
        {
            headers.Add("X-Debug-Mode", "true");
            headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
            headers.Add("X-Request-Source", Environment.MachineName);
        }

        // Add versioning headers
        var apiVersion = _config.GetValue<string>("ApiVersion");
        if (!string.IsNullOrEmpty(apiVersion))
        {
            headers.Add("API-Version", apiVersion);
        }

        return await _client.GetAsync<T>(endpoint, headers);
    }
}
```

### Header Validation and Sanitization

```csharp
public static class HeaderValidator
{
    private static readonly HashSet<string> ForbiddenHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Content-Length", "Transfer-Encoding", "Connection"
    };

    public static Dictionary<string, string> ValidateAndSanitize(
        IDictionary<string, string> headers)
    {
        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in headers)
        {
            // Skip forbidden headers
            if (ForbiddenHeaders.Contains(key))
                continue;

            // Sanitize header values
            var cleanValue = value?.Trim();
            if (string.IsNullOrEmpty(cleanValue))
                continue;

            // Remove dangerous characters
            cleanValue = cleanValue.Replace("\r", "").Replace("\n", "");

            sanitized[key] = cleanValue;
        }

        return sanitized;
    }
}

// Usage in service
public async Task<T> SafeGetAsync<T>(string endpoint, IDictionary<string, string> userHeaders)
{
    var sanitizedHeaders = HeaderValidator.ValidateAndSanitize(userHeaders);
    return await _client.GetAsync<T>(endpoint, sanitizedHeaders);
}
```

## Multiple Named HttpClients

Configure different resilience policies for different services using presets and custom configuration:

```csharp
// Fast internal service - use preset
services.AddHttpClient("internal-api", c => c.BaseAddress = new Uri("http://internal-api"))
    .AddResilience(HttpClientPresets.FastInternalApi());

// External service - use preset with customization
services.AddHttpClient("external-api", c => c.BaseAddress = new Uri("https://external-api.com"))
    .AddResilience(HttpClientPresets.SlowExternalApi(), options =>
    {
        // Customize the preset
        options.Retry.MaxRetries = 3; // Override preset's 2 retries
    });

// File downloads - specific preset
services.AddHttpClient("file-downloads")
    .AddResilience(HttpClientPresets.FileDownload());

// Custom configuration with builder pattern
services.AddHttpClient("custom-api")
    .AddResilience(builder => builder
        .WithTimeout(TimeSpan.FromSeconds(45))
        .WithRetry(retry => retry
            .WithMaxRetries(4)
            .WithBaseDelay(TimeSpan.FromMilliseconds(800))
            .WithJitter(0.4))
        .WithCircuitBreaker(cb => cb
            .WithFailureThreshold(7)
            .WithOpenDuration(TimeSpan.FromMinutes(3))));
```

## Configuration Patterns by Scenario

### High-Performance Internal APIs

```csharp
services.AddHttpClient<OrderService>()
    .AddResilience(HttpClientPresets.FastInternalApi())
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 100,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10)
    });
```

### External Partner APIs

```csharp
services.AddHttpClient<PartnerApiClient>()
    .AddResilience(builder => builder
        .WithTimeout(TimeSpan.FromMinutes(2))
        .WithRetry(retry => retry
            .WithMaxRetries(3)
            .WithBaseDelay(TimeSpan.FromSeconds(3))
            .WithJitter(0.5)) // Higher jitter for external APIs
        .WithCircuitBreaker(cb => cb
            .WithFailureThreshold(5)
            .WithOpenDuration(TimeSpan.FromMinutes(10)))); // Longer recovery time
```

### Real-time Data APIs

```csharp
services.AddHttpClient<RealTimeDataClient>()
    .AddResilience(HttpClientPresets.RealTimeApi())
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("X-Real-Time", "true");
    });
```

### File Upload/Download Services

```csharp
// Download client
services.AddHttpClient<FileDownloadClient>()
    .AddResilience(HttpClientPresets.FileDownload());

// Upload client (custom configuration)
services.AddHttpClient<FileUploadClient>()
    .AddResilience(builder => builder
        .WithTimeout(TimeSpan.FromMinutes(15))
        .WithRetry(retry => retry
            .WithMaxRetries(2)
            .WithBaseDelay(TimeSpan.FromSeconds(5)))
        .WithoutCircuitBreaker()); // No circuit breaker for uploads
```

## Typed HttpClients

Use with typed HttpClient pattern for better encapsulation:

```csharp
public class WeatherApiClient
{
    private readonly HttpClient _httpClient;

    public WeatherApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherData> GetWeatherAsync(string city)
    {
        var response = await _httpClient.GetAsync($"/weather?city={city}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WeatherData>();
    }
}

// Registration
services.AddHttpClient<WeatherApiClient>(c =>
{
    c.BaseAddress = new Uri("https://api.weather.com");
    c.DefaultRequestHeaders.Add("Api-Key", "your-key");
})
.AddResilience();
```

## Custom Error Handling

Override which errors should trigger retries:

```csharp
public class CustomHttpResponseHandler : HttpResponseHandlerBase
{
    protected override bool ShouldRetry(HttpResponseMessage response)
    {
        // Retry on server errors and rate limiting
        if (response.StatusCode >= HttpStatusCode.InternalServerError)
            return true;

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return true;

        // Don't retry on 4xx client errors (except 408, 429)
        if (response.StatusCode >= HttpStatusCode.BadRequest &&
            response.StatusCode < HttpStatusCode.InternalServerError)
            return false;

        return false;
    }
}

// Registration
services.AddSingleton<IHttpResponseHandler, CustomHttpResponseHandler>();
services.AddHttpClient("api").AddResilience();
```

## Conditional Resilience

Apply resilience based on environment or configuration:

```csharp
public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddConditionalResilience(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        var resilienceEnabled = configuration.GetValue<bool>("Features:ResilienceEnabled", true);

        if (resilienceEnabled)
        {
            return builder.AddResilience();
        }

        return builder;
    }
}

// Usage
services.AddHttpClient("api")
    .AddConditionalResilience(configuration);
```

## Monitoring and Observability

### Logging Circuit Breaker Events

```csharp
public class ObservableCircuitBreakerOptions : CircuitBreakerOptions
{
    public Action<string> OnCircuitOpened { get; set; }
    public Action<string> OnCircuitClosed { get; set; }
    public Action<string> OnCircuitHalfOpened { get; set; }
}

// In your configuration
.AddResilience(options =>
{
    options.CircuitBreaker.OnCircuitOpened = (name) =>
        logger.LogWarning("Circuit breaker {Name} opened", name);
    options.CircuitBreaker.OnCircuitClosed = (name) =>
        logger.LogInformation("Circuit breaker {Name} closed", name);
});
```

### Custom Metrics

```csharp
public class MetricsHttpResponseHandler : HttpResponseHandlerBase
{
    private readonly IMetrics _metrics;

    public MetricsHttpResponseHandler(IMetrics metrics)
    {
        _metrics = metrics;
    }

    protected override bool ShouldRetry(HttpResponseMessage response)
    {
        _metrics.Counter("http_requests_total")
            .WithTag("status_code", ((int)response.StatusCode).ToString())
            .Increment();

        return base.ShouldRetry(response);
    }
}
```

## Performance Optimization

### Reusing HttpClient Instances

```csharp
// ✅ Good - reuse HttpClient instances
services.AddHttpClient("shared-api")
    .AddResilience()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        MaxConnectionsPerServer = 100,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10)
    });

// ❌ Bad - creating new HttpClient instances
public class BadService
{
    public async Task<string> GetDataAsync()
    {
        using var client = new HttpClient(); // Don't do this!
        return await client.GetStringAsync("https://api.example.com/data");
    }
}
```

### Connection Pooling

```csharp
services.AddHttpClient("optimized-api")
    .AddResilience()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 50,
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
        EnableMultipleHttp2Connections = true
    });
```

## Testing with Resilience

### Unit Testing

```csharp
[Test]
public async Task Should_Retry_On_Server_Error()
{
    var mockHandler = new Mock<HttpMessageHandler>();

    // First call fails, second succeeds
    mockHandler.SetupSequence(h => h.SendAsync(
            It.IsAny<HttpRequestMessage>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError))
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("success")
        });

    var httpClient = new HttpClient(mockHandler.Object);
    var service = new MyService(httpClient);

    var result = await service.GetDataAsync();

    result.Should().Be("success");
    mockHandler.Verify(h => h.SendAsync(
        It.IsAny<HttpRequestMessage>(),
        It.IsAny<CancellationToken>()), Times.Exactly(2));
}
```

### Integration Testing

```csharp
[Test]
public async Task Should_Handle_Real_Network_Failures()
{
    var services = new ServiceCollection();
    services.AddHttpClient<TestApiClient>(c =>
    {
        c.BaseAddress = new Uri("https://httpstat.us");
        c.Timeout = TimeSpan.FromSeconds(2);
    })
    .AddResilience(options =>
    {
        options.Retry.MaxRetries = 2;
        options.Retry.BaseDelay = TimeSpan.FromMilliseconds(100);
    });

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<TestApiClient>();

    // This will retry on 500 errors
    var response = await client.GetAsync("/500");

    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
}
```

## Common Patterns

### Graceful Degradation

```csharp
public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public async Task<WeatherData> GetWeatherAsync(string city)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/weather?city={city}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<WeatherData>();
                _cache.Set($"weather_{city}", data, TimeSpan.FromMinutes(30));
                return data;
            }
        }
        catch (HttpRequestException)
        {
            // Fallback to cache if available
            if (_cache.TryGetValue($"weather_{city}", out WeatherData cachedData))
            {
                return cachedData;
            }
        }

        // Return default/fallback data
        return new WeatherData { City = city, Temperature = "Unknown" };
    }
}
```

### Bulkhead Pattern

Isolate different types of operations using presets:

```csharp
// Separate clients for different operation types
services.AddHttpClient("read-operations")
    .AddResilience(HttpClientPresets.FastInternalApi()); // Aggressive for reads

services.AddHttpClient("write-operations")
    .AddResilience(builder => builder
        .WithRetry(retry => retry.WithMaxRetries(1)) // Conservative for writes
        .WithCircuitBreaker(cb => cb.WithFailureThreshold(3)));

services.AddHttpClient("auth-operations")
    .AddResilience(HttpClientPresets.AuthenticationApi());
```

## Advanced Builder Patterns

### Conditional Configuration

```csharp
public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddEnvironmentBasedResilience(
        this IHttpClientBuilder builder,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Fast-fail in development
            return builder.AddResilience(b => b
                .WithRetry(retry => retry.WithMaxRetries(1))
                .WithCircuitBreaker(cb => cb.WithFailureThreshold(2)));
        }

        if (environment.IsProduction())
        {
            // Use production preset
            return builder.AddResilience(HttpClientPresets.SlowExternalApi());
        }

        // Default for staging
        return builder.AddResilience();
    }
}

// Usage
services.AddHttpClient<MyApiClient>()
    .AddEnvironmentBasedResilience(environment);
```

### Configuration Composition

```csharp
public static class ResiliencePresets
{
    public static HttpClientOptions CreateCustomPreset()
    {
        return new HttpClientOptionsBuilder()
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithRetry(retry => retry
                .WithMaxRetries(3)
                .WithBaseDelay(TimeSpan.FromSeconds(1))
                .WithJitter(0.25))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(5)
                .WithOpenDuration(TimeSpan.FromMinutes(1)))
            .Build();
    }

    public static HttpClientOptions ModifyPreset(HttpClientOptions basePreset, Action<HttpClientOptionsBuilder> modify)
    {
        var builder = new HttpClientOptionsBuilder()
            .WithTimeout(TimeSpan.FromSeconds(basePreset.TimeoutSeconds))
            .WithRetry(retry => retry
                .WithMaxRetries(basePreset.Retry.MaxRetries)
                .WithBaseDelay(basePreset.Retry.BaseDelay)
                .WithJitter(basePreset.Retry.JitterFactor))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(basePreset.CircuitBreaker.FailuresBeforeOpen)
                .WithOpenDuration(basePreset.CircuitBreaker.OpenDuration));

        modify(builder);
        return builder.Build();
    }
}

// Usage
var customPreset = ResiliencePresets.ModifyPreset(
    HttpClientPresets.SlowExternalApi(),
    builder => builder.WithTimeout(TimeSpan.FromMinutes(5)));

services.AddHttpClient("long-running-api")
    .AddResilience(customPreset);
```
