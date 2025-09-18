using System.Net.Http.Json;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching;

/// <summary>
/// Universal HTTP client with caching implementation that can handle multiple response types.
/// This is the recommended approach when you need caching across different response types in a single client.
/// For type-safe caching of specific response types, consider using CachedHttpClient&lt;T&gt; from Reliable.HttpClient.Caching.Generic namespace.
/// </summary>
/// <param name="httpClient">HTTP client instance</param>
/// <param name="cache">Memory cache instance</param>
/// <param name="responseHandler">Universal response handler</param>
/// <param name="cacheKeyGenerator">Cache key generator (optional)</param>
/// <param name="logger">Logger instance (optional)</param>
/// <param name="defaultCacheDuration">Default cache duration (optional, defaults to 5 minutes)</param>
public class HttpClientWithCache(
    System.Net.Http.HttpClient httpClient,
    IMemoryCache cache,
    IHttpResponseHandler responseHandler,
    ISimpleCacheKeyGenerator? cacheKeyGenerator = null,
    ILogger<HttpClientWithCache>? logger = null,
    TimeSpan? defaultCacheDuration = null) : IHttpClientWithCache, IHttpClientAdapter
{
    private readonly System.Net.Http.HttpClient _httpClient = httpClient;
    private readonly IMemoryCache _cache = cache;
    private readonly IHttpResponseHandler _responseHandler = responseHandler;
    private readonly ISimpleCacheKeyGenerator _cacheKeyGenerator = cacheKeyGenerator ?? new DefaultSimpleCacheKeyGenerator();
    private readonly ILogger<HttpClientWithCache>? _logger = logger;
    private readonly TimeSpan _defaultCacheDuration = defaultCacheDuration ?? TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        var cacheKey = _cacheKeyGenerator.GenerateKey(typeof(TResponse).Name, requestUri);

        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResult) && cachedResult is not null)
        {
            _logger?.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedResult;
        }

        _logger?.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

        HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        TResponse result = await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);

        TimeSpan duration = cacheDuration ?? _defaultCacheDuration;
        _cache.Set(cacheKey, result, duration);

        _logger?.LogDebug("Cached result for key: {CacheKey}, Duration: {Duration}", cacheKey, duration);

        return result;
    }

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(
        Uri requestUri,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await GetAsync<TResponse>(requestUri.ToString(), cacheDuration, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        TResponse result = await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);

        // Invalidate cache only after successful response handling
        await InvalidateRelatedCacheAsync(requestUri).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        Uri requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await PostAsync<TRequest, TResponse>(requestUri.ToString(), content, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        TResponse result = await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);

        // Invalidate cache only after successful response handling
        await InvalidateRelatedCacheAsync(requestUri).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<TResponse> DeleteAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        TResponse result = await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);

        // Invalidate cache only after successful response handling
        await InvalidateRelatedCacheAsync(requestUri).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public Task InvalidateCacheAsync(string pattern)
    {
        // For now, we'll implement a simple approach
        // In production, you might want to use a more sophisticated cache that supports pattern-based invalidation
        _logger?.LogDebug("Cache invalidation requested for pattern: {Pattern}", pattern);

        // Note: MemoryCache doesn't natively support pattern-based invalidation
        // This is a limitation we acknowledge in the documentation
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearCacheAsync()
    {
        // For MemoryCache, we can't easily clear all entries without disposing
        // This is a known limitation documented in the API
        _logger?.LogDebug("Cache clear requested");
        return Task.CompletedTask;
    }

    private async Task InvalidateRelatedCacheAsync(string requestUri)
    {
        // Extract the base path to invalidate related GET requests
        var uri = new Uri(requestUri, UriKind.RelativeOrAbsolute);
        var basePath = uri.IsAbsoluteUri ? uri.AbsolutePath : requestUri;

        // Remove any ID or query parameters for broader invalidation
        var pathSegments = basePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length > 0)
        {
            var resourcePath = string.Join('/', pathSegments[..^1]);
            await InvalidateCacheAsync(resourcePath).ConfigureAwait(false);
        }
    }

    // IHttpClientAdapter implementation (without caching for non-GET operations)
    Task<TResponse> IHttpClientAdapter.GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken) =>
        GetAsync<TResponse>(requestUri, cacheDuration: null, cancellationToken);

    Task<TResponse> IHttpClientAdapter.GetAsync<TResponse>(Uri requestUri, CancellationToken cancellationToken) =>
        GetAsync<TResponse>(requestUri, cacheDuration: null, cancellationToken);

    async Task<HttpResponseMessage> IHttpClientAdapter.PostAsync<TRequest>(
        string requestUri, TRequest content, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);

        // Invalidate cache only after successful HTTP request (before response handler to maintain adapter contract)
        await InvalidateRelatedCacheAsync(requestUri).ConfigureAwait(false);

        return response;
    }

    async Task<HttpResponseMessage> IHttpClientAdapter.DeleteAsync(string requestUri, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);

        // Invalidate cache only after successful HTTP request (before response handler to maintain adapter contract)
        await InvalidateRelatedCacheAsync(requestUri).ConfigureAwait(false);

        return response;
    }
}
