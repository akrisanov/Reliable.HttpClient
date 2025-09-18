using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Xunit;

namespace Reliable.HttpClient.Tests;

/// <summary>
/// Tests for DefaultHttpResponseHandler
/// </summary>
public class DefaultHttpResponseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var testData = new TestResponse { Id = 1, Name = "Test" };
        var json = JsonSerializer.Serialize(testData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act
        TestResponse result = await handler.HandleAsync<TestResponse>(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyContent_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response));
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("invalid json", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response));
    }

    [Fact]
    public async Task HandleAsync_WithErrorStatusCode_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = content };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response));
    }

    [Fact]
    public async Task HandleAsync_WithNullDeserializationResult_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("null", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act & Assert
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response));

        Assert.Contains("Failed to deserialize response to TestResponse", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task HandleAsync_WithDifferentErrorStatusCodes_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(statusCode) { Content = content };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response));
    }

    [Fact]
    public async Task HandleAsync_WithCustomJsonOptions_UsesProvidedOptions()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        IOptions<JsonSerializerOptions> optionsWrapper = Options.Create(customOptions);
        var handler = new DefaultHttpResponseHandler(optionsWrapper);

        var testData = new TestSnakeCaseResponse { CustomProperty = "test-value" };
        var json = JsonSerializer.Serialize(testData, customOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act
        TestSnakeCaseResponse result = await handler.HandleAsync<TestSnakeCaseResponse>(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.CustomProperty, result.CustomProperty);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("{\"id\": 1}", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response, cts.Token));

        // The inner exception should be TaskCanceledException or OperationCanceledException
        Assert.NotNull(exception.InnerException);
        Assert.IsType<TaskCanceledException>(exception.InnerException);
    }

    [Fact]
    public async Task HandleAsync_WithComplexObject_DeserializesCorrectly()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var testData = new ComplexTestResponse
        {
            Id = 42,
            Name = "Complex Test",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Tags = ["tag1", "tag2", "tag3"],
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { "key1", "value1" },
                { "key2", 123 },
            },
        };

        var json = JsonSerializer.Serialize(testData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act
        ComplexTestResponse result = await handler.HandleAsync<ComplexTestResponse>(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
        Assert.Equal(testData.IsActive, result.IsActive);
        Assert.Equal(testData.Tags.Length, result.Tags.Length);
        Assert.Equal(testData.Tags, result.Tags);
        Assert.Equal(testData.Metadata.Count, result.Metadata.Count);
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceOnlyContent_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DefaultHttpResponseHandler();
        var content = new StringContent("   ", Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act & Assert
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync<TestResponse>(response));

        Assert.Contains("Invalid JSON response", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultJsonOptions_WorksCorrectly()
    {
        // Arrange - No custom options provided
        var handler = new DefaultHttpResponseHandler();
        var testData = new TestResponse { Id = 1, Name = "Test" };

        // Use default JSON options for serialization
        var json = JsonSerializer.Serialize(testData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

        // Act
        TestResponse result = await handler.HandleAsync<TestResponse>(response);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData.Id, result.Id);
        Assert.Equal(testData.Name, result.Name);
    }

    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestSnakeCaseResponse
    {
        public string CustomProperty { get; set; } = string.Empty;
    }

    private class ComplexTestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Metadata { get; set; } = new(StringComparer.Ordinal);
    }
}
