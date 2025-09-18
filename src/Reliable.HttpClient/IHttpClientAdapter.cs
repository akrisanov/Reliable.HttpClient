namespace Reliable.HttpClient;

/// <summary>
/// Universal HTTP client interface that can be implemented by both regular HttpClient and cached HttpClient
/// </summary>
public interface IHttpClientAdapter
{
    /// <summary>
    /// Performs GET request
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs GET request
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> GetAsync<TResponse>(
        Uri requestUri,
        CancellationToken cancellationToken = default) where TResponse : class;

    /// <summary>
    /// Performs POST request
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
    /// Performs POST request
    /// </summary>
    /// <typeparam name="TRequest">Request content type</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="content">Request content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> PostAsync<TRequest>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs PUT request
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
    /// Performs DELETE request
    /// </summary>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs DELETE request with typed response
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="requestUri">Request URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed response</returns>
    Task<TResponse> DeleteAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class;
}
