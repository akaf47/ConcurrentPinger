Based on the repository analysis, I'll generate comprehensive unit tests for all C# files to achieve 100% coverage. Here are the test files:

=== FILE: ConcurrentPinger.Core.Tests/Models/UrlPingResultTests.cs ===
using Xunit;
using ConcurrentPing.Core.Models;
using System;

namespace ConcurrentPinger.Core.Tests.Models
{
    public class UrlPingResultTests
    {
        [Fact]
        public void UrlPingResult_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var url = "https://example.com";
            var isSuccess = true;
            var responseTime = TimeSpan.FromMilliseconds(100);
            var statusCode = 200;
            var errorMessage = "";

            // Act
            var result = new UrlPingResult
            {
                Url = url,
                IsSuccess = isSuccess,
                ResponseTime = responseTime,
                StatusCode = statusCode,
                ErrorMessage = errorMessage
            };

            // Assert
            Assert.Equal(url, result.Url);
            Assert.Equal(isSuccess, result.IsSuccess);
            Assert.Equal(responseTime, result.ResponseTime);
            Assert.Equal(statusCode, result.StatusCode);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void UrlPingResult_DefaultValues_AreSetCorrectly()
        {
            // Act
            var result = new UrlPingResult();

            // Assert
            Assert.Null(result.Url);
            Assert.False(result.IsSuccess);
            Assert.Equal(TimeSpan.Zero, result.ResponseTime);
            Assert.Equal(0, result.StatusCode);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData("https://google.com", true, 150, 200, "")]
        [InlineData("https://invalid-url.com", false, 0, 0, "Timeout")]
        [InlineData("http://localhost:8080", true, 50, 404, "Not Found")]
        public void UrlPingResult_WithVariousValues_SetsPropertiesCorrectly(
            string url, bool isSuccess, int responseTimeMs, int statusCode, string errorMessage)
        {
            // Arrange
            var responseTime = TimeSpan.FromMilliseconds(responseTimeMs);

            // Act
            var result = new UrlPingResult
            {
                Url = url,
                IsSuccess = isSuccess,
                ResponseTime = responseTime,
                StatusCode = statusCode,
                ErrorMessage = errorMessage
            };

            // Assert
            Assert.Equal(url, result.Url);
            Assert.Equal(isSuccess, result.IsSuccess);
            Assert.Equal(responseTime, result.ResponseTime);
            Assert.Equal(statusCode, result.StatusCode);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void UrlPingResult_ToString_ReturnsFormattedString()
        {
            // Arrange
            var result = new UrlPingResult
            {
                Url = "https://example.com",
                IsSuccess = true,
                ResponseTime = TimeSpan.FromMilliseconds(100),
                StatusCode = 200,
                ErrorMessage = ""
            };

            // Act
            var toString = result.ToString();

            // Assert
            Assert.Contains("https://example.com", toString);
            Assert.Contains("True", toString);
            Assert.Contains("200", toString);
        }
    }
}
=== END FILE ===

=== FILE: ConcurrentPinger.Core.Tests/Services/HttpPingStrategyTests.cs ===
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
=== END FILE ===

=== FILE: ConcurrentPinger.Core.Tests/Services/UrlPingerTests.cs ===
using Xunit;
using Moq;
using ConcurrentPing.Core.Services;
using ConcurrentPing.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrentPinger.Core.Tests.Services
{
    public class UrlPingerTests
    {
        private readonly Mock<IPingStrategy> _mockPingStrategy;
        private readonly UrlPinger _urlPinger;

        public UrlPingerTests()
        {
            _mockPingStrategy = new Mock<IPingStrategy>();
            _urlPinger = new UrlPinger(_mockPingStrategy.Object);
        }

        [Fact]
        public async Task PingUrlsAsync_SingleUrl_ReturnsOneResult()
        {
            // Arrange
            var url = "https://example.com";
            var urls = new[] { url };
            var expectedResult = new UrlPingResult
            {
                Url = url,
                IsSuccess = true,
                StatusCode = 200,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };

            _mockPingStrategy.Setup(x => x.PingAsync(url))
                .ReturnsAsync(expectedResult);

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Single(results);
            Assert.Equal(expectedResult.Url, results.First().Url);
            Assert.Equal(expectedResult.IsSuccess, results.First().IsSuccess);
            Assert.Equal(expectedResult.StatusCode, results.First().StatusCode);
        }

        [Fact]
        public async Task PingUrlsAsync_MultipleUrls_ReturnsMultipleResults()
        {
            // Arrange
            var urls = new[] { "https://example1.com", "https://example2.com", "https://example3.com" };
            var expectedResults = urls.Select(url => new UrlPingResult
            {
                Url = url,
                IsSuccess = true,
                StatusCode = 200,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            }).ToList();

            foreach (var (url, result) in urls.Zip(expectedResults))
            {
                _mockPingStrategy.Setup(x => x.PingAsync(url))
                    .ReturnsAsync(result);
            }

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Equal(3, results.Count());
            Assert.All(results, result => Assert.True(result.IsSuccess));
            Assert.Equal(urls, results.Select(r => r.Url));
        }

        [Fact]
        public async Task PingUrlsAsync_EmptyUrlList_ReturnsEmptyResults()
        {
            // Arrange
            var urls = new string[0];

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task PingUrlsAsync_NullUrlList_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _urlPinger.PingUrlsAsync(null));
        }

        [Fact]
        public async Task PingUrlsAsync_MixedSuccessAndFailure_ReturnsAllResults()
        {
            // Arrange
            var urls = new[] { "https://success.com", "https://failure.com" };
            var successResult = new UrlPingResult
            {
                Url = "https://success.com",
                IsSuccess = true,
                StatusCode = 200,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };
            var failureResult = new UrlPingResult
            {
                Url = "https://failure.com",
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = "Connection failed"
            };

            _mockPingStrategy.Setup(x => x.PingAsync("https://success.com"))
                .ReturnsAsync(successResult);
            _mockPingStrategy.Setup(x => x.PingAsync("https://failure.com"))
                .ReturnsAsync(failureResult);

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Equal(2, results.Count());
            Assert.Contains(results, r => r.IsSuccess && r.Url == "https://success.com");
            