using System.Net;
using System.Text;

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using Xunit;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching.Tests;

/// <summary>
/// Tests for HttpClientWithCache headers functionality
/// </summary>
public class HttpClientWithCacheHeadersTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly Mock<IHttpResponseHandler> _mockResponseHandler;
    private readonly Mock<ISimpleCacheKeyGenerator> _mockCacheKeyGenerator;
    private readonly HttpClientWithCache _httpClientWithCache;

    public HttpClientWithCacheHeadersTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new System.Net.Http.HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com"),
        };
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockResponseHandler = new Mock<IHttpResponseHandler>();
        _mockCacheKeyGenerator = new Mock<ISimpleCacheKeyGenerator>();

        var cacheOptions = new HttpCacheOptions();
        cacheOptions.DefaultHeaders["X-Default-Header"] = "default-value";
        cacheOptions.DefaultHeaders["Authorization"] = "Bearer default-token";

        _httpClientWithCache = new HttpClientWithCache(
            _httpClient,
            _cache,
            _mockResponseHandler.Object,
            cacheOptions,
            _mockCacheKeyGenerator.Object);
    }

    [Fact]
    public async Task GetAsync_WithCustomHeaders_IncludesHeadersInCacheKey()
    {
        // Arrange
        var requestUri = "/test";
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Custom-Header"] = "custom-value",
            ["Authorization"] = "Bearer user-token", // Override default
        };

        _mockCacheKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-cache-key");

        SetupHttpResponse(HttpStatusCode.OK, """{"id": 1, "name": "Test"}""");

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "Test" });

        // Act
        TestModel result = await _httpClientWithCache.GetAsync<TestModel>(requestUri, customHeaders);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");

        // Verify cache key includes headers
        _mockCacheKeyGenerator.Verify(
            x => x.GenerateKey("TestModel", It.Is<string>(s => s.Contains("#") && s.Contains("Authorization=Bearer user-token"))),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithCustomHeaders_SendsCorrectHeadersToServer()
    {
        // Arrange
        var requestUri = "/test";
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Request-ID"] = "123456",
            ["Authorization"] = "Bearer user-token",
        };

        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id": 1, "name": "Test"}""", Encoding.UTF8, "application/json"),
            });

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "Test" });

        _mockCacheKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-cache-key");

        // Act
        await _httpClientWithCache.GetAsync<TestModel>(requestUri, customHeaders);

        // Assert
        capturedRequest.Should().NotBeNull();

        // Check that all headers are present (default + custom, with custom overriding default)
        capturedRequest!.Headers.GetValues("X-Default-Header").Should().Contain("default-value");
        capturedRequest!.Headers.GetValues("X-Request-ID").Should().Contain("123456");
        capturedRequest.Headers.GetValues("Authorization").Should().Contain("Bearer user-token"); // Override
    }

    [Fact]
    public async Task GetAsync_DifferentHeaders_GeneratesDifferentCacheKeys()
    {
        // Arrange
        var requestUri = "/test";
        var headers1 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["X-User"] = "user1" };
        var headers2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["X-User"] = "user2" };

        var cacheKeyCallCount = 0;
        _mockCacheKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => $"cache-key-{++cacheKeyCallCount}");

        SetupHttpResponse(HttpStatusCode.OK, """{"id": 1, "name": "Test"}""");

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "Test" });

        // Act
        await _httpClientWithCache.GetAsync<TestModel>(requestUri, headers1);
        await _httpClientWithCache.GetAsync<TestModel>(requestUri, headers2);

        // Assert - should generate different cache keys
        _mockCacheKeyGenerator.Verify(
            x => x.GenerateKey("TestModel", It.Is<string>(s => s.Contains("X-User=user1"))),
            Times.Once);

        _mockCacheKeyGenerator.Verify(
            x => x.GenerateKey("TestModel", It.Is<string>(s => s.Contains("X-User=user2"))),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_WithCustomHeaders_PassesHeadersCorrectly()
    {
        // Arrange
        var requestUri = "/create";
        var requestData = new { Name = "New Item" };
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Transaction-ID"] = "tx-123",
            ["Content-Type"] = "application/json",
        };

        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"id": 1, "name": "New Item"}""", Encoding.UTF8, "application/json"),
            });

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "New Item" });

        // Act
        await _httpClientWithCache.PostAsync<object, TestModel>(requestUri, requestData, customHeaders);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);

        // Verify headers are included - check both request headers and content headers
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> allHeaders = capturedRequest.Headers.Concat(
            capturedRequest.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>());

        allHeaders.Should().Contain(h => h.Key == "X-Default-Header" && h.Value.Contains("default-value"));
        allHeaders.Should().Contain(h => h.Key == "X-Transaction-ID" && h.Value.Contains("tx-123"));
    }

    [Fact]
    public async Task PutAsync_WithCustomHeaders_PassesHeadersCorrectly()
    {
        // Arrange
        var requestUri = "/update/1";
        var requestData = new { Name = "Updated Item" };
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Idempotency-Key"] = "idem-456",
        };

        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id": 1, "name": "Updated Item"}""", Encoding.UTF8, "application/json")
            });

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "Updated Item" });

        // Act
        await _httpClientWithCache.PutAsync<object, TestModel>(requestUri, requestData, customHeaders);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Put);
        capturedRequest.Headers.GetValues("X-Idempotency-Key").Should().Contain("idem-456");
    }

    [Fact]
    public async Task DeleteAsync_WithCustomHeaders_PassesHeadersCorrectly()
    {
        // Arrange
        var requestUri = "/delete/1";
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Confirm-Delete"] = "true",
        };

        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "Deleted" });

        // Act
        await _httpClientWithCache.DeleteAsync<TestModel>(requestUri, customHeaders);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Delete);
        capturedRequest.Headers.GetValues("X-Confirm-Delete").Should().Contain("true");
    }

    [Fact]
    public async Task GetAsync_WithoutCustomHeaders_UsesOnlyDefaultHeaders()
    {
        // Arrange
        var requestUri = "/test";

        _mockCacheKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-cache-key");

        SetupHttpResponse(HttpStatusCode.OK, """{"id": 1, "name": "Test"}""");

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestModel>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestModel { Id = 1, Name = "Test" });

        // Act
        await _httpClientWithCache.GetAsync<TestModel>(requestUri);

        // Assert - should include default headers in cache key
        _mockCacheKeyGenerator.Verify(
            x => x.GenerateKey("TestModel", It.Is<string>(s =>
                s.Contains('#') &&
                s.Contains("Authorization=Bearer default-token") &&
                s.Contains("X-Default-Header=default-value"))),
            Times.Once);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            });
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _cache?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
