using Xunit;
using Moq;
using Moq.Protected;
using ConcurrentPing.Core.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentPinger.Core.Tests.Services
{
    public class HttpPingStrategyTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly HttpPingStrategy _httpPingStrategy;

        public HttpPingStrategyTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _httpPingStrategy = new HttpPingStrategy(_httpClient);
        }

        [Fact]
        public async Task PingAsync_SuccessfulResponse_ReturnsSuccessResult()
        {
            // Arrange
            var url = "https://example.com";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Success")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _httpPingStrategy.PingAsync(url);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(url, result.Url);
            Assert.Equal(200, result.StatusCode);
            Assert.True(result.ResponseTime > TimeSpan.Zero);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public async Task PingAsync_HttpRequestException_ReturnsFailureResult()
        {
            // Arrange
            var url = "https://invalid-url.com";
            var exceptionMessage = "Connection failed";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException(exceptionMessage));

            // Act
            var result = await _httpPingStrategy.PingAsync(url);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(url, result.Url);
            Assert.Equal(0, result.StatusCode);
            Assert.Contains(exceptionMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task PingAsync_TaskCanceledException_ReturnsTimeoutResult()
        {
            // Arrange
            var url = "https://timeout-url.com";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            // Act
            var result = await _httpPingStrategy.PingAsync(url);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(url, result.Url);
            Assert.Equal(0, result.StatusCode);
            Assert.Contains("timeout", result.ErrorMessage.ToLower());
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound, 404)]
        [InlineData(HttpStatusCode.InternalServerError, 500)]
        [InlineData(HttpStatusCode.BadRequest, 400)]
        public async Task PingAsync_DifferentStatusCodes_ReturnsCorrectStatusCode(HttpStatusCode statusCode, int expectedCode)
        {
            // Arrange
            var url = "https://example.com";
            var responseMessage = new HttpResponseMessage(statusCode);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _httpPingStrategy.PingAsync(url);

            // Assert
            Assert.Equal(expectedCode, result.StatusCode);
            Assert.Equal(url, result.Url);
            Assert.True(result.IsSuccess); // HTTP response received, even if not 2xx
        }

        [Fact]
        public async Task PingAsync_GenericException_ReturnsFailureResult()
        {
            // Arrange
            var url = "https://example.com";
            var exceptionMessage = "Unexpected error";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _httpPingStrategy.PingAsync(url);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(url, result.Url);
            Assert.Equal(0, result.StatusCode);
            Assert.Contains(exceptionMessage, result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task PingAsync_InvalidUrl_ReturnsFailureResult(string invalidUrl)
        {
            // Act
            var result = await _httpPingStrategy.PingAsync(invalidUrl);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(invalidUrl, result.Url);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task PingAsync_ValidUrl_MeasuresResponseTime()
        {
            // Arrange
            var url = "https://example.com";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async () =>
                {
                    await Task.Delay(10); // Simulate some response time
                    return responseMessage;
                });

            // Act
            var result = await _httpPingStrategy.PingAsync(url);

            // Assert
            Assert.True(result.ResponseTime.TotalMilliseconds > 0);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _mockHttpMessageHandler?.Protected().Verify(
                "Dispose",
                Times.Never(),
                ItExpr.IsAny<bool>());
        }
    }
}