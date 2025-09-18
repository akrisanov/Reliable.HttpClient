using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching;

/// <summary>
/// Cached wrapper for HttpClient that caches responses
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public class CachedHttpClient<TResponse>(
    System.Net.Http.HttpClient httpClient,
    IHttpResponseCache<TResponse> cache,
    IOptionsSnapshot<HttpCacheOptions> options,
    ILogger<CachedHttpClient<TResponse>> logger)
{
    private readonly System.Net.Http.HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IHttpResponseCache<TResponse> _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly HttpCacheOptions _options = options?.Value ?? new HttpCacheOptions();
    private readonly ILogger<CachedHttpClient<TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Sends an HTTP request with caching support
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <param name="responseHandler">Function to convert HttpResponseMessage to TResponse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or fresh response</returns>
    public async Task<TResponse> SendAsync(
        HttpRequestMessage request,
        Func<HttpResponseMessage, Task<TResponse>> responseHandler,
        CancellationToken cancellationToken = default)
    {
        // Check if this request should be cached
        if (!ShouldCacheRequest(request))
        {
            _logger.LogDebug("Request not cacheable: {Method} {Uri}", request.Method, request.RequestUri);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await responseHandler(response).ConfigureAwait(false);
        }

        // Generate cache key
        var cacheKey = _options.KeyGenerator.GenerateKey(request);

        // Try to get from cache first
        TResponse? cachedResponse = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cachedResponse is not null)
        {
            _logger.LogDebug("Returning cached response for: {Method} {Uri}", request.Method, request.RequestUri);
            return cachedResponse;
        }

        // Execute request
        _logger.LogDebug("Cache miss, executing request: {Method} {Uri}", request.Method, request.RequestUri);
        HttpResponseMessage httpResponse = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        TResponse? result = await responseHandler(httpResponse).ConfigureAwait(false);

        // Cache the response if it should be cached
        if (ShouldCacheResponse(request, httpResponse))
        {
            TimeSpan expiry = _options.GetExpiry(request, httpResponse);
            await _cache.SetAsync(cacheKey, result, expiry, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cached response for: {Method} {Uri}, expiry: {Expiry}",
                request.Method, request.RequestUri, expiry);
        }

        return result;
    }

    /// <summary>
    /// GET request with JSON deserialization and caching
    /// </summary>
    public async Task<TResponse> GetFromJsonAsync(
        string requestUri,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        return await SendAsync(request, async response =>
        {
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TResponse>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize response");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears all cached responses
    /// </summary>
    public Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        return _cache.ClearAsync(cancellationToken);
    }

    /// <summary>
    /// Removes a specific cached response
    /// </summary>
    public Task RemoveFromCacheAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var cacheKey = _options.KeyGenerator.GenerateKey(request);
        return _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private bool ShouldCacheRequest(HttpRequestMessage request)
    {
        return _options.CacheableMethods.Contains(request.Method);
    }

    private bool ShouldCacheResponse(HttpRequestMessage request, HttpResponseMessage response)
    {
        return _options.CacheableStatusCodes.Contains(response.StatusCode)
               && _options.ShouldCache(request, response);
    }
}
