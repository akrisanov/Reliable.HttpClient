using System.Net;
using System.Net.Http;

using FluentAssertions;

using Moq;

using Reliable.HttpClient.Caching.Abstractions;

using Xunit;

namespace Reliable.HttpClient.Caching.Tests;

public class HttpCacheOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new HttpCacheOptions();

        // Assert
        options.DefaultExpiry.Should().Be(TimeSpan.FromMinutes(5));
        options.DefaultHeaders.Should().NotBeNull().And.BeEmpty();
        options.CacheableMethods.Should().Contain(HttpMethod.Get);
        options.CacheableMethods.Should().Contain(HttpMethod.Head);
        options.CacheableStatusCodes.Should().Contain(HttpStatusCode.OK);
        options.KeyGenerator.Should().NotBeNull();
    }

    [Fact]
    public void DefaultHeaders_CanAddAndRetrieveHeaders()
    {
        // Arrange
        var options = new HttpCacheOptions();

        // Act
        options.DefaultHeaders["Authorization"] = "Bearer token";
        options.DefaultHeaders["X-API-Key"] = "api-key";

        // Assert
        options.DefaultHeaders.Should().HaveCount(2);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.DefaultHeaders["X-API-Key"].Should().Be("api-key");
    }

    [Fact]
    public void DefaultHeaders_IsCaseInsensitive()
    {
        // Arrange
        var options = new HttpCacheOptions();

        // Act
        options.DefaultHeaders["authorization"] = "Bearer token";

        // Assert
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.DefaultHeaders["AUTHORIZATION"].Should().Be("Bearer token");
    }

    [Fact]
    public void GetExpiry_WithDefaultSettings_ReturnsDefaultExpiry()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        TimeSpan expiry = options.GetExpiry(request, response);

        // Assert
        expiry.Should().Be(options.DefaultExpiry);
    }

    [Fact]
    public void GetExpiry_WithCustomExpiryFunc_ReturnsCustomExpiry()
    {
        // Arrange
        var customExpiry = TimeSpan.FromMinutes(10);
        var options = new HttpCacheOptions
        {
            GetExpiry = (req, resp) => customExpiry,
        };
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        TimeSpan expiry = options.GetExpiry(request, response);

        // Assert
        expiry.Should().Be(customExpiry);
    }

    [Fact]
    public void GetExpiry_WithCacheControlMaxAge_ReturnsMaxAge()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromMinutes(15),
        };

        // Act
        TimeSpan expiry = options.GetExpiry(request, response);

        // Assert
        expiry.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetExpiry_WithZeroMaxAge_ReturnsZero()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = TimeSpan.Zero,
        };

        // Act
        TimeSpan expiry = options.GetExpiry(request, response);

        // Assert
        expiry.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetExpiry_WithNoCacheDirective_ReturnsZero()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
        };

        // Act
        TimeSpan expiry = options.GetExpiry(request, response);

        // Assert
        expiry.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetExpiry_WithNoStoreDirective_ReturnsZero()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoStore = true,
        };

        // Act
        TimeSpan expiry = options.GetExpiry(request, response);

        // Assert
        expiry.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ShouldCache_WithDefaultSettings_ReturnsTrue()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var shouldCache = options.ShouldCache(request, response);

        // Assert
        shouldCache.Should().BeTrue();
    }

    [Fact]
    public void ShouldCache_WithCustomFunc_ReturnsCustomResult()
    {
        // Arrange
        var options = new HttpCacheOptions
        {
            ShouldCache = (req, resp) => false,
        };
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var shouldCache = options.ShouldCache(request, response);

        // Assert
        shouldCache.Should().BeFalse();
    }

    [Fact]
    public void ShouldCache_WithNoCacheDirective_ReturnsFalse()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
        };

        // Act
        var shouldCache = options.ShouldCache(request, response);

        // Assert
        shouldCache.Should().BeFalse();
    }

    [Fact]
    public void ShouldCache_WithNoStoreDirective_ReturnsFalse()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            NoStore = true,
        };

        // Act
        var shouldCache = options.ShouldCache(request, response);

        // Assert
        shouldCache.Should().BeFalse();
    }

    [Fact]
    public void CacheableMethods_CanBeModified()
    {
        // Arrange
        var options = new HttpCacheOptions();

        // Act
        options.CacheableMethods.Add(HttpMethod.Post);
        options.CacheableMethods.Remove(HttpMethod.Get);

        // Assert
        options.CacheableMethods.Should().Contain(HttpMethod.Post);
        options.CacheableMethods.Should().NotContain(HttpMethod.Get);
        options.CacheableMethods.Should().Contain(HttpMethod.Head); // Should still contain Head
    }

    [Fact]
    public void CacheableStatusCodes_CanBeModified()
    {
        // Arrange
        var options = new HttpCacheOptions();

        // Act
        options.CacheableStatusCodes.Add(HttpStatusCode.Created);
        options.CacheableStatusCodes.Remove(HttpStatusCode.OK);

        // Assert
        options.CacheableStatusCodes.Should().Contain(HttpStatusCode.Created);
        options.CacheableStatusCodes.Should().NotContain(HttpStatusCode.OK);
    }

    [Fact]
    public void KeyGenerator_CanBeReplaced()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var mockKeyGenerator = new Mock<ICacheKeyGenerator>();

        // Act
        options.KeyGenerator = mockKeyGenerator.Object;

        // Assert
        options.KeyGenerator.Should().Be(mockKeyGenerator.Object);
    }

    [Fact]
    public void DefaultExpiry_CanBeChanged()
    {
        // Arrange
        var options = new HttpCacheOptions();
        var newExpiry = TimeSpan.FromHours(1);

        // Act
        options.DefaultExpiry = newExpiry;

        // Assert
        options.DefaultExpiry.Should().Be(newExpiry);
    }

    [Fact]
    public void GetExpiry_CanBeSet()
    {
        // Arrange
        var options = new HttpCacheOptions();
        Func<HttpRequestMessage, HttpResponseMessage, TimeSpan> customFunc = (req, resp) => TimeSpan.FromMinutes(30);

        // Act
        options.GetExpiry = customFunc;

        // Assert
        options.GetExpiry.Should().Be(customFunc);
    }

    [Fact]
    public void ShouldCache_CanBeSet()
    {
        // Arrange
        var options = new HttpCacheOptions();
        Func<HttpRequestMessage, HttpResponseMessage, bool> customFunc = (req, resp) => req.Method == HttpMethod.Get;

        // Act
        options.ShouldCache = customFunc;

        // Assert
        options.ShouldCache.Should().Be(customFunc);
    }
}
