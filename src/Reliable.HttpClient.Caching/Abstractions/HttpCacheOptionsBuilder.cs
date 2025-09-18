using System.Net;

namespace Reliable.HttpClient.Caching.Abstractions;

/// <summary>
/// Builder for configuring HTTP cache options
/// </summary>
public sealed class HttpCacheOptionsBuilder
{
    private readonly HttpCacheOptions _options = new();

    /// <summary>
    /// Sets the default cache expiry time
    /// </summary>
    /// <param name="defaultExpiry">Default cache expiry time</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expiry time is not positive</exception>
    public HttpCacheOptionsBuilder WithDefaultExpiry(TimeSpan defaultExpiry)
    {
        if (defaultExpiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultExpiry),
                defaultExpiry,
                $"{nameof(defaultExpiry)} ('{defaultExpiry}') must be greater than '{TimeSpan.Zero}'.");
        }

        _options.DefaultExpiry = defaultExpiry;
        return this;
    }

    /// <summary>
    /// Sets the maximum cache size
    /// </summary>
    /// <param name="maxCacheSize">Maximum number of entries to store in cache</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when cache size is negative</exception>
    public HttpCacheOptionsBuilder WithMaxCacheSize(int maxCacheSize)
    {
        if (maxCacheSize < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCacheSize),
                maxCacheSize,
                $"{nameof(maxCacheSize)} ('{maxCacheSize}') must be a non-negative value.");
        }

        _options.MaxCacheSize = maxCacheSize;
        return this;
    }

    /// <summary>
    /// Adds a default header to all requests
    /// </summary>
    /// <param name="name">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when header name is null or whitespace</exception>
    /// <exception cref="ArgumentNullException">Thrown when header value is null</exception>
    public HttpCacheOptionsBuilder AddHeader(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
        }
        ArgumentNullException.ThrowIfNull(value);

        _options.DefaultHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple default headers to all requests
    /// </summary>
    /// <param name="headers">Dictionary of headers to add</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when headers dictionary is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder AddHeaders(IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (KeyValuePair<string, string> header in headers)
        {
            _options.DefaultHeaders[header.Key] = header.Value;
        }
        return this;
    }

    /// <summary>
    /// Removes a default header
    /// </summary>
    /// <param name="name">Header name to remove</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when header name is null or whitespace</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder RemoveHeader(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

        _options.DefaultHeaders.Remove(name);
        return this;
    }

    /// <summary>
    /// Clears all default headers
    /// </summary>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder ClearHeaders()
    {
        _options.DefaultHeaders.Clear();
        return this;
    }

    /// <summary>
    /// Sets custom cache key generator
    /// </summary>
    /// <param name="keyGenerator">Cache key generator</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when key generator is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder WithKeyGenerator(ICacheKeyGenerator keyGenerator)
    {
        ArgumentNullException.ThrowIfNull(keyGenerator);

        _options.KeyGenerator = keyGenerator;
        return this;
    }

    /// <summary>
    /// Adds cacheable status code
    /// </summary>
    /// <param name="statusCode">HTTP status code to cache</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder AddCacheableStatusCode(HttpStatusCode statusCode)
    {
        _options.CacheableStatusCodes.Add(statusCode);
        return this;
    }

    /// <summary>
    /// Adds cacheable HTTP method
    /// </summary>
    /// <param name="method">HTTP method to cache</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when HTTP method is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder AddCacheableMethod(HttpMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);

        _options.CacheableMethods.Add(method);
        return this;
    }

    /// <summary>
    /// Sets custom should cache predicate
    /// </summary>
    /// <param name="shouldCache">Predicate to determine if response should be cached</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder WithShouldCache(Func<HttpRequestMessage, HttpResponseMessage, bool> shouldCache)
    {
        ArgumentNullException.ThrowIfNull(shouldCache);

        _options.ShouldCache = shouldCache;
        return this;
    }

    /// <summary>
    /// Sets custom expiry getter
    /// </summary>
    /// <param name="getExpiry">Function to get expiry time for specific request/response pair</param>
    /// <returns>Builder instance for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when function is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when builder has already been built</exception>
    public HttpCacheOptionsBuilder WithGetExpiry(Func<HttpRequestMessage, HttpResponseMessage, TimeSpan> getExpiry)
    {
        ArgumentNullException.ThrowIfNull(getExpiry);

        _options.GetExpiry = getExpiry;
        return this;
    }

    /// <summary>
    /// Builds the HTTP cache options
    /// </summary>
    /// <returns>Configured HTTP cache options</returns>
    public HttpCacheOptions Build()
    {
        // Create a deep copy to prevent external mutations
        var result = new HttpCacheOptions
        {
            DefaultExpiry = _options.DefaultExpiry,
            MaxCacheSize = _options.MaxCacheSize,
            KeyGenerator = _options.KeyGenerator,
            DefaultHeaders = new Dictionary<string, string>(_options.DefaultHeaders, StringComparer.OrdinalIgnoreCase),
            CacheableStatusCodes = new HashSet<HttpStatusCode>(_options.CacheableStatusCodes),
            CacheableMethods = new HashSet<HttpMethod>(_options.CacheableMethods),
            ShouldCache = _options.ShouldCache,
            GetExpiry = _options.GetExpiry,
        };

        return result;
    }

    /// <summary>
    /// Implicit conversion to HttpCacheOptions
    /// </summary>
    /// <param name="builder">The builder instance</param>
    /// <returns>Built HTTP cache options</returns>
    public static implicit operator HttpCacheOptions(HttpCacheOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Build();
    }
}
