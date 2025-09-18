using System.Net;
using System.Text;
using System.Text.Json;
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

    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
