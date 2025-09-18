using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reliable.HttpClient;

/// <summary>
/// Default implementation of universal response handler
/// </summary>
/// <param name="jsonOptions">JSON serialization options</param>
/// <param name="logger">Logger instance</param>
public class DefaultHttpResponseHandler(
    IOptions<JsonSerializerOptions>? jsonOptions = null,
    ILogger<DefaultHttpResponseHandler>? logger = null) : IHttpResponseHandler
{
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions?.Value ?? new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILogger<DefaultHttpResponseHandler>? _logger = logger;

    /// <summary>
    /// Handles HTTP response and returns typed result
    /// </summary>
    /// <typeparam name="TResponse">Response type after deserialization</typeparam>
    /// <param name="response">HTTP response to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed typed response</returns>
    /// <exception cref="HttpRequestException">On HTTP errors or deserialization failures</exception>
    public virtual async Task<TResponse> HandleAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw new HttpRequestException("Empty response received");
            }

            TResponse? result = JsonSerializer.Deserialize<TResponse>(content, _jsonOptions) ??
                throw new HttpRequestException($"Failed to deserialize response to {typeof(TResponse).Name}");

            return result;
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error for type {Type}", typeof(TResponse).Name);
            throw new HttpRequestException($"Invalid JSON response", ex);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error handling response for type {Type}", typeof(TResponse).Name);
            throw new HttpRequestException($"Unexpected error handling response", ex);
        }
    }
}
