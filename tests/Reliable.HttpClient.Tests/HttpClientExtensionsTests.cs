using System.Net;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientExtensionsTests
{
    private readonly IServiceCollection _services = null!;
    private readonly IConfiguration _configuration = null!;

    public HttpClientExtensionsTests()
    {
        _services = new ServiceCollection();
        _services.AddLogging();

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["TestApi:BaseUrl"] = "https://api.test.com",
            ["TestApi:TimeoutSeconds"] = "45",
            ["TestApi:Retry:MaxRetries"] = "5",
            ["TestApi:Retry:BaseDelay"] = "00:00:02",
            ["TestApi:Retry:MaxDelay"] = "00:01:00",
            ["TestApi:UserAgent"] = "Test-Client/2.0",
            ["TestApi:CircuitBreaker:Enabled"] = "true",
            ["TestApi:CircuitBreaker:FailuresBeforeOpen"] = "3",
            ["TestApi:CircuitBreaker:OpenDuration"] = "00:00:30"
        });
        _configuration = configurationBuilder.Build();
    }

    [Fact]
    public void ConfigureResilientClient_ConfiguresHttpClientCorrectly()
    {
        // Arrange
        _services.Configure<TestHttpClientOptions>(options => _configuration.GetSection("TestApi").Bind(options));
        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>();

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(typeof(TestHttpClient).Name);

        // Assert
        httpClient.BaseAddress.Should().Be(new Uri("https://api.test.com"));
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(45));

        var userAgentHeader = httpClient.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault();
        userAgentHeader.Should().Be("Test-Client/2.0");
    }

    [Fact]
    public void ConfigureResilientClient_WithEmptyBaseUrl_DoesNotSetBaseAddress()
    {
        // Arrange
        _services.Configure<TestHttpClientOptions>(options =>
        {
            options.BaseUrl = string.Empty;
            options.TimeoutSeconds = 30;
        });

        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>();

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(typeof(TestHttpClient).Name);

        // Assert
        httpClient.BaseAddress.Should().BeNull();
    }

    [Fact]
    public void ConfigureResilientClient_WithEmptyUserAgent_DoesNotAddHeader()
    {
        // Arrange
        _services.Configure<TestHttpClientOptions>(options =>
        {
            options.BaseUrl = "https://api.test.com";
            options.UserAgent = string.Empty;
        });

        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>();

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(typeof(TestHttpClient).Name);

        // Assert
        httpClient.DefaultRequestHeaders.Contains("User-Agent").Should().BeFalse();
    }

    [Fact]
    public void ConfigureResilientClient_WithCustomClientConfiguration_AppliesCustomSettings()
    {
        // Arrange
        _services.Configure<TestHttpClientOptions>(options => _configuration.GetSection("TestApi").Bind(options));

        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>((options, client) =>
            {
                client.DefaultRequestHeaders.Add("X-API-Key", "test-key");
                client.DefaultRequestHeaders.Add("X-Custom-Header", "custom-value");
            });

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(typeof(TestHttpClient).Name);

        // Assert
        var apiKeyHeader = httpClient.DefaultRequestHeaders.GetValues("X-API-Key").FirstOrDefault();
        var customHeader = httpClient.DefaultRequestHeaders.GetValues("X-Custom-Header").FirstOrDefault();

        apiKeyHeader.Should().Be("test-key");
        customHeader.Should().Be("custom-value");
    }

    [Fact]
    public void ConfigureResilientClient_WithNamedClient_ConfiguresCorrectly()
    {
        // Arrange
        const string clientName = "TestNamedClient";
        _services.Configure<TestHttpClientOptions>(options => _configuration.GetSection("TestApi").Bind(options));

        _services.AddHttpClient(clientName)
            .ConfigureResilientClient<TestHttpClientOptions>(clientName);

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient(clientName);

        // Assert
        httpClient.BaseAddress.Should().Be(new Uri("https://api.test.com"));
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task BasicHttpRequest_WorksCorrectlyAsync()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.ReturnsResponse(HttpStatusCode.OK, "Test Response");

        _services.Configure<TestHttpClientOptions>(options => _configuration.GetSection("TestApi").Bind(options));

        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        TestHttpClient client = serviceProvider.GetRequiredService<TestHttpClient>();

        // Act
        HttpResponseMessage response = await client.GetAsync("test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Test Response");
        mockHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_DoesNotRetryOnClientErrorsAsync()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.ReturnsResponse(HttpStatusCode.BadRequest);

        _services.Configure<TestHttpClientOptions>(options =>
        {
            options.Retry.MaxRetries = 3;
            options.Retry.BaseDelay = TimeSpan.FromMilliseconds(50);
        });

        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        TestHttpClient client = serviceProvider.GetRequiredService<TestHttpClient>();

        // Act
        HttpResponseMessage response = await client.GetAsync("test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // Should not retry for 4xx errors
        mockHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task CircuitBreaker_DisabledWhenConfiguredAsync()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.ReturnsResponse(HttpStatusCode.InternalServerError);

        _services.Configure<TestHttpClientOptions>(options =>
        {
            options.Retry.MaxRetries = 0; // Disable retry for test clarity
            options.CircuitBreaker.Enabled = false;
        });

        _services.AddHttpClient<TestHttpClient>()
            .ConfigureResilientClient<TestHttpClient, TestHttpClientOptions>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        TestHttpClient client = serviceProvider.GetRequiredService<TestHttpClient>();

        // Act - make many requests that should have triggered circuit breaker
        for (var i = 0; i < 10; i++)
        {
            await client.GetAsync("test");
        }

        // Assert - all requests should pass, circuit breaker is disabled
        mockHandler.RequestCount.Should().Be(10);
    }
}

// Test classes
public class TestHttpClientOptions : HttpClientOptions
{
    public TestHttpClientOptions()
    {
        BaseUrl = "https://api.test.com";
        TimeoutSeconds = 30;
        UserAgent = "Test-Client/1.0";

        // Configure retry defaults
        Retry.MaxRetries = 3;
        Retry.BaseDelay = TimeSpan.FromMilliseconds(200);

        // Configure circuit breaker defaults
        CircuitBreaker.Enabled = true;
        CircuitBreaker.FailuresBeforeOpen = 5;
    }
}

public class TestHttpClient(System.Net.Http.HttpClient httpClient)
{
    private readonly System.Net.Http.HttpClient _httpClient = httpClient;

    public async Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        return await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
    }
}

// Mock HTTP Message Handler for testing
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _content = string.Empty;
    public int RequestCount { get; private set; }

    public MockHttpMessageHandler ReturnsResponse(HttpStatusCode statusCode, string content = "")
    {
        _statusCode = statusCode;
        _content = content;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content),
        });
    }
}

