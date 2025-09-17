namespace Reliable.HttpClient;

/// <summary>
/// Base settings for HTTP clients with resilience policies
/// </summary>
public class HttpClientOptions
{
    /// <summary>
    /// Base API URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// User-Agent for HTTP requests
    /// </summary>
    public string UserAgent { get; set; } = "Reliable.HttpClient/1.1.0";

    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker policy configuration
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    public virtual void Validate()
    {
#pragma warning disable MA0015 // Specify the parameter name in ArgumentException
        if (TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be greater than 0", nameof(TimeoutSeconds));

        if (!string.IsNullOrEmpty(BaseUrl))
        {
            if (!Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute))
                throw new ArgumentException("BaseUrl must be a valid absolute URI when specified", nameof(BaseUrl));

            var uri = new Uri(BaseUrl);
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
                throw new ArgumentException("BaseUrl must use HTTP or HTTPS scheme", nameof(BaseUrl));
        }
        Retry.Validate();
        CircuitBreaker.Validate();
#pragma warning restore MA0015 // Specify the parameter name in ArgumentException
    }
}
