namespace Reliable.HttpClient.Caching;

/// <summary>
/// Universal HTTP client with caching, not tied to specific types
/// </summary>
public interface IHttpClientWithCache
{
    /// <summary>
    /// Performs GET request with caching support
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cacheDuration">Cache duration (optional, uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response from cache or HTTP request</returns>
    Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs GET request with caching support
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cacheDuration">Cache duration (optional, uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response from cache or HTTP request</returns>
    Task<TResponse> GetAsync<TResponse>(
        Uri requestUri,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs POST request (not cached, invalidates related cache entries)
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs POST request (not cached, invalidates related cache entries)
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> PostAsync<TRequest, TResponse>(
        Uri requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs PUT request (not cached, invalidates related cache entries)
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs DELETE request (not cached, invalidates related cache entries)
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> DeleteAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Invalidates cache entries matching the specified pattern
    /// </summary>
    /// <param name="pattern">Cache key pattern to match</param>
    /// <returns>Task representing the async operation</returns>
    Task InvalidateCacheAsync(string pattern);

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ClearCacheAsync();
}
