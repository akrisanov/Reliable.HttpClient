using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching;

/// <summary>
/// Predefined cache configuration presets for common scenarios
/// </summary>
public static class CachePresets
{
    /// <summary>
    /// Short-term caching for frequently changing data (1 minute)
    /// </summary>
    public static HttpCacheOptions ShortTerm => new()
    {
        DefaultExpiry = TimeSpan.FromMinutes(1),
        MaxCacheSize = 500,
        ShouldCache = (request, response) =>
            response.IsSuccessStatusCode &&
            response.Content.Headers.ContentLength < 100_000, // Don't cache large responses
    };

    /// <summary>
    /// Medium-term caching for moderately stable data (10 minutes)
    /// </summary>
    public static HttpCacheOptions MediumTerm => new()
    {
        DefaultExpiry = TimeSpan.FromMinutes(10),
        MaxCacheSize = 1_000,
        ShouldCache = (request, response) =>
            response.IsSuccessStatusCode &&
            response.Content.Headers.ContentLength < 500_000,
    };

    /// <summary>
    /// Long-term caching for stable data (1 hour)
    /// </summary>
    public static HttpCacheOptions LongTerm => new()
    {
        DefaultExpiry = TimeSpan.FromHours(1),
        MaxCacheSize = 2_000,
        ShouldCache = (request, response) =>
            response.IsSuccessStatusCode &&
            response.Content.Headers.ContentLength < 1_000_000,
    };

    /// <summary>
    /// High-performance caching for API responses (5 minutes, larger cache)
    /// </summary>
    public static HttpCacheOptions HighPerformance => new()
    {
        DefaultExpiry = TimeSpan.FromMinutes(5),
        MaxCacheSize = 5_000,
        ShouldCache = (request, response) =>
            response.IsSuccessStatusCode &&
            response.Content.Headers.ContentLength < 200_000,
    };

    /// <summary>
    /// Configuration data caching for rarely changing data (30 minutes)
    /// </summary>
    public static HttpCacheOptions Configuration => new()
    {
        DefaultExpiry = TimeSpan.FromMinutes(30),
        MaxCacheSize = 100,
        ShouldCache = (request, response) =>
            response.IsSuccessStatusCode, // Cache all successful config responses
    };

    /// <summary>
    /// File download caching with no size limit (2 hours)
    /// </summary>
    public static HttpCacheOptions FileDownload => new()
    {
        DefaultExpiry = TimeSpan.FromHours(2),
        MaxCacheSize = 50, // Fewer items but potentially larger
        ShouldCache = (request, response) =>
            response.IsSuccessStatusCode &&
            (response.Content.Headers.ContentType?.MediaType?.StartsWith("application/", StringComparison.OrdinalIgnoreCase) == true ||
             response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true),
    };
}
