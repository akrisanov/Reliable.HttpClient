using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching.Providers;

/// <summary>
/// Memory cache provider for HTTP responses
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <param name="memoryCache">Memory cache instance</param>
/// <param name="logger">Logger instance</param>
public class MemoryCacheProvider<TResponse>(
    IMemoryCache memoryCache,
    ILogger<MemoryCacheProvider<TResponse>> logger) : IHttpResponseCache<TResponse>
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<MemoryCacheProvider<TResponse>> _logger = logger;
    private readonly string _keyPrefix = $"http_cache_{typeof(TResponse).Name}_";
    private readonly ConcurrentBag<string> _cacheKeys = [];

    public Task<TResponse?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var fullKey = _keyPrefix + key;

        if (_memoryCache.TryGetValue(fullKey, out var cachedValue) && cachedValue is TResponse response)
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return Task.FromResult<TResponse?>(response);
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult<TResponse?>(default);
    }

    public Task SetAsync(string key, TResponse value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var fullKey = _keyPrefix + key;
        var options = new MemoryCacheEntryOptions();

        if (expiry.HasValue)
        {
            // Memory cache requires positive expiry values
            if (expiry.Value <= TimeSpan.Zero)
            {
                _logger.LogDebug("Skipping cache set for key: {Key} due to non-positive expiry: {Expiry}", key, expiry);
                return Task.CompletedTask;
            }

            options.SetAbsoluteExpiration(expiry.Value);
        }

        _memoryCache.Set(fullKey, value, options);
        _cacheKeys.Add(fullKey);

        _logger.LogDebug("Cached response for key: {Key}, expiry: {Expiry}", key, expiry);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var fullKey = _keyPrefix + key;
        _memoryCache.Remove(fullKey);

        _logger.LogDebug("Removed cached response for key: {Key}", key);

        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // Remove all tracked keys
        foreach (var key in _cacheKeys)
        {
            _memoryCache.Remove(key);
        }

        // Clear the tracking collection
        while (_cacheKeys.TryTake(out _)) { }

        _logger.LogDebug("Cleared all cached responses");
        return Task.CompletedTask;
    }
}
