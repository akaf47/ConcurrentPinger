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
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            
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
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task PingAsync_HttpRequestException_ReturnsFailureResult()
        {
            // Arrange
            var url = "https://invalid-url.com";
            var exceptionMessage = "Network error";
            
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
        [InlineData(HttpStatusCode.Unauthorized, 401)]
        public async Task PingAsync_VariousStatusCodes_ReturnsCorrectStatusCode(
            HttpStatusCode statusCode, int expectedStatusCode)
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
            Assert.Equal(expectedStatusCode, result.StatusCode);
            Assert.Equal(url, result.Url);
            Assert.True(result.ResponseTime >= TimeSpan.Zero);
        }

        [Fact]
        public async Task PingAsync_GeneralException_ReturnsFailureResult()
        {
            // Arrange
            var url = "https://example.com";
            var exceptionMessage = "General error";
            
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

        [Fact]
        public async Task PingAsync_NullUrl_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _httpPingStrategy.PingAsync(null));
        }

        [Fact]
        public async Task PingAsync_EmptyUrl_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _httpPingStrategy.PingAsync(""));
        }

        [Fact]
        public async Task PingAsync_WhitespaceUrl_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _httpPingStrategy.PingAsync("   "));
        }

        [Fact]
        public async Task PingAsync_ValidUrl_MakesHttpGetRequest()
        {
            // Arrange
            var url = "https://example.com";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            await _httpPingStrategy.PingAsync(url);

            // Assert
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>());
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _mockHttpMessageHandler?.Dispose();
        }
    }
}