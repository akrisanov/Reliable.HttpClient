# Configuration Examples

Real-world examples showing different ways to configure Reliable.HttpClient for various scenarios.

## Quick Setup Examples

### 1. Zero Configuration (Recommended for Most Cases)

```csharp
// Perfect for 80% of scenarios
services.AddHttpClient<MyApiClient>(c =>
{
    c.BaseAddress = new Uri("https://api.example.com");
})
.AddResilience(); // Uses sensible defaults
```

### 2. Using Presets for Common Scenarios

```csharp
// External third-party API
services.AddHttpClient<PaymentApiClient>()
    .AddResilience(HttpClientPresets.SlowExternalApi());

// Internal microservice
services.AddHttpClient<UserServiceClient>()
    .AddResilience(HttpClientPresets.FastInternalApi());

// File downloads
services.AddHttpClient<FileDownloadClient>()
    .AddResilience(HttpClientPresets.FileDownload());
```

### 3. Fluent Builder for Custom Needs

```csharp
// When you need specific configuration
services.AddHttpClient<CustomApiClient>()
    .AddResilience(builder => builder
        .WithTimeout(TimeSpan.FromMinutes(2))
        .WithRetry(retry => retry
            .WithMaxRetries(4)
            .WithBaseDelay(TimeSpan.FromSeconds(2))
            .WithJitter(0.3))
        .WithCircuitBreaker(cb => cb
            .WithFailureThreshold(8)
            .WithOpenDuration(TimeSpan.FromMinutes(5))));
```

### 4. Caching with Custom Headers

```csharp
// Configure default headers and caching
services.AddHttpClient<ApiClient>()
    .AddResilience()
    .AddMemoryCache(options => options
        .WithDefaultExpiry(TimeSpan.FromMinutes(10))
        .AddHeader("User-Agent", "MyApp/1.0")
        .AddHeader("Accept", "application/json")
        .AddHeader("API-Version", "v2"));

// Per-request header customization
public class ApiService(IHttpClientWithCache client)
{
    public async Task<UserData> GetUserDataAsync(int userId, string userToken)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {userToken}",
            ["X-Request-ID"] = Guid.NewGuid().ToString()
        };

        return await client.GetAsync<UserData>($"/users/{userId}", headers);
    }
}
```

## Real-World Scenario Examples

### E-commerce Platform

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Payment gateway - critical, high retry
    services.AddHttpClient<PaymentGatewayClient>()
        .AddResilience(builder => builder
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithRetry(retry => retry
                .WithMaxRetries(5)
                .WithBaseDelay(TimeSpan.FromSeconds(1)))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(3)
                .WithOpenDuration(TimeSpan.FromMinutes(5))));

    // Product catalog - cached, moderate retry
    services.AddHttpClient<ProductCatalogClient>()
        .AddResilience(HttpClientPresets.FastInternalApi());

    // Shipping provider - external, slow
    services.AddHttpClient<ShippingProviderClient>()
        .AddResilience(HttpClientPresets.SlowExternalApi());

    // Email service - fire-and-forget
    services.AddHttpClient<EmailServiceClient>()
        .AddResilience(builder => builder
            .WithTimeout(TimeSpan.FromSeconds(10))
            .WithRetry(retry => retry.WithMaxRetries(2))
            .WithoutCircuitBreaker());
}
```

### Microservices Architecture

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Service-to-service communication
    services.AddHttpClient<UserServiceClient>("user-service")
        .AddResilience(HttpClientPresets.FastInternalApi());

    services.AddHttpClient<OrderServiceClient>("order-service")
        .AddResilience(HttpClientPresets.FastInternalApi());

    services.AddHttpClient<InventoryServiceClient>("inventory-service")
        .AddResilience(HttpClientPresets.FastInternalApi());

    // External partner APIs
    services.AddHttpClient<PartnerApiClient>("partner-api")
        .AddResilience(HttpClientPresets.SlowExternalApi(), options =>
        {
            options.UserAgent = "MyCompany-Integration/2.0";
        });
}
```

