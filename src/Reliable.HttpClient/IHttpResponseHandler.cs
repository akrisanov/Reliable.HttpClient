namespace Reliable.HttpClient;

/// <summary>
/// Universal HTTP response handler without type constraints
/// </summary>
public interface IHttpResponseHandler
{
    /// <summary>
    /// Handles HTTP response and returns typed result
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="response">HTTP response to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed typed response</returns>
    /// <exception cref="HttpRequestException">On HTTP errors</exception>
    Task<TResponse> HandleAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for handling HTTP responses from external services
/// </summary>
/// <typeparam name="TResponse">Response type after processing</typeparam>
public interface IHttpResponseHandler<TResponse>
{
    /// <summary>
    /// Processes HTTP response and returns typed result
    /// </summary>
    /// <param name="response">HTTP response to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed typed response</returns>
    /// <exception cref="HttpRequestException">On HTTP errors</exception>
    Task<TResponse> HandleAsync(HttpResponseMessage response, CancellationToken cancellationToken = default);
}
