using FluentAssertions;

using Reliable.HttpClient.Caching.Abstractions;

using Xunit;

namespace Reliable.HttpClient.Caching.Tests;

public class HttpCacheOptionsBuilderTests
{
    [Fact]
    public void Build_WithDefaultValues_ReturnsDefaultOptions()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act
        HttpCacheOptions options = builder.Build();

        // Assert
        options.Should().NotBeNull();
        options.DefaultExpiry.Should().Be(TimeSpan.FromMinutes(5));
        options.DefaultHeaders.Should().NotBeNull();
        options.DefaultHeaders.Should().BeEmpty();
    }

    [Fact]
    public void WithDefaultExpiry_SetsExpiry_ReturnsBuilder()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();
        var expiry = TimeSpan.FromMinutes(10);

        // Act
        HttpCacheOptionsBuilder result = builder.WithDefaultExpiry(expiry);

        // Assert
        result.Should().BeSameAs(builder);
        builder.Build().DefaultExpiry.Should().Be(expiry);
    }

    [Fact]
    public void WithDefaultExpiry_WithZeroTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act & Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithDefaultExpiry(TimeSpan.Zero));

        exception.ParamName.Should().Be("defaultExpiry");
    }

    [Fact]
    public void WithDefaultExpiry_WithNegativeTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act & Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithDefaultExpiry(TimeSpan.FromMinutes(-1)));

        exception.ParamName.Should().Be("defaultExpiry");
    }

    [Fact]
    public void AddHeader_ValidNameAndValue_AddsHeader()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act
        HttpCacheOptionsBuilder result = builder.AddHeader("Authorization", "Bearer token");

        // Assert
        result.Should().BeSameAs(builder);
        HttpCacheOptions options = builder.Build();
        options.DefaultHeaders.Should().ContainKey("Authorization");
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
    }

    [Fact]
    public void AddHeader_SameHeaderTwice_OverwritesValue()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act
        builder.AddHeader("Authorization", "Bearer token1")
               .AddHeader("Authorization", "Bearer token2");

        // Assert
        var options = builder.Build();
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token2");
    }

    [Fact]
    public void AddHeader_CaseInsensitive_HandlesCorrectly()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act
        builder.AddHeader("authorization", "Bearer token1")
               .AddHeader("AUTHORIZATION", "Bearer token2");

        // Assert
        HttpCacheOptions options = builder.Build();
        options.DefaultHeaders.Should().HaveCount(1);
        options.DefaultHeaders.Should().ContainKey("authorization");
        options.DefaultHeaders["AUTHORIZATION"].Should().Be("Bearer token2");
    }

    [Fact]
    public void AddHeader_MultipleHeaders_AddsAll()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act
        builder.AddHeader("Authorization", "Bearer token")
               .AddHeader("User-Agent", "TestApp/1.0")
               .AddHeader("X-API-Key", "api-key");

        // Assert
        HttpCacheOptions options = builder.Build();
        options.DefaultHeaders.Should().HaveCount(3);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.DefaultHeaders["User-Agent"].Should().Be("TestApp/1.0");
        options.DefaultHeaders["X-API-Key"].Should().Be("api-key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddHeader_InvalidHeaderName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddHeader(invalidName, "value"));
        exception.ParamName.Should().Be("name");
    }

    [Fact]
    public void AddHeader_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddHeader(null!, "value"));
        exception.ParamName.Should().Be("name");
    }

    [Fact]
    public void AddHeader_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder.AddHeader("Name", null!));
        exception.ParamName.Should().Be("value");
    }

    [Fact]
    public void AddHeaders_ValidDictionary_AddsAllHeaders()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer token",
            ["X-API-Key"] = "api-key",
            ["User-Agent"] = "TestApp/1.0",
        };

        // Act
        HttpCacheOptionsBuilder result = builder.AddHeaders(headers);

        // Assert
        result.Should().BeSameAs(builder);
        HttpCacheOptions options = builder.Build();
        options.DefaultHeaders.Should().HaveCount(3);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.DefaultHeaders["X-API-Key"].Should().Be("api-key");
        options.DefaultHeaders["User-Agent"].Should().Be("TestApp/1.0");
    }

    [Fact]
    public void AddHeaders_WithNullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder.AddHeaders(null!));
        exception.ParamName.Should().Be("headers");
    }

    [Fact]
    public void AddHeaders_WithEmptyDictionary_DoesNothing()
    {
        // Arrange
        var builder = new HttpCacheOptionsBuilder();
        var emptyHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Act
        builder.AddHeaders(emptyHeaders);

        // Assert
        HttpCacheOptions options = builder.Build();
        options.DefaultHeaders.Should().BeEmpty();
    }

    [Fact]
    public void FluentAPI_CombinedOperations_WorksCorrectly()
    {
        // Arrange & Act
        HttpCacheOptions options = new HttpCacheOptionsBuilder()
            .WithDefaultExpiry(TimeSpan.FromMinutes(15))
            .AddHeader("Authorization", "Bearer token")
            .AddHeaders(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-API-Version"] = "v2",
                ["Accept"] = "application/json",
            })
            .AddHeader("User-Agent", "TestApp/1.0")
            .Build();

        // Assert
        options.DefaultExpiry.Should().Be(TimeSpan.FromMinutes(15));
        options.DefaultHeaders.Should().HaveCount(4);
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token");
        options.DefaultHeaders["X-API-Version"].Should().Be("v2");
        options.DefaultHeaders["Accept"].Should().Be("application/json");
        options.DefaultHeaders["User-Agent"].Should().Be("TestApp/1.0");
    }
}