### Data Processing Pipeline

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Real-time data ingestion
    services.AddHttpClient<RealTimeDataClient>()
        .AddResilience(HttpClientPresets.RealTimeApi());

    // Batch data processing
    services.AddHttpClient<BatchProcessingClient>()
        .AddResilience(builder => builder
            .WithTimeout(TimeSpan.FromMinutes(30))
            .WithRetry(retry => retry
                .WithMaxRetries(2)
                .WithBaseDelay(TimeSpan.FromSeconds(5)))
            .WithoutCircuitBreaker());

    // File storage (uploads/downloads)
    services.AddHttpClient<FileStorageClient>()
        .AddResilience(HttpClientPresets.FileDownload());
}
```

## Environment-Specific Configuration

### Development Environment

```csharp
#if DEBUG
public void ConfigureServices(IServiceCollection services)
{
    // Fast-fail in development for quick debugging
    services.AddHttpClient<ApiClient>()
        .AddResilience(builder => builder
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithRetry(retry => retry.WithMaxRetries(1))
            .WithCircuitBreaker(cb => cb
                .WithFailureThreshold(2)
                .WithOpenDuration(TimeSpan.FromSeconds(10))));
}
#endif
```

### Production Environment

```csharp
#if !DEBUG
public void ConfigureServices(IServiceCollection services)
{
    // Production-ready resilience
    services.AddHttpClient<ApiClient>()
        .AddResilience(HttpClientPresets.SlowExternalApi())
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 100,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        });
}
#endif
```

## Configuration from appsettings.json

### appsettings.json

```json
{
  "HttpClients": {
    "PaymentApi": {
      "BaseUrl": "https://payments.example.com",
      "TimeoutSeconds": 30,
      "Retry": {
        "MaxRetries": 5,
        "BaseDelay": "00:00:01",
        "MaxDelay": "00:00:30",
        "JitterFactor": 0.25
      },
      "CircuitBreaker": {
        "FailuresBeforeOpen": 3,
        "OpenDuration": "00:05:00"
      }
    },
    "EmailApi": {
      "BaseUrl": "https://email.example.com",
      "TimeoutSeconds": 10,
      "Retry": {
        "MaxRetries": 2
      },
      "CircuitBreaker": {
        "Enabled": false
      }
    }
  }
}
```

### Configuration Code

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Bind configuration from appsettings.json
    var paymentConfig = configuration.GetSection("HttpClients:PaymentApi").Get<HttpClientOptions>();
    var emailConfig = configuration.GetSection("HttpClients:EmailApi").Get<HttpClientOptions>();

    services.AddHttpClient<PaymentApiClient>(c =>
    {
        c.BaseAddress = new Uri(paymentConfig.BaseUrl);
    })
    .AddResilience(paymentConfig);

    services.AddHttpClient<EmailApiClient>(c =>
    {
        c.BaseAddress = new Uri(emailConfig.BaseUrl);
    })
    .AddResilience(emailConfig);
}
```

## Testing Configuration

### Unit Testing

```csharp
[Test]
public void Should_Use_Correct_Preset_Configuration()
{
    var preset = HttpClientPresets.FastInternalApi();

    preset.TimeoutSeconds.Should().Be(10);
    preset.Retry.MaxRetries.Should().Be(5);
    preset.Retry.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(200));
    preset.CircuitBreaker.FailuresBeforeOpen.Should().Be(3);
    preset.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromSeconds(30));
}

[Test]
public void Should_Build_Custom_Configuration()
{
    var options = new HttpClientOptionsBuilder()
        .WithTimeout(TimeSpan.FromMinutes(1))
        .WithRetry(retry => retry
            .WithMaxRetries(3)
            .WithBaseDelay(TimeSpan.FromSeconds(2)))
        .WithCircuitBreaker(cb => cb
            .WithFailureThreshold(5)
            .WithOpenDuration(TimeSpan.FromMinutes(2)))
        .Build();

    options.TimeoutSeconds.Should().Be(60);
    options.Retry.MaxRetries.Should().Be(3);
    options.Retry.BaseDelay.Should().Be(TimeSpan.FromSeconds(2));
    options.CircuitBreaker.FailuresBeforeOpen.Should().Be(5);
    options.CircuitBreaker.OpenDuration.Should().Be(TimeSpan.FromMinutes(2));
}
```

### Integration Testing

```csharp
[Test]
public async Task Should_Apply_Resilience_Policies()
{
    var services = new ServiceCollection();

    services.AddHttpClient<TestApiClient>()
        .AddResilience(HttpClientPresets.FastInternalApi())
        .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler());

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<TestApiClient>();

    // Test that resilience policies are applied
    var result = await client.GetDataAsync();

    result.Should().NotBeNull();
}
```

## Performance Optimization Examples

### High-Throughput APIs

```csharp
services.AddHttpClient<HighThroughputApiClient>()
    .AddResilience(HttpClientPresets.FastInternalApi())
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 200,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        EnableMultipleHttp2Connections = true
    });
```

### Memory-Efficient Configuration

```csharp
services.AddHttpClient<EfficientApiClient>()
    .AddResilience(builder => builder
        .WithRetry(retry => retry
            .WithMaxRetries(2)
            .WithoutJitter()) // Reduce memory allocation
        .WithCircuitBreaker(cb => cb
            .WithFailureThreshold(10))) // Higher threshold to reduce state changes
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 10, // Lower connection pool
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });
```

## Summary

Choose the configuration method that best fits your needs:

1. **Zero Config**: For most scenarios, just use `.AddResilience()`
2. **Presets**: For common patterns, use `HttpClientPresets.*`
3. **Builder Pattern**: For complex custom configuration
4. **Traditional**: For gradual migration or simple overrides
5. **Configuration Files**: For environment-specific settings

All methods can be combined and customized as needed!
