using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace Reliable.HttpClient;

/// <summary>
/// HTTP response handler for JSON deserialization
/// </summary>
/// <typeparam name="TResponse">Target type for JSON deserialization</typeparam>
public class JsonResponseHandler<TResponse>(ILogger<JsonResponseHandler<TResponse>> logger)
    : HttpResponseHandlerBase<TResponse>(logger)
{
    private static readonly JsonSerializerOptions s_defaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Processes HTTP response and deserializes JSON to typed result
    /// </summary>
    /// <param name="response">HTTP response to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized typed response</returns>
    /// <exception cref="HttpRequestException">On HTTP errors or JSON deserialization failures</exception>
    public override async Task<TResponse> HandleAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var content = await ReadResponseContentAsync(response, cancellationToken).ConfigureAwait(false);

        LogHttpResponse(response, content, "JsonResponseHandler");

        if (!IsSuccessStatusCode(response))
        {
            var statusDescription = GetStatusCodeDescription(response.StatusCode);
            throw new HttpRequestException($"HTTP request failed: {statusDescription}", inner: null, response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new HttpRequestException("Response content is empty or null");
        }

        try
        {
            TResponse? result = JsonSerializer.Deserialize<TResponse>(content, s_defaultJsonOptions) ??
                throw new HttpRequestException("JSON deserialization returned null");

            return result;
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException($"Failed to deserialize JSON response: {ex.Message}", ex);
        }
    }
}
