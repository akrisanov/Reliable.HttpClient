namespace Reliable.HttpClient.Caching.Abstractions;

/// <summary>
/// Default cache key generator implementation
/// </summary>
internal class DefaultSimpleCacheKeyGenerator : ISimpleCacheKeyGenerator
{
    /// <inheritdoc />
    public string GenerateKey(string typeName, string requestUri)
    {
        return $"http_cache:{typeName}:{requestUri}";
    }
}
