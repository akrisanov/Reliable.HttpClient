namespace Reliable.HttpClient.Caching;

/// <summary>
/// Simple cache key generator for universal caching
/// </summary>
public interface ISimpleCacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for the given type and URI
    /// </summary>
    /// <param name="typeName">Response type name</param>
    /// <param name="requestUri">Request URI</param>
    /// <returns>Cache key</returns>
    string GenerateKey(string typeName, string requestUri);
}
