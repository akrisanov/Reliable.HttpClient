namespace Reliable.HttpClient;

/// <summary>
/// Builder for fluent configuration of HTTP client options
/// </summary>
public class HttpClientOptionsBuilder
{
    private readonly HttpClientOptions _options = new();

    /// <summary>
    /// Sets the base URL for HTTP requests
    /// </summary>
    /// <param name="baseUrl">Base URL</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or whitespace", nameof(baseUrl));

        _options.BaseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Sets the request timeout
    /// </summary>
    /// <param name="timeout">Timeout duration</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive");

        _options.TimeoutSeconds = (int)timeout.TotalSeconds;
        return this;
    }

    /// <summary>
    /// Sets the User-Agent header
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent cannot be null or whitespace", nameof(userAgent));

        _options.UserAgent = userAgent;
        return this;
    }

    /// <summary>
    /// Adds a default header that will be included in all requests
    /// </summary>
    /// <param name="name">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithHeader(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Header name cannot be null or whitespace", nameof(name));
        ArgumentNullException.ThrowIfNull(value);

        _options.DefaultHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple default headers that will be included in all requests
    /// </summary>
    /// <param name="headers">Headers to add</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithHeaders(IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (KeyValuePair<string, string> header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
                throw new ArgumentException("Header name cannot be null or whitespace", nameof(headers));
            ArgumentNullException.ThrowIfNull(header.Value, nameof(headers));

            _options.DefaultHeaders[header.Key] = header.Value;
        }
        return this;
    }

    /// <summary>
    /// Removes a default header
    /// </summary>
    /// <param name="name">Header name to remove</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithoutHeader(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Header name cannot be null or whitespace", nameof(name));

        _options.DefaultHeaders.Remove(name);
        return this;
    }

    /// <summary>
    /// Clears all default headers
    /// </summary>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithoutHeaders()
    {
        _options.DefaultHeaders.Clear();
        return this;
    }

    /// <summary>
    /// Configures retry policy
    /// </summary>
    /// <param name="configure">Retry configuration action</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithRetry(Action<RetryOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RetryOptionsBuilder(_options.Retry);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Configures circuit breaker policy
    /// </summary>
    /// <param name="configure">Circuit breaker configuration action</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithCircuitBreaker(Action<CircuitBreakerOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CircuitBreakerOptionsBuilder(_options.CircuitBreaker);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Disables circuit breaker
    /// </summary>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithoutCircuitBreaker()
    {
        _options.CircuitBreaker.Enabled = false;
        return this;
    }

    /// <summary>
    /// Builds the HTTP client options
    /// </summary>
    /// <returns>Configured options</returns>
    public HttpClientOptions Build()
    {
        _options.Validate();
        return _options;
    }

    /// <summary>
    /// Implicitly converts builder to options
    /// </summary>
    public static implicit operator HttpClientOptions(HttpClientOptionsBuilder builder) => builder.Build();
}
