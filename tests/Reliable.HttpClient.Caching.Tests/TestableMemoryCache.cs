using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Reliable.HttpClient.Caching.Tests;

/// <summary>
/// Testable memory cache wrapper that allows access to keys
/// </summary>
public interface ITestableMemoryCache : IMemoryCache
{
    /// <summary>
    /// Gets all cache keys
    /// </summary>
    IEnumerable<object> Keys { get; }

    /// <summary>
    /// Removes entries matching the pattern
    /// </summary>
    void RemoveByPattern(string pattern);

    /// <summary>
    /// Clears all entries
    /// </summary>
    void Clear();
}

/// <summary>
/// Testable memory cache implementation
/// </summary>
public class TestableMemoryCache : ITestableMemoryCache
{
    private readonly Dictionary<object, object> _cache = [];
    private readonly Dictionary<object, DateTimeOffset> _expirations = [];

    public IEnumerable<object> Keys => [.. _cache.Keys];

    public ICacheEntry CreateEntry(object key)
    {
        return new TestableCacheEntry(key, this);
    }

    public void Dispose()
    {
        _cache.Clear();
        _expirations.Clear();
        GC.SuppressFinalize(this);
    }

    public void Remove(object key)
    {
        _cache.Remove(key);
        _expirations.Remove(key);
    }

    public bool TryGetValue(object key, out object? value)
    {
        // Check if expired
        if (_expirations.TryGetValue(key, out DateTimeOffset expiration) && DateTimeOffset.UtcNow > expiration)
        {
            Remove(key);
            value = null;
            return false;
        }

        return _cache.TryGetValue(key, out value);
    }

    public void RemoveByPattern(string pattern)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.ToString()?.Contains(pattern, StringComparison.Ordinal) == true)
            .ToList();

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }
    }

    public void Clear()
    {
        _cache.Clear();
        _expirations.Clear();
    }

    internal void Set(object key, object value, DateTimeOffset? expiration = null)
    {
        _cache[key] = value;
        if (expiration.HasValue)
        {
            _expirations[key] = expiration.Value;
        }
    }
}

/// <summary>
/// Testable cache entry implementation
/// </summary>
internal class TestableCacheEntry : ICacheEntry
{
    private readonly TestableMemoryCache _cache;

    public TestableCacheEntry(object key, TestableMemoryCache cache)
    {
        Key = key;
        _cache = cache;
    }

    public object Key { get; }
    public object? Value { get; set; }
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public IList<IChangeToken> ExpirationTokens { get; } = [];
    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = [];
    public CacheItemPriority Priority { get; set; }
    public long? Size { get; set; }

    public void Dispose()
    {
        DateTimeOffset? expiration = AbsoluteExpiration;
        if (AbsoluteExpirationRelativeToNow.HasValue)
        {
            expiration = DateTimeOffset.UtcNow.Add(AbsoluteExpirationRelativeToNow.Value);
        }

        if (Value is not null)
        {
            _cache.Set(Key, Value, expiration);
        }
    }
}
