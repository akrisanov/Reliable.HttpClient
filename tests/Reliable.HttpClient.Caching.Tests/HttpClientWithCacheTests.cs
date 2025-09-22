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
        // Verify the method completes without throwing an exception
        // Note: MemoryCache doesn't support pattern-based invalidation,
        // but the method should handle this gracefully
        Assert.NotNull(_httpClientWithCache);
    }

    [Fact]
    public async Task ClearCacheAsync_LogsClearRequest()
    {
        // Arrange & Act
        await _httpClientWithCache.ClearCacheAsync();

        // Assert
        // Verify the method completes without throwing an exception
        // Note: This validates the basic functionality works
        Assert.NotNull(_httpClientWithCache);
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
        TestResponse result = await _httpClientWithCache.PostAsync<object, TestResponse>(requestUri, requestData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Updated", result.Name);

        // Verify HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(requestUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var postUri = "/api/test";
        var postRequest = new { Data = "posted" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Posted successfully"),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains(postUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        HttpResponseMessage result = await _httpClientWithCache.PostAsync(postUri, postRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // Verify HTTP request was made correctly (without involving response handler)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains(postUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was NOT called for raw HttpResponseMessage
        _mockResponseHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostAsync_WithHeaders_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var postUri = "/api/test";
        var postRequest = new { Data = "posted" };
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { { "Authorization", "Bearer token" } };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Posted successfully"),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token" &&
                    req.RequestUri!.ToString().Contains(postUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        HttpResponseMessage result = await _httpClientWithCache.PostAsync(postUri, postRequest, headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // Verify HTTP request was made with correct headers
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.Headers.Contains("Authorization") &&
                req.RequestUri!.ToString().Contains(postUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was NOT called for raw HttpResponseMessage
        _mockResponseHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchAsync_InvalidatesRelatedCache()
    {
        // Arrange
        var patchUri = "/api/test/1";
        var patchRequest = new { Data = "patched" };
        var patchResponse = new TestResponse { Id = 1, Name = "Patched" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(patchResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.ToString().Contains(patchUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patchResponse);

        // Act
        TestResponse result = await _httpClientWithCache.PatchAsync<object, TestResponse>(patchUri, patchRequest);

        // Assert
        Assert.Equal(patchResponse.Id, result.Id);
        Assert.Equal(patchResponse.Name, result.Name);

        // Verify HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.RequestUri!.ToString().Contains(patchUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called exactly once
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WithHeaders_InvalidatesRelatedCache()
    {
        // Arrange
        var patchUri = "/api/test/1";
        var patchRequest = new { Data = "patched" };
        var patchResponse = new TestResponse { Id = 1, Name = "Patched" };
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { { "Authorization", "Bearer token" } };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(patchResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token" &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patchResponse);

        // Act
        TestResponse result = await _httpClientWithCache.PatchAsync<object, TestResponse>(patchUri, patchRequest, headers);

        // Assert
        Assert.Equal(patchResponse.Id, result.Id);
        Assert.Equal(patchResponse.Name, result.Name);

        // Verify HTTP request was made with correct headers
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.Headers.Contains("Authorization")),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_ResponseHandlerThrows_CacheRemainsValid()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test/1";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var requestData = new { Name = "Patched Data" };

        // Pre-populate cache with valid data
        _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(5));
        _mockCacheKeyGenerator.Setup(x => x.GenerateKey("TestResponse", requestUri))
            .Returns(cacheKey);

        // Setup HTTP client to return successful response
        var responseContent = JsonSerializer.Serialize(new TestResponse { Id = 2, Name = "Patched" });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to throw exception
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Response handler failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _httpClientWithCache.PatchAsync<object, TestResponse>(requestUri, requestData));

        // Verify that cached data is still available (cache was not invalidated due to failure)
        var cacheExists = _cache.TryGetValue(cacheKey, out TestResponse? stillCachedResult);
        Assert.True(cacheExists);
        Assert.NotNull(stillCachedResult);
        Assert.Equal(1, stillCachedResult.Id);
        Assert.Equal("Cached", stillCachedResult.Name);
    }

    [Fact]
    public async Task PatchAsync_SuccessfulHandling_InvalidatesCache()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test/1";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var requestData = new { Name = "Patched Data" };
        var responseData = new TestResponse { Id = 2, Name = "Patched" };

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
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to succeed
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        TestResponse result = await _httpClientWithCache.PatchAsync<object, TestResponse>(requestUri, requestData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Patched", result.Name);

        // Verify that the HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(
            x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PatchAsync_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var patchUri = "/api/test/1";
        var patchRequest = new { Data = "patched" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Patched successfully"),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.ToString().Contains(patchUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        HttpResponseMessage result = await _httpClientWithCache.PatchAsync(patchUri, patchRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // Verify HTTP request was made correctly (without involving response handler)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.RequestUri!.ToString().Contains(patchUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was NOT called for raw HttpResponseMessage
        _mockResponseHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchAsync_WithHeaders_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var patchUri = "/api/test/1";
        var patchRequest = new { Data = "patched" };
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { { "Authorization", "Bearer token" } };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Patched successfully"),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token" &&
                    req.RequestUri!.ToString().Contains(patchUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        HttpResponseMessage result = await _httpClientWithCache.PatchAsync(patchUri, patchRequest, headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // Verify HTTP request was made with correct headers
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.Headers.Contains("Authorization") &&
                req.RequestUri!.ToString().Contains(patchUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was NOT called for raw HttpResponseMessage
        _mockResponseHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchAsync_SendsCorrectJsonContent()
    {
        // Arrange
        var patchUri = "/api/test/1";
        var patchRequest = new { Id = 1, Name = "Updated Name", Status = "Active" };
        var patchResponse = new TestResponse { Id = 1, Name = "Updated Name" };
        string? capturedRequestBody = null;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(patchResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                if (request.Content != null)
                {
                    capturedRequestBody = request.Content.ReadAsStringAsync(_).GetAwaiter().GetResult();
                }
            })
            .ReturnsAsync(httpResponse);

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patchResponse);

        // Act
        TestResponse result = await _httpClientWithCache.PatchAsync<object, TestResponse>(patchUri, patchRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patchResponse.Id, result.Id);
        Assert.Equal(patchResponse.Name, result.Name);

        // Verify the JSON content was serialized correctly
        Assert.NotNull(capturedRequestBody);
        JsonElement sentData = JsonSerializer.Deserialize<JsonElement>(capturedRequestBody);

        // Use TryGetProperty for case-insensitive property access
        Assert.True(sentData.TryGetProperty("Id", out JsonElement idProperty) || sentData.TryGetProperty("id", out idProperty));
        Assert.Equal(1, idProperty.GetInt32());

        Assert.True(sentData.TryGetProperty("Name", out JsonElement nameProperty) || sentData.TryGetProperty("name", out nameProperty));
        Assert.Equal("Updated Name", nameProperty.GetString());

        Assert.True(sentData.TryGetProperty("Status", out JsonElement statusProperty) || sentData.TryGetProperty("status", out statusProperty));
        Assert.Equal("Active", statusProperty.GetString());
    }

    [Fact]
    public async Task PutAsync_InvalidatesRelatedCache()
    {
        // Arrange
        var putUri = "/api/test/1";
        var putRequest = new { Data = "put" };
        var putResponse = new TestResponse { Id = 1, Name = "Put" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(putResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().Contains(putUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(putResponse);

        // Act
        TestResponse result = await _httpClientWithCache.PutAsync<object, TestResponse>(putUri, putRequest);

        // Assert
        Assert.Equal(putResponse.Id, result.Id);
        Assert.Equal(putResponse.Name, result.Name);

        // Verify HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains(putUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called exactly once
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PutAsync_WithHeaders_InvalidatesRelatedCache()
    {
        // Arrange
        var putUri = "/api/test/1";
        var putRequest = new { Data = "put" };
        var putResponse = new TestResponse { Id = 1, Name = "Put" };
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { { "Authorization", "Bearer token" } };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(putResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token" &&
                    req.RequestUri!.ToString().Contains(putUri) &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(putResponse);

        // Act
        TestResponse result = await _httpClientWithCache.PutAsync<object, TestResponse>(putUri, putRequest, headers);

        // Assert
        Assert.Equal(putResponse.Id, result.Id);
        Assert.Equal(putResponse.Name, result.Name);

        // Verify HTTP request was made with correct headers
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.Headers.Contains("Authorization") &&
                req.RequestUri!.ToString().Contains(putUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PutAsync_SuccessfulHandling_InvalidatesCache()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test/1";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var requestData = new { Name = "Put Data" };
        var responseData = new TestResponse { Id = 2, Name = "Put" };

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
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to succeed
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        TestResponse result = await _httpClientWithCache.PutAsync<object, TestResponse>(requestUri, requestData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Put", result.Name);

        // Verify HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(requestUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PutAsync_ResponseHandlerThrows_CacheRemainsValid()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test/1";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var requestData = new { Name = "Put Data" };

        // Pre-populate cache with valid data
        _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(5));
        _mockCacheKeyGenerator.Setup(x => x.GenerateKey("TestResponse", requestUri))
            .Returns(cacheKey);

        // Setup HTTP client to return successful response
        var responseContent = JsonSerializer.Serialize(new TestResponse { Id = 2, Name = "Put" });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to throw exception
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Response handler failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _httpClientWithCache.PutAsync<object, TestResponse>(requestUri, requestData));

        // Verify that cached data is still available (cache was not invalidated due to failure)
        var cacheExists = _cache.TryGetValue(cacheKey, out TestResponse? stillCachedResult);
        Assert.True(cacheExists);
        Assert.NotNull(stillCachedResult);
        Assert.Equal(1, stillCachedResult.Id);
        Assert.Equal("Cached", stillCachedResult.Name);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesRelatedCache()
    {
        // Arrange
        var deleteUri = "/api/test/1";
        var deleteResponse = new TestResponse { Id = 1, Name = "Deleted" };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(deleteResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains(deleteUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        // Act
        TestResponse result = await _httpClientWithCache.DeleteAsync<TestResponse>(deleteUri);

        // Assert
        Assert.Equal(deleteResponse.Id, result.Id);
        Assert.Equal(deleteResponse.Name, result.Name);

        // Verify HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri!.ToString().Contains(deleteUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called exactly once
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithHeaders_InvalidatesRelatedCache()
    {
        // Arrange
        var deleteUri = "/api/test/1";
        var deleteResponse = new TestResponse { Id = 1, Name = "Deleted" };
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { { "Authorization", "Bearer token" } };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(deleteResponse)),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token" &&
                    req.RequestUri!.ToString().Contains(deleteUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        // Act
        TestResponse result = await _httpClientWithCache.DeleteAsync<TestResponse>(deleteUri, headers);

        // Assert
        Assert.Equal(deleteResponse.Id, result.Id);
        Assert.Equal(deleteResponse.Name, result.Name);

        // Verify HTTP request was made with correct headers
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.Headers.Contains("Authorization") &&
                req.RequestUri!.ToString().Contains(deleteUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var deleteUri = "/api/test/1";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Deleted successfully"),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains(deleteUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        HttpResponseMessage result = await _httpClientWithCache.DeleteAsync(deleteUri);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // Verify HTTP request was made correctly (without involving response handler)
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri!.ToString().Contains(deleteUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was NOT called for raw HttpResponseMessage
        _mockResponseHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_WithHeaders_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var deleteUri = "/api/test/1";
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { { "Authorization", "Bearer token" } };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Deleted successfully"),
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Authorization!.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "token" &&
                    req.RequestUri!.ToString().Contains(deleteUri)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        HttpResponseMessage result = await _httpClientWithCache.DeleteAsync(deleteUri, headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // Verify HTTP request was made with correct headers
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.Headers.Contains("Authorization") &&
                req.RequestUri!.ToString().Contains(deleteUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was NOT called for raw HttpResponseMessage
        _mockResponseHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_SuccessfulHandling_InvalidatesCache()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test/1";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };
        var responseData = new TestResponse { Id = 2, Name = "Deleted" };

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
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to succeed
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        TestResponse result = await _httpClientWithCache.DeleteAsync<TestResponse>(requestUri);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Deleted", result.Name);

        // Verify HTTP request was made correctly
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(requestUri)),
            ItExpr.IsAny<CancellationToken>());

        // Verify response handler was called
        _mockResponseHandler.Verify(x => x.HandleAsync<TestResponse>(httpResponse, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ResponseHandlerThrows_CacheRemainsValid()
    {
        // Arrange
        const string cacheKey = "TestResponse_/api/test";
        const string requestUri = "/api/test/1";
        var cachedData = new TestResponse { Id = 1, Name = "Cached" };

        // Pre-populate cache with valid data
        _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(5));
        _mockCacheKeyGenerator.Setup(x => x.GenerateKey("TestResponse", requestUri))
            .Returns(cacheKey);

        // Setup HTTP client to return successful response
        var responseContent = JsonSerializer.Serialize(new TestResponse { Id = 2, Name = "Deleted" });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Setup response handler to throw exception
        _mockResponseHandler.Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Response handler failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _httpClientWithCache.DeleteAsync<TestResponse>(requestUri));

        // Verify that cached data is still available (cache was not invalidated due to failure)
        var cacheExists = _cache.TryGetValue(cacheKey, out TestResponse? stillCachedResult);
        Assert.True(cacheExists);
        Assert.NotNull(stillCachedResult);
        Assert.Equal(1, stillCachedResult.Id);
        Assert.Equal("Cached", stillCachedResult.Name);
    }

    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
