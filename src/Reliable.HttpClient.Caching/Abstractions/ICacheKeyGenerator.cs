namespace Reliable.HttpClient.Caching.Abstractions;

/// <summary>
/// Generates cache keys for HTTP requests
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for the given HTTP request
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <returns>Cache key</returns>
    string GenerateKey(HttpRequestMessage request);
}
