using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Extensions;
using Reliable.HttpClient.Caching.Generic;
using Reliable.HttpClient.Caching.Generic.Extensions;

using NetHttpClient = System.Net.Http.HttpClient;

using Xunit;

namespace Reliable.HttpClient.Caching.Tests.Integration;

public class EndToEndTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly CachedHttpClient<TestResponse> _cachedClient;
    private readonly MockHttpMessageHandler _mockHandler;

    public EndToEndTests()
    {
        _mockHandler = new MockHttpMessageHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpClient<TestApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => _mockHandler);
        services.AddGenericHttpClientCaching<TestApiClient, TestResponse>();

        _serviceProvider = services.BuildServiceProvider();

        // Create cached client with the HttpClient that has the mock handler
        IHttpClientFactory httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        NetHttpClient httpClient = httpClientFactory.CreateClient(nameof(TestApiClient));
        IHttpResponseCache<TestResponse> cache = _serviceProvider.GetRequiredService<IHttpResponseCache<TestResponse>>();
        IOptionsSnapshot<HttpCacheOptions> options = _serviceProvider.GetRequiredService<IOptionsSnapshot<HttpCacheOptions>>();
        ILogger<CachedHttpClient<TestResponse>> logger = _serviceProvider.GetRequiredService<ILogger<CachedHttpClient<TestResponse>>>();

        _cachedClient = new CachedHttpClient<TestResponse>(httpClient, cache, options, logger);
    }

    [Fact]
    public async Task GetFromJsonAsync_FirstRequest_ExecutesHttpCallAndCaches()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new TestResponse { Id = 1, Name = "John" });
        _mockHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

        // Act
        TestResponse result1 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/1");
        TestResponse result2 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/1");

        // Assert
        result1.Should().NotBeNull();
        result1.Id.Should().Be(1);
        result1.Name.Should().Be("John");

        result2.Should().NotBeNull();
        result2.Id.Should().Be(1);
        result2.Name.Should().Be("John");

        // Should only make one HTTP call due to caching
        _mockHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WithDifferentRequests_CachesSeparately()
    {
        // Arrange
        var response1Json = JsonSerializer.Serialize(new TestResponse { Id = 1, Name = "John" });
        var response2Json = JsonSerializer.Serialize(new TestResponse { Id = 2, Name = "Jane" });

        _mockHandler.SetupSequentialResponses(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response1Json, Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response2Json, Encoding.UTF8, "application/json"),
            }
        );

        // Act
        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/2");

        TestResponse result1 = await _cachedClient.SendAsync(request1, DeserializeResponse);
        TestResponse result2 = await _cachedClient.SendAsync(request2, DeserializeResponse);

        // Repeat requests - should come from cache
        TestResponse cachedResult1 = await _cachedClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1"),
            DeserializeResponse);

        TestResponse cachedResult2 = await _cachedClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/2"),
            DeserializeResponse);

        // Assert
        result1.Id.Should().Be(1);
        result1.Name.Should().Be("John");
        result2.Id.Should().Be(2);
        result2.Name.Should().Be("Jane");

        cachedResult1.Id.Should().Be(1);
        cachedResult1.Name.Should().Be("John");
        cachedResult2.Id.Should().Be(2);
        cachedResult2.Name.Should().Be("Jane");

        // Should make exactly 2 HTTP calls
        _mockHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WithAuthenticatedRequests_CachesSeparatelyByAuth()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new TestResponse { Id = 1, Name = "John" });
        _mockHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        });

        // Act
        var publicRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1");
        var authRequest1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1");
        var authRequest2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1");

        authRequest1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token1");
        authRequest2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token2");

        TestResponse publicResult = await _cachedClient.SendAsync(publicRequest, DeserializeResponse);
        TestResponse authResult1 = await _cachedClient.SendAsync(authRequest1, DeserializeResponse);
        TestResponse authResult2 = await _cachedClient.SendAsync(authRequest2, DeserializeResponse);

        // Assert
        publicResult.Should().NotBeNull();
        authResult1.Should().NotBeNull();
        authResult2.Should().NotBeNull();

        // Should make 3 HTTP calls - different cache keys due to different auth contexts
        _mockHandler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task ClearCacheAsync_RemovesAllCachedResponses()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new TestResponse { Id = 1, Name = "John" });
        _mockHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        });

        // Act
        TestResponse result1 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/1");
        _mockHandler.RequestCount.Should().Be(1);

        // Clear cache
        await _cachedClient.ClearCacheAsync();

        // Make the same request again
        TestResponse result2 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/1");

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        // Should make 2 HTTP calls because cache was cleared
        _mockHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task RemoveFromCacheAsync_RemovesSpecificCachedResponse()
    {
        // Arrange
        var response1Json = JsonSerializer.Serialize(new TestResponse { Id = 1, Name = "John" });
        var response2Json = JsonSerializer.Serialize(new TestResponse { Id = 2, Name = "Jane" });

        _mockHandler.SetupSequentialResponses(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response1Json, Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response2Json, Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response1Json, Encoding.UTF8, "application/json"),
            }
        );

        // Act
        TestResponse result1 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/1");
        TestResponse result2 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/2");
        _mockHandler.RequestCount.Should().Be(2);

        // Remove specific item from cache
        var removeRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users/1");
        await _cachedClient.RemoveFromCacheAsync(removeRequest);

        // Make requests again
        TestResponse cachedResult1 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/1"); // Should hit HTTP
        TestResponse cachedResult2 = await _cachedClient.GetFromJsonAsync("https://api.example.com/users/2"); // Should hit cache

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        cachedResult1.Should().NotBeNull();
        cachedResult2.Should().NotBeNull();

        // Should make 3 HTTP calls total (1 was removed from cache)
        _mockHandler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_WithNonCacheableMethod_SkipsCache()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new TestResponse { Id = 1, Name = "John" });
        _mockHandler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        });

        // Act
        var request1 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");

        TestResponse result1 = await _cachedClient.SendAsync(request1, DeserializeResponse);
        TestResponse result2 = await _cachedClient.SendAsync(request2, DeserializeResponse);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        // Should make 2 HTTP calls because POST is not cacheable by default
        _mockHandler.RequestCount.Should().Be(2);
    }

    private static async Task<TestResponse> DeserializeResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<TestResponse>(json)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class TestApiClient
    {
        // This class is used only as a type parameter for DI registration
        // No actual HttpClient usage needed in tests
    }

    public class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        private HttpResponseMessage? _defaultResponse;

        public int RequestCount { get; private set; }

        public void SetResponse(HttpResponseMessage response)
        {
            _defaultResponse = response;
        }

        public void SetupSequentialResponses(params HttpResponseMessage[] responses)
        {
            _responses.Clear();
            foreach (HttpResponseMessage response in responses)
            {
                _responses.Enqueue(response);
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (_responses.Count > 0)
            {
                return Task.FromResult(_responses.Dequeue());
            }

            if (_defaultResponse is not null)
            {
                return Task.FromResult(_defaultResponse);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
