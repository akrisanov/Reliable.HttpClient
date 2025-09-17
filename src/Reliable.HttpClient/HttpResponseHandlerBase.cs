using System.Net;

using Microsoft.Extensions.Logging;

namespace Reliable.HttpClient;

/// <summary>
/// Base class for handling HTTP responses from external services
/// </summary>
/// <typeparam name="TResponse">Response type after processing</typeparam>
public abstract class HttpResponseHandlerBase<TResponse>(ILogger logger) : IHttpResponseHandler<TResponse>
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Processes HTTP response and returns typed result
    /// </summary>
    /// <param name="response">HTTP response to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed typed response</returns>
    /// <exception cref="HttpRequestException">On HTTP errors</exception>
    public abstract Task<TResponse> HandleAsync(HttpResponseMessage response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads HTTP response content as string
    /// </summary>
    /// <param name="response">HTTP response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response content as string</returns>
    protected async Task<string> ReadResponseContentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read HTTP response content");
            return string.Empty;
        }
    }

    /// <summary>
    /// Logs information about HTTP response
    /// </summary>
    /// <param name="response">HTTP response</param>
    /// <param name="content">Response content (optional)</param>
    /// <param name="serviceName">Service name for logging</param>
    protected void LogHttpResponse(
        HttpResponseMessage response,
        string? content = null,
        string serviceName = "ExternalService")
    {
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("{ServiceName} HTTP request successful: {StatusCode}",
                serviceName, statusCode);
        }
        else
        {
            var errorMessage = $"HTTP {statusCode}: {response.ReasonPhrase}";
            if (!string.IsNullOrWhiteSpace(content))
            {
                errorMessage += $". Content: {content}";
            }

            _logger.LogError("{ServiceName} HTTP error: {ErrorMessage}",
                serviceName, errorMessage);
        }
    }

    /// <summary>
    /// Checks if status code indicates success
    /// </summary>
    /// <param name="response">HTTP response</param>
    /// <returns>true if response is successful</returns>
    protected static bool IsSuccessStatusCode(HttpResponseMessage response)
    {
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets HTTP status code description
    /// </summary>
    /// <param name="statusCode">Status code</param>
    /// <returns>Status code description</returns>
    protected static string GetStatusCodeDescription(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication error",
            HttpStatusCode.Forbidden => "Access forbidden",
            HttpStatusCode.NotFound => "Resource not found",
            HttpStatusCode.TooManyRequests => "Rate limit exceeded",
            HttpStatusCode.InternalServerError => "Internal server error",
            HttpStatusCode.BadGateway => "Bad gateway",
            HttpStatusCode.ServiceUnavailable => "Service unavailable",
            HttpStatusCode.GatewayTimeout => "Gateway timeout",
            _ => $"HTTP {(int)statusCode}: {statusCode}"
        };
    }
}
