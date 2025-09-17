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
        _options.UserAgent = userAgent;
        return this;
    }

    /// <summary>
    /// Configures retry policy
    /// </summary>
    /// <param name="configure">Retry configuration action</param>
    /// <returns>Builder for method chaining</returns>
    public HttpClientOptionsBuilder WithRetry(Action<RetryOptionsBuilder> configure)
    {
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
