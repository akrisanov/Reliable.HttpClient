namespace Reliable.HttpClient.Caching.Abstractions;

/// <summary>
/// Configuration options for HTTP response caching
/// </summary>
public class HttpCacheOptions
{
    /// <summary>
    /// Default expiry time for cached responses
    /// </summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of items to cache (for memory cache). Helps prevent memory leaks.
    /// </summary>
    public int? MaxCacheSize { get; set; } = 1_000;

    /// <summary>
    /// Custom cache key generator
    /// </summary>
    public ICacheKeyGenerator KeyGenerator { get; set; } = new DefaultCacheKeyGenerator();

    /// <summary>
    /// HTTP status codes that should be cached (idempotent responses only)
    /// </summary>
    public ISet<System.Net.HttpStatusCode> CacheableStatusCodes { get; set; } =
        new HashSet<System.Net.HttpStatusCode>
        {
            System.Net.HttpStatusCode.OK,              // 200 - Standard success
            System.Net.HttpStatusCode.NotModified,     // 304 - Not modified
            System.Net.HttpStatusCode.PartialContent,  // 206 - Partial content
        };

    /// <summary>
    /// HTTP methods that should be cached
    /// </summary>
    public ISet<HttpMethod> CacheableMethods { get; set; } =
        new HashSet<HttpMethod>
        {
            HttpMethod.Get,
            HttpMethod.Head,
        };

    /// <summary>
    /// Determines if a response should be cached based on the request and response
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage, bool> ShouldCache { get; set; } =
        (request, response) =>
        {
            // Check Cache-Control directives
            return response.Headers.CacheControl is not { NoCache: true } and not { NoStore: true };
        };

    /// <summary>
    /// Gets the expiry time for a specific request/response pair
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage, TimeSpan> GetExpiry { get; set; } =
        (request, response) =>
        {
            // Check Cache-Control max-age directive
            if (response.Headers.CacheControl?.MaxAge is { } maxAge)
            {
                return maxAge;
            }

            // Check Cache-Control no-cache or no-store directives
            if (response.Headers.CacheControl is { NoCache: true } or { NoStore: true })
            {
                return TimeSpan.Zero;
            }

            // This will be overridden to use the correct DefaultExpiry by CopyPresetToOptions
            return TimeSpan.FromMinutes(5);
        };
}
