using System.Net;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpClientAdapterTests
{
    private readonly Mock<IHttpResponseHandler> _mockResponseHandler = new();
    private readonly Mock<ILogger<HttpClientAdapterTests>> _mockLogger = new();

    [Fact]
    public async Task GetAsync_WithStringUri_CallsResponseHandler()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient(new MockHttpMessageHandler("{\"id\": 1, \"name\": \"Test\"}"));
        var adapter = new HttpClientAdapter(httpClient, _mockResponseHandler.Object);
        var expectedResponse = new TestResponse { Id = 1, Name = "Test" };

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        TestResponse result = await adapter.GetAsync<TestResponse>("https://api.test.com/test");

        // Assert
        result.Should().Be(expectedResponse);
        _mockResponseHandler.Verify(
            x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithUri_CallsResponseHandler()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient(new MockHttpMessageHandler("{\"id\": 1, \"name\": \"Test\"}"));
        var adapter = new HttpClientAdapter(httpClient, _mockResponseHandler.Object);
        var expectedResponse = new TestResponse { Id = 1, Name = "Test" };

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        TestResponse result = await adapter.GetAsync<TestResponse>(new Uri("https://api.test.com/test"));

        // Assert
        result.Should().Be(expectedResponse);
        _mockResponseHandler.Verify(
            x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_WithTypedResponse_CallsResponseHandler()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient(new MockHttpMessageHandler("{\"id\": 1, \"name\": \"Created\"}"));
        var adapter = new HttpClientAdapter(httpClient, _mockResponseHandler.Object);
        var request = new TestRequest { Name = "New Item" };
        var expectedResponse = new TestResponse { Id = 1, Name = "Created" };

        _mockResponseHandler
            .Setup(x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        TestResponse result = await adapter.PostAsync<TestRequest, TestResponse>("https://api.test.com/test", request);

        // Assert
        result.Should().Be(expectedResponse);
        _mockResponseHandler.Verify(
            x => x.HandleAsync<TestResponse>(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_WithoutTypedResponse_ReturnsHttpResponseMessage()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient(new MockHttpMessageHandler("Created"));
        var adapter = new HttpClientAdapter(httpClient, _mockResponseHandler.Object);
        var request = new TestRequest { Name = "New Item" };

        // Act
        HttpResponseMessage result = await adapter.PostAsync("https://api.test.com/test", request);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<HttpClientAdapter> act = () => new HttpClientAdapter(null!, _mockResponseHandler.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullResponseHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient();

        // Act & Assert
        Func<HttpClientAdapter> act = () => new HttpClientAdapter(httpClient, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("responseHandler");
    }

    private class TestRequest
    {
        public string Name { get; init; } = string.Empty;
    }

    private class TestResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private class MockHttpMessageHandler(string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response),
            });
        }
    }
}
