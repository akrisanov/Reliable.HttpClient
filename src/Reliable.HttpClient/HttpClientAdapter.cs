using System.Net.Http.Json;

namespace Reliable.HttpClient;

/// <summary>
/// Adapter for System.Net.HttpClient to implement IHttpClientAdapter interface
/// </summary>
public class HttpClientAdapter(System.Net.Http.HttpClient httpClient, IHttpResponseHandler responseHandler) : IHttpClientAdapter
{
    private readonly System.Net.Http.HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly IHttpResponseHandler _responseHandler = responseHandler ?? throw new ArgumentNullException(nameof(responseHandler));

    public async Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> GetAsync<TResponse>(
        Uri requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PostAsync<TRequest>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> DeleteAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }
}
