using ConcurrentPinger.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace ConcurrentPinger.Core.Tests.Services;

public class HttpPingStrategyTests
{
    [Fact]
    public async Task PingAsync_ReturnsSuccess_WhenHttpStatusIsSuccess()
    {
        // Arrange
        var url = "https://test.com";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });
        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = new Mock<ILogger<HttpPingStrategy>>();
        var strategy = new HttpPingStrategy(httpClient, loggerMock.Object);

        // Act
        var result = await strategy.PingAsync(url);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(url, result.Url);
        Assert.True(result.ResponseTimeMs >= 0);
    }

    [Fact]
    public async Task PingAsync_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        var url = "https://fail.com";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = new Mock<ILogger<HttpPingStrategy>>();
        var strategy = new HttpPingStrategy(httpClient, loggerMock.Object);

        // Act
        var result = await strategy.PingAsync(url);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(url, result.Url);
        Assert.Equal("Network error", result.ErrorMessage);
    }
}