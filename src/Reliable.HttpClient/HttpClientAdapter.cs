using System.Net.Http.Json;

namespace Reliable.HttpClient;

/// <summary>
/// Adapter for System.Net.HttpClient to implement IHttpClientAdapter interface
/// </summary>
public class HttpClientAdapter(System.Net.Http.HttpClient httpClient, IHttpResponseHandler responseHandler) : IHttpClientAdapter
{
    private readonly System.Net.Http.HttpClient _httpClient = httpClient;
    private readonly IHttpResponseHandler _responseHandler = responseHandler;

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        IDictionary<string, string>? headers,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        if (headers is null or { Count: 0 })
        {
            return await GetAsync<TResponse>(requestUri, cancellationToken).ConfigureAwait(false);
        }

        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Get, requestUri, content: null, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(
        Uri requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> GetAsync<TResponse>(
        Uri requestUri,
        IDictionary<string, string>? headers,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        if (headers is null or { Count: 0 })
        {
            return await GetAsync<TResponse>(requestUri, cancellationToken).ConfigureAwait(false);
        }

        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Get, requestUri, content: null, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            requestUri, content, cancellationToken).ConfigureAwait(false);

        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Post, requestUri, JsonContent.Create(content), headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PostAsync<TRequest>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.PostAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PostAsync<TRequest>(
        string requestUri,
        TRequest content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            HttpMethod.Post, requestUri, JsonContent.Create(content), headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Patch, requestUri, JsonContent.Create(content), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Patch, requestUri, JsonContent.Create(content), headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PatchAsync<TRequest>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            HttpMethod.Patch, requestUri, JsonContent.Create(content), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PatchAsync<TRequest>(
        string requestUri,
        TRequest content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            HttpMethod.Patch, requestUri, JsonContent.Create(content), headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Put, requestUri, JsonContent.Create(content), headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Delete, requestUri, content: null, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> DeleteAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> DeleteAsync<TResponse>(
        string requestUri,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        return await SendWithResponseHandlerAsync<TResponse>(
            HttpMethod.Delete, requestUri, content: null, headers, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends HTTP request with response handler for typed responses
    /// </summary>
    private async Task<TResponse> SendWithResponseHandlerAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken) where TResponse : class
    {
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        AddHeaders(request, headers);

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends HTTP request with response handler for typed responses
    /// </summary>
    private async Task<TResponse> SendWithResponseHandlerAsync<TResponse>(
        HttpMethod method,
        Uri requestUri,
        HttpContent? content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken) where TResponse : class
    {
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        AddHeaders(request, headers);

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends HTTP request with response handler for typed responses
    /// </summary>
    private async Task<TResponse> SendWithResponseHandlerAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken) where TResponse : class
    {
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await _responseHandler.HandleAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends HTTP request returning raw HttpResponseMessage
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        AddHeaders(request, headers);

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends HTTP request returning raw HttpResponseMessage
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds custom headers to the HTTP request
    /// </summary>
    /// <param name="request">HTTP request message</param>
    /// <param name="headers">Headers to add</param>
    private static void AddHeaders(HttpRequestMessage request, IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(headers);

        foreach ((var key, var value) in headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }
}
