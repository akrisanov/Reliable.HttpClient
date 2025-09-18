using System.Text;

using FluentAssertions;
using Xunit;

using Reliable.HttpClient.Caching.Abstractions;

namespace Reliable.HttpClient.Caching.Tests.Providers;

public class DefaultCacheKeyGeneratorTests
{
    private readonly DefaultCacheKeyGenerator _generator = new();

    [Fact]
    public void GenerateKey_WithBasicGetRequest_ReturnsExpectedKey()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().StartWith("GET|https://api.example.com/users|public");
    }

    [Fact]
    public void GenerateKey_WithQueryParameters_IncludesQueryInKey()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users?page=1&limit=10");

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().StartWith("GET|https://api.example.com/users?page=1&limit=10|public");
    }

    [Fact]
    public void GenerateKey_WithAuthorizationHeader_IncludesHashedAuth()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret-token");

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().StartWith("GET|https://api.example.com/users|auth|");
        key.Should().NotContain("secret-token"); // Should not contain actual token
        key.Length.Should().BeGreaterThan("GET|https://api.example.com/users|auth|".Length);
    }

    [Fact]
    public void GenerateKey_WithSameAuthToken_ReturnsSameKey()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");

        request1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "same-token");
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "same-token");

        // Act
        var key1 = _generator.GenerateKey(request1);
        var key2 = _generator.GenerateKey(request2);

        // Assert
        key1.Should().Be(key2);
    }

    [Fact]
    public void GenerateKey_WithDifferentAuthTokens_ReturnsDifferentKeys()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");

        request1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-1");
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-2");

        // Act
        var key1 = _generator.GenerateKey(request1);
        var key2 = _generator.GenerateKey(request2);

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateKey_WithCustomHeaders_IncludesHeadersInKey()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        request.Headers.Add("X-Custom-Header", "custom-value");
        request.Headers.Add("Accept-Language", "en-US");

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().Contain("X-Custom-Header|custom-value");
        key.Should().Contain("Accept-Language|en-US");
    }

    [Fact]
    public void GenerateKey_WithPostRequest_IncludesMethodInKey()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().StartWith("POST|https://api.example.com/users|public");
    }

    [Fact]
    public void GenerateKey_WithRequestBody_IncludesBodyHashInKey()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users")
        {
            Content = new StringContent("{\"name\":\"John\"}", Encoding.UTF8, "application/json"),
        };

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().Contain("body|");
        key.Should().NotContain("{\"name\":\"John\"}"); // Should not contain actual content
    }

    [Fact]
    public void GenerateKey_WithSameRequestBody_ReturnsSameKey()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");

        request1.Content = new StringContent("{\"name\":\"John\"}", Encoding.UTF8, "application/json");
        request2.Content = new StringContent("{\"name\":\"John\"}", Encoding.UTF8, "application/json");

        // Act
        var key1 = _generator.GenerateKey(request1);
        var key2 = _generator.GenerateKey(request2);

        // Assert
        key1.Should().Be(key2);
    }

    [Fact]
    public void GenerateKey_WithDifferentRequestBodies_ReturnsDifferentKeys()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/users");

        request1.Content = new StringContent("{\"name\":\"John\"}", Encoding.UTF8, "application/json");
        request2.Content = new StringContent("{\"name\":\"Jane\"}", Encoding.UTF8, "application/json");

        // Act
        var key1 = _generator.GenerateKey(request1);
        var key2 = _generator.GenerateKey(request2);

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateKey_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<string> action = () => _generator.GenerateKey(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateKey_WithEmptyUri_HandlesGracefully()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "");

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().StartWith("GET||public");
    }

    [Fact]
    public void GenerateKey_WithSimilarAuthTokens_GeneratesDifferentKeys()
    {
        // Arrange - Test that similar tokens generate completely different hashes
        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");

        request1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token123");
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token124"); // Only one character different

        // Act
        var key1 = _generator.GenerateKey(request1);
        var key2 = _generator.GenerateKey(request2);

        // Assert
        key1.Should().NotBe(key2);

        // Both should contain auth hashes but be different
        key1.Should().Contain("auth|");
        key2.Should().Contain("auth|");

        // Extract auth hash parts (format: GET|url|auth|hash)
        var parts1 = key1.Split('|');
        var parts2 = key2.Split('|');

        parts1.Length.Should().BeGreaterOrEqualTo(4);
        parts2.Length.Should().BeGreaterOrEqualTo(4);

        var hash1 = parts1[3]; // The actual hash part
        var hash2 = parts2[3];

        hash1.Should().NotBe(hash2);
        hash1.Should().NotBeEmpty();
        hash2.Should().NotBeEmpty();

        // Ensure hashes don't have common prefixes (avalanche effect)
        if (hash1.Length > 4 && hash2.Length > 4)
        {
            hash1[..4].Should().NotBe(hash2[..4]);
        }
    }

    [Fact]
    public void GenerateKey_WithLongAuthToken_ProducesConsistentShortHash()
    {
        // Arrange - Test with very long auth token
        var longToken = new string('a', 10_000); // 10KB token
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", longToken);

        // Act
        var key = _generator.GenerateKey(request);

        // Assert
        key.Should().Contain("auth|");
        var parts = key.Split('|');
        parts.Length.Should().BeGreaterOrEqualTo(4);

        var authHash = parts[3];
        authHash.Should().NotContain(longToken); // Should not contain original token
        authHash.Length.Should().BeLessOrEqualTo(20); // Hash should be short regardless of input size
    }

    [Fact]
    public void GenerateKey_CryptographicHashConsistency_ReturnsSameHashForSameInput()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/users");

        var token = "consistent-token-for-testing-123!@#$%^&*()";
        request1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Generate multiple times
        var key1a = _generator.GenerateKey(request1);
        var key1b = _generator.GenerateKey(request1);
        var key2 = _generator.GenerateKey(request2);

        // Assert - Should be identical every time
        key1a.Should().Be(key1b);
        key1a.Should().Be(key2);
    }
}
