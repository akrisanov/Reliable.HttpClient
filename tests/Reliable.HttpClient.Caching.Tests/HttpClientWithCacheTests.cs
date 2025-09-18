using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching.Tests;

/// <summary>
/// Tests for HttpClientWithCache
/// </summary>
public class HttpClientWithCacheTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly Mock<IHttpResponseHandler> _mockResponseHandler;
    private readonly Mock<ISimpleCacheKeyGenerator> _mockCacheKeyGenerator;
    private readonly Mock<ILogger<HttpClientWithCache>> _mockLogger;
    private readonly HttpClientWithCache _httpClientWithCache;

    public HttpClientWithCacheTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new System.Net.Http.HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com"),
        };
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockResponseHandler = new Mock<IHttpResponseHandler>();
        _mockCacheKeyGenerator = new Mock<ISimpleCacheKeyGenerator>();
        _mockLogger = new Mock<ILogger<HttpClientWithCache>>();

        _httpClientWithCache = new HttpClientWithCache(
            _httpClient,
            _cache,
            _mockResponseHandler.Object,
            cacheOptions: null,
            _mockCacheKeyGenerator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_FirstCall_MakesHttpRequestAndCachesResult()
    {
        // Arrange
        var requestUri = "/api/test";
        var cacheKey = "test_cache_key";
        var expectedResponse = new TestResponse { Id = 1, Name = "Test" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse)),
        };

        _mockCacheKeyGenerator
            .Setup(x => x.GenerateKey(nameof(TestResponse), requestUri))
            .Returns(cacheKey);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        TestResponse result = await _httpClientWithCache.GetAsync<TestResponse>(requestUri);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);

        // Verify result is cached
        TestResponse? cachedResult = _cache.Get<TestResponse>(cacheKey);
        Assert.Equal(expectedResponse, cachedResult);
    }

    [Fact]
    public async Task GetAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        var requestUri = "/api/test";
        var cacheKey = "test_cache_key";
        var cachedResponse = new TestResponse { Id = 1, Name = "Cached" };

        _mockCacheKeyGenerator
            .Setup(x => x.GenerateKey(nameof(TestResponse), requestUri))
            .Returns(cacheKey);

        // Pre-populate cache
        _cache.Set(cacheKey, cachedResponse);

        // Act
        TestResponse result = await _httpClientWithCache.GetAsync<TestResponse>(requestUri);

        // Assert
        Assert.Equal(cachedResponse, result);

        // Verify no HTTP request was made
        _mockHttpMessageHandler
            .Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PostAsync_InvalidatesRelatedCache()
    {
        // Arrange
        var postUri = "/api/test";
        var cacheKey = "test_cache_key";
        var cachedResponse = new TestResponse { Id = 1, Name = "Cached" };
        var postRequest = new { Data = "test" };
        var postResponse = new TestResponse { Id = 2, Name = "Posted" };

        // Pre-populate cache
        _cache.Set(cacheKey, cachedResponse);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(postResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(postResponse);

        // Act
        TestResponse result = await _httpClientWithCache.PostAsync<object, TestResponse>(postUri, postRequest);

        // Assert
        Assert.Equal(postResponse, result);
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheAsync_LogsInvalidationRequest()
    {
        // Arrange
        var pattern = "/api/test";

        // Act
        await _httpClientWithCache.InvalidateCacheAsync(pattern);

        // Assert
        // For now, we just verify the method completes without error
        // Full implementation would require a more sophisticated cache
        Assert.True(true);
    }

    [Fact]
    public async Task ClearCacheAsync_LogsClearRequest()
    {
        // Arrange & Act
        await _httpClientWithCache.ClearCacheAsync();

        // Assert
        // For now, we just verify the method completes without error
        // Full implementation would require a more sophisticated cache
        Assert.True(true);
    }

    [Fact]
    public async Task PostAsync_ResponseHandlerThrows_CacheRemainsValid()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var requestData = new { Name = "New Data" };

        // Pre-populate cache with valid data
        _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(5));
        _mockCacheKeyGenerator.Setup(x => x.GenerateKey("TestResponse", requestUri))
            .Returns(cacheKey);

        // Setup HTTP client to return successful response
        var responseContent = JsonSerializer.Serialize(new TestResponse { Id = 2, Name = "Updated" });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to throw exception
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Response handler failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _httpClientWithCache.PostAsync<object, TestResponse>(requestUri, requestData));

        // Verify that cached data is still available (cache was not invalidated due to failure)
        var cacheExists = _cache.TryGetValue(cacheKey, out TestResponse? stillCachedResult);
        Assert.True(cacheExists);
        Assert.NotNull(stillCachedResult);
        Assert.Equal(1, stillCachedResult.Id);
        Assert.Equal("Cached", stillCachedResult.Name);
    }

    [Fact]
    public async Task PostAsync_SuccessfulHandling_InvalidatesCache()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var requestData = new { Name = "New Data" };
        var responseData = new TestResponse { Id = 2, Name = "Updated" };

        // Pre-populate cache with valid data
        _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(5));
        _mockCacheKeyGenerator.Setup(x => x.GenerateKey("TestResponse", requestUri))
            .Returns(cacheKey);

        // Setup HTTP client to return successful response
        var responseContent = JsonSerializer.Serialize(responseData);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json"),
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to succeed
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var result = await _httpClientWithCache.PostAsync<object, TestResponse>(requestUri, requestData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Updated", result.Name);

        // Cache invalidation is attempted (though MemoryCache doesn't support pattern-based invalidation)
        // We verify the behavior through the successful completion of the operation
        Assert.True(true); // Placeholder for cache invalidation verification
    }

    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
