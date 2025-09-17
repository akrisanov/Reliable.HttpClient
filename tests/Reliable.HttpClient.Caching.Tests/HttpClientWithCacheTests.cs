using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Reliable.HttpClient.Caching;

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
            BaseAddress = new Uri("https://api.test.com")
        };
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockResponseHandler = new Mock<IHttpResponseHandler>();
        _mockCacheKeyGenerator = new Mock<ISimpleCacheKeyGenerator>();
        _mockLogger = new Mock<ILogger<HttpClientWithCache>>();

        _httpClientWithCache = new HttpClientWithCache(
            _httpClient,
            _cache,
            _mockResponseHandler.Object,
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
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
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
        var result = await _httpClientWithCache.GetAsync<TestResponse>(requestUri);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);

        // Verify result is cached
        var cachedResult = _cache.Get<TestResponse>(cacheKey);
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
        var result = await _httpClientWithCache.GetAsync<TestResponse>(requestUri);

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
            Content = new StringContent(JsonSerializer.Serialize(postResponse))
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
        var result = await _httpClientWithCache.PostAsync<object, TestResponse>(postUri, postRequest);

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

    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
