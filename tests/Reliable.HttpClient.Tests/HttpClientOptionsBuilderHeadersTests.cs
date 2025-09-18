using FluentAssertions;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientOptionsBuilderHeadersTests
{
    [Fact]
    public void WithHeader_AddsHeaderToDefaultHeaders()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithHeader("Authorization", "Bearer test-token")
            .WithHeader("X-Custom-Header", "custom-value")
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(2);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer test-token");
        options.DefaultHeaders["X-Custom-Header"].Should().Be("custom-value");
    }

    [Fact]
    public void WithHeaders_AddsMultipleHeaders()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Authorization", "Bearer test-token" },
            { "X-API-Key", "api-key-123" },
            { "X-Client-Id", "client-456" }
        };

        // Act
        HttpClientOptions options = builder
            .WithHeaders(headers)
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(3);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer test-token");
        options.DefaultHeaders["X-API-Key"].Should().Be("api-key-123");
        options.DefaultHeaders["X-Client-Id"].Should().Be("client-456");
    }

    [Fact]
    public void WithoutHeader_RemovesSpecificHeader()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithHeader("Authorization", "Bearer test-token")
            .WithHeader("X-Custom-Header", "custom-value")
            .WithoutHeader("Authorization")
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(1);
        options.DefaultHeaders.Should().NotContainKey("Authorization");
        options.DefaultHeaders["X-Custom-Header"].Should().Be("custom-value");
    }

    [Fact]
    public void WithoutHeaders_ClearsAllHeaders()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithHeader("Authorization", "Bearer test-token")
            .WithHeader("X-Custom-Header", "custom-value")
            .WithoutHeaders()
            .Build();

        // Assert
        options.DefaultHeaders.Should().BeEmpty();
    }

    [Fact]
    public void WithHeader_WithNullOrWhiteSpaceName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithHeader(null!, "value"))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");

        builder.Invoking(b => b.WithHeader("", "value"))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");

        builder.Invoking(b => b.WithHeader("   ", "value"))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void WithHeader_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithHeader("Authorization", null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void WithHeaders_WithNullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithHeaders(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("headers");
    }

    [Fact]
    public void WithoutHeader_WithNullOrWhiteSpaceName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert
        builder.Invoking(b => b.WithoutHeader(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");

        builder.Invoking(b => b.WithoutHeader(""))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");

        builder.Invoking(b => b.WithoutHeader("   "))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void WithHeader_OverridesSameHeader()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithHeader("Authorization", "Bearer old-token")
            .WithHeader("Authorization", "Bearer new-token")
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(1);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer new-token");
    }

    [Fact]
    public void HeadersAreCaseInsensitive()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act
        HttpClientOptions options = builder
            .WithHeader("authorization", "Bearer test-token")
            .Build();

        // Assert
        options.DefaultHeaders.Should().HaveCount(1);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer test-token");
        options.DefaultHeaders["AUTHORIZATION"].Should().Be("Bearer test-token");
    }

    [Fact]
    public void WithHeaders_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();

        // Act & Assert - Empty key
        var headersWithEmptyKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "", "value" } };
        builder.Invoking(b => b.WithHeaders(headersWithEmptyKey))
            .Should().Throw<ArgumentException>()
            .WithParameterName("headers");

        // Act & Assert - Whitespace key
        var headersWithWhitespaceKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "   ", "value" } };
        builder.Invoking(b => b.WithHeaders(headersWithWhitespaceKey))
            .Should().Throw<ArgumentException>()
            .WithParameterName("headers");
    }

    [Fact]
    public void WithHeaders_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpClientOptionsBuilder();
        var headersWithNullValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Authorization", null! } };

        // Act & Assert
        builder.Invoking(b => b.WithHeaders(headersWithNullValue))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("headers");
    }
}
