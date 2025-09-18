using System.Net;
using System.Text;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using Reliable.HttpClient.Caching.Abstractions;
using Reliable.HttpClient.Caching.Generic;

using NetHttpClient = System.Net.Http.HttpClient;

using Xunit;

namespace Reliable.HttpClient.Caching.Tests;

public class CachedHttpClientTests : IDisposable
{
    private readonly Mock<IHttpResponseCache<TestResponse>> _mockCache;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ILogger<CachedHttpClient<TestResponse>>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly NetHttpClient _httpClient;
    private readonly HttpCacheOptions _options;
    private readonly IOptionsSnapshot<HttpCacheOptions> _optionsSnapshot;
    private readonly CachedHttpClient<TestResponse> _cachedClient;

    public CachedHttpClientTests()
    {
        _mockCache = new Mock<IHttpResponseCache<TestResponse>>();
        _mockKeyGenerator = new Mock<ICacheKeyGenerator>();
        _mockLogger = new Mock<ILogger<CachedHttpClient<TestResponse>>>();
        _mockHandler = new Mock<HttpMessageHandler>();

        _httpClient = new NetHttpClient(_mockHandler.Object);

        _options = new HttpCacheOptions
        {
            KeyGenerator = _mockKeyGenerator.Object
        };

        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<HttpCacheOptions>>();
        mockOptionsSnapshot.Setup(x => x.Value).Returns(_options);
        _optionsSnapshot = mockOptionsSnapshot.Object;

        _cachedClient = new CachedHttpClient<TestResponse>(
            _httpClient,
            _mockCache.Object,
            _optionsSnapshot,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_WithCacheableRequest_ChecksCacheFirst()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var cacheKey = "test-cache-key";
        var cachedResponse = new TestResponse { Id = 1, Name = "Cached" };

        _mockKeyGenerator.Setup(x => x.GenerateKey(request)).Returns(cacheKey);
        _mockCache.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedResponse);

        // Act
        TestResponse result = await _cachedClient.SendAsync(request,
            response => Task.FromResult(new TestResponse { Id = 2, Name = "Fresh" }));

        // Assert
        result.Should().Be(cachedResponse);
        result.Id.Should().Be(1);
        result.Name.Should().Be("Cached");

        _mockCache.Verify(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockHandler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithCacheMiss_ExecutesRequestAndCaches()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var cacheKey = "test-cache-key";
        var freshResponse = new TestResponse { Id = 2, Name = "Fresh" };

        _mockKeyGenerator.Setup(x => x.GenerateKey(request)).Returns(cacheKey);
        _mockCache.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((TestResponse?)null);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":2,\"name\":\"Fresh\"}"),
        };

        _mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponse);

        // Act
        var result = await _cachedClient.SendAsync(request,
            response => Task.FromResult(freshResponse));

        // Assert
        result.Should().Be(freshResponse);
        result.Id.Should().Be(2);
        result.Name.Should().Be("Fresh");

        _mockCache.Verify(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(cacheKey, freshResponse, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithNonCacheableMethod_SkipsCache()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");
        var freshResponse = new TestResponse { Id = 2, Name = "Fresh" };

        // Configure options to only cache GET requests
        _options.CacheableMethods.Clear();
        _options.CacheableMethods.Add(HttpMethod.Get);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponse);

        // Act
        TestResponse result = await _cachedClient.SendAsync(request,
            response => Task.FromResult(freshResponse));

        // Assert
        result.Should().Be(freshResponse);

        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TestResponse>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithNonCacheableStatusCode_DoesNotCache()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var cacheKey = "test-cache-key";
        var freshResponse = new TestResponse { Id = 2, Name = "Fresh" };

        _mockKeyGenerator.Setup(x => x.GenerateKey(request)).Returns(cacheKey);
        _mockCache.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((TestResponse?)null);

        // Configure options to only cache 200 OK responses
        _options.CacheableStatusCodes.Clear();
        _options.CacheableStatusCodes.Add(HttpStatusCode.OK);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        _mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponse);

        // Act
        var result = await _cachedClient.SendAsync(request,
            response => Task.FromResult(freshResponse));

        // Assert
        result.Should().Be(freshResponse);

        _mockCache.Verify(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TestResponse>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFromJsonAsync_WithSuccessfulResponse_DeserializesAndCaches()
    {
        // Arrange
        var requestUri = "https://api.example.com/users/1";
        var cacheKey = "test-cache-key";
        var responseJson = "{\"Id\":1,\"Name\":\"John\"}";

        _mockKeyGenerator.Setup(x => x.GenerateKey(It.IsAny<HttpRequestMessage>())).Returns(cacheKey);
        _mockCache.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((TestResponse?)null);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(httpResponse);

        // Act
        var result = await _cachedClient.GetFromJsonAsync(requestUri);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("John");

        _mockCache.Verify(x => x.SetAsync(cacheKey, It.IsAny<TestResponse>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearCacheAsync_CallsCacheClear()
    {
        // Act
        await _cachedClient.ClearCacheAsync();

        // Assert
        _mockCache.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromCacheAsync_GeneratesKeyAndRemoves()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var cacheKey = "test-cache-key";

        _mockKeyGenerator.Setup(x => x.GenerateKey(request)).Returns(cacheKey);

        // Act
        await _cachedClient.RemoveFromCacheAsync(request);

        // Assert
        _mockKeyGenerator.Verify(x => x.GenerateKey(request), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<CachedHttpClient<TestResponse>> action = () => new CachedHttpClient<TestResponse>(
            null!,
            _mockCache.Object,
            _optionsSnapshot,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<CachedHttpClient<TestResponse>> action = () => new CachedHttpClient<TestResponse>(
            _httpClient,
            null!,
            _optionsSnapshot,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<CachedHttpClient<TestResponse>> action = () => new CachedHttpClient<TestResponse>(
            _httpClient,
            _mockCache.Object,
            _optionsSnapshot,
            null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    public class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
