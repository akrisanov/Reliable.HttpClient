using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Reliable.HttpClient.Caching.Abstractions;

/// <summary>
/// Default cache key generator that uses method + URI + headers + body
/// </summary>
public class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    public string GenerateKey(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = request.Method.Method;
        var uri = request.RequestUri?.ToString() ?? string.Empty;

        // Include authorization context to prevent cross-user data leaks
        // Use cryptographically secure hash instead of GetHashCode()
        var authContext = request.Headers.Authorization?.Parameter is not null
            ? $"auth|{ComputeSecureHash(request.Headers.Authorization.Parameter)}"
            : "public";

        var parts = new List<string> { method, uri, authContext };

        // Include significant headers
        if (request.Headers.Any())
        {
            var headerParts = new List<string>();
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                // Skip authorization header as it's already included
                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Use pipe separator to avoid conflicts with colons in values
                headerParts.Add($"{header.Key}|{string.Join(',', header.Value)}");
            }

            if (headerParts.Count > 0)
            {
                headerParts.Sort(StringComparer.Ordinal); // Ensure consistent ordering
                parts.Add($"headers|{string.Join('|', headerParts)}");
            }
        }

        // Include request body for non-GET requests
        if (request.Content is not null && request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            try
            {
                // Clone content to avoid modifying original request
                var bodyContent = CloneAndReadContent(request.Content);
                if (!string.IsNullOrEmpty(bodyContent))
                {
                    var bodyHash = ComputeSecureHash(bodyContent);
                    parts.Add($"body|{bodyHash}");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ObjectDisposedException)
            {
                // If we can't read the body, use content type and length as fallback
                var contentType = request.Content.Headers.ContentType?.ToString() ?? "unknown";
                var contentLength = request.Content.Headers.ContentLength?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
                parts.Add($"body-meta|{contentType}|{contentLength}");
            }
        }

        // Use pipe separator to avoid collisions
        return string.Join('|', parts);
    }

    /// <summary>
    /// Computes a cryptographically secure hash for cache keys
    /// </summary>
    private static string ComputeSecureHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);

        // Use base64 for shorter, URL-safe keys (vs hex which is longer)
        return Convert.ToBase64String(hashBytes)[..16]; // Take first 16 chars for shorter keys
    }

    /// <summary>
    /// Safely reads content without modifying the original HttpContent
    /// </summary>
    private static string CloneAndReadContent(HttpContent content)
    {
        // For most content types, we can read safely
        // This is a simplified approach - in production might need more sophisticated cloning
        return content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
