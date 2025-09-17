using System.Net;
using System.Text;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Reliable.HttpClient.Tests;

public class HttpResponseHandlerBaseTests
{
    private readonly TestHttpResponseHandler _handler = null!;
    private readonly Mock<ILogger> _loggerMock = null!;

    public HttpResponseHandlerBaseTests()
    {
        _loggerMock = new Mock<ILogger>();
        _handler = new TestHttpResponseHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task ReadResponseContentAsync_ValidContent_ReturnsContent()
    {
        // Arrange
        const string expectedContent = "test content";
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedContent, Encoding.UTF8, "text/plain"),
        };

        // Act
        var result = await _handler.ReadResponseContentAsync(response);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task ReadResponseContentAsync_ExceptionDuringRead_ReturnsEmptyString()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ThrowingStringContent();

        // Act
        var result = await _handler.ReadResponseContentAsync(response);

        // Assert
        result.Should().Be(string.Empty);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to read HTTP response content")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogHttpResponse_SuccessfulResponse_LogsDebug()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        _handler.LogHttpResponse(response, serviceName: "TestService");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestService HTTP request successful")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogHttpResponse_FailedResponse_LogsError()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };
        const string content = "error details";

        // Act
        _handler.LogHttpResponse(response, content, "TestService");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestService HTTP error") &&
                                              v.ToString()!.Contains("Bad Request") &&
                                              v.ToString()!.Contains("error details")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void IsSuccessStatusCode_SuccessfulResponse_ReturnsTrue()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var result = TestHttpResponseHandler.IsSuccessStatusCode(response);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSuccessStatusCode_FailedResponse_ReturnsFalse()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

        // Act
        var result = TestHttpResponseHandler.IsSuccessStatusCode(response);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetStatusCodeDescription_KnownStatusCodes_ReturnsCorrectDescription()
    {
        // Act & Assert
        TestHttpResponseHandler.GetStatusCodeDescription(HttpStatusCode.Unauthorized)
            .Should().Be("Authentication error");

        TestHttpResponseHandler.GetStatusCodeDescription(HttpStatusCode.Forbidden)
            .Should().Be("Access forbidden");

        TestHttpResponseHandler.GetStatusCodeDescription(HttpStatusCode.TooManyRequests)
            .Should().Be("Rate limit exceeded");

        TestHttpResponseHandler.GetStatusCodeDescription(HttpStatusCode.InternalServerError)
            .Should().Be("Internal server error");
    }

    [Fact]
    public void GetStatusCodeDescription_UnknownStatusCode_ReturnsGenericDescription()
    {
        // Act
        var result = TestHttpResponseHandler.GetStatusCodeDescription(HttpStatusCode.NotImplemented);

        // Assert
        result.Should().Be("HTTP 501: NotImplemented");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new TestHttpResponseHandler(null!))
            .Should().Throw<ArgumentNullException>()
            .WithMessage("*logger*");
    }

    // Test implementation for accessing protected methods
    private class TestHttpResponseHandler(ILogger logger) : HttpResponseHandlerBase<string>(logger)
    {
        public override async Task<string> HandleAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
            // Simple implementation for testing - just return content
            return await ReadResponseContentAsync(response, cancellationToken).ConfigureAwait(false);
        }
        public new async Task<string> ReadResponseContentAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
            => await base.ReadResponseContentAsync(response, cancellationToken).ConfigureAwait(false);

        public new void LogHttpResponse(HttpResponseMessage response, string? content = null, string serviceName = "ExternalService")
            => base.LogHttpResponse(response, content, serviceName);

        public static new bool IsSuccessStatusCode(HttpResponseMessage response)
            => HttpResponseHandlerBase<string>.IsSuccessStatusCode(response);

        public static new string GetStatusCodeDescription(HttpStatusCode statusCode)
            => HttpResponseHandlerBase<string>.GetStatusCodeDescription(statusCode);
    }

    // Mock content that throws exception when reading
    private class ThrowingStringContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            throw new InvalidOperationException("Simulated read error");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
