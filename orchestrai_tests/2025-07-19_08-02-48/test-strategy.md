I'll analyze the ConcurrentPinger repository and generate comprehensive unit tests for all C# files to achieve 100% coverage. Based on the file structure, this is a concurrent URL pinging application.

=== FILE: ConcurrentPinger.Core.Tests/Models/UrlPingResultTests.cs ===
```csharp
using System;
using Xunit;
using ConcurrentPing.Core.Models;

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
            var result = new UrlPingResult(url, isSuccess, responseTime, statusCode, errorMessage);

            // Assert
            Assert.Equal(url, result.Url);
            Assert.Equal(isSuccess, result.IsSuccess);
            Assert.Equal(responseTime, result.ResponseTime);
            Assert.Equal(statusCode, result.StatusCode);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void UrlPingResult_WithFailure_SetsPropertiesCorrectly()
        {
            // Arrange
            var url = "https://invalid-url.com";
            var isSuccess = false;
            var responseTime = TimeSpan.Zero;
            var statusCode = 0;
            var errorMessage = "Connection timeout";

            // Act
            var result = new UrlPingResult(url, isSuccess, responseTime, statusCode, errorMessage);

            // Assert
            Assert.Equal(url, result.Url);
            Assert.False(result.IsSuccess);
            Assert.Equal(responseTime, result.ResponseTime);
            Assert.Equal(statusCode, result.StatusCode);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void UrlPingResult_WithEmptyOrNullUrl_HandlesCorrectly(string url)
        {
            // Act
            var result = new UrlPingResult(url, false, TimeSpan.Zero, 0, "Invalid URL");

            // Assert
            Assert.Equal(url, result.Url);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void UrlPingResult_ToString_ReturnsFormattedString()
        {
            // Arrange
            var result = new UrlPingResult("https://example.com", true, TimeSpan.FromMilliseconds(150), 200, "");

            // Act
            var stringResult = result.ToString();

            // Assert
            Assert.Contains("https://example.com", stringResult);
            Assert.Contains("200", stringResult);
            Assert.Contains("150", stringResult);
        }

        [Fact]
        public void UrlPingResult_Equality_WorksCorrectly()
        {
            // Arrange
            var result1 = new UrlPingResult("https://example.com", true, TimeSpan.FromMilliseconds(100), 200, "");
            var result2 = new UrlPingResult("https://example.com", true, TimeSpan.FromMilliseconds(100), 200, "");
            var result3 = new UrlPingResult("https://different.com", true, TimeSpan.FromMilliseconds(100), 200, "");

            // Assert
            Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
            Assert.NotEqual(result1.GetHashCode(), result3.GetHashCode());
        }
    }
}
```
=== END FILE ===

=== FILE: ConcurrentPinger.Core.Tests/Services/HttpPingStrategyTests.cs ===
```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using ConcurrentPing.Core.Services;

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
        public async Task PingAsync_TaskCancelledException_ReturnsTimeoutResult()
        {
            // Arrange
            var url = "https://slow-server.com";
            
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
        }

        [Fact]
        public async Task PingAsync_WithTimeout_ConfiguresHttpClientCorrectly()
        {
            // Arrange
            var url = "https://example.com";
            var timeout = TimeSpan.FromSeconds(5);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var strategyWithTimeout = new HttpPingStrategy(_httpClient, timeout);

            // Act
            var result = await strategyWithTimeout.PingAsync(url);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(timeout, _httpClient.Timeout);
        }

        [Fact]
        public async Task PingAsync_GeneralException_ReturnsFailureResult()
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
            Assert.Contains(exceptionMessage, result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("invalid-url")]
        public async Task PingAsync_InvalidUrl_ReturnsFailureResult(string invalidUrl)
        {
            // Act
            var result = await _httpPingStrategy.PingAsync(invalidUrl);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(invalidUrl, result.Url);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _mockHttpMessageHandler?.Dispose();
        }
    }
}
```
=== END FILE ===

=== FILE: ConcurrentPinger.Core.Tests/Services/UrlPingerTests.cs ===
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using ConcurrentPing.Core.Models;
using ConcurrentPing.Core.Services;

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
        public async Task PingUrlsAsync_SingleUrl_ReturnsResult()
        {
            // Arrange
            var url = "https://example.com";
            var urls = new[] { url };
            var expectedResult = new UrlPingResult(url, true, TimeSpan.FromMilliseconds(100), 200, "");
            
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
        public async Task PingUrlsAsync_MultipleUrls_ReturnsAllResults()
        {
            // Arrange
            var urls = new[] { "https://example1.com", "https://example2.com", "https://example3.com" };
            var expectedResults = urls.Select(url => 
                new UrlPingResult(url, true, TimeSpan.FromMilliseconds(100), 200, "")).ToList();

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
        }

        [Fact]
        public async Task PingUrlsAsync_ConcurrentExecution_ExecutesInParallel()
        {
            // Arrange
            var urls = new[] { "https://example1.com", "https://example2.com", "https://example3.com" };
            var delayTasks = new List<TaskCompletionSource<UrlPingResult>>();

            foreach (var url in urls)
            {
                var tcs = new TaskCompletionSource<UrlPingResult>();
                delayTasks.Add(tcs);
                _mockPingStrategy.Setup(x => x.PingAsync(url))
                    .Returns(tcs.Task);
            }

            // Act
            var pingTask = _urlPinger.PingUrlsAsync(urls);

            // Complete all tasks simultaneously
            foreach (var (tcs, url) in delayTasks.Zip(urls))
            {
                tcs.SetResult(new UrlPingResult(url, true, TimeSpan.FromMilliseconds(50), 200, ""));
            }

            var results = await pingTask;

            // Assert
            Assert.Equal(3, results.Count());
            _mockPingStrategy.Verify(x => x.PingAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task PingUrlsAsync_MixedSuccessAndFailure_ReturnsAllResults()
        {
            // Arrange
            var urls = new[] { "https://success.com", "https://failure.com" };
            var successResult = new UrlPingResult("https://success.com", true, TimeSpan.FromMilliseconds(100), 200, "");
            var failureResult = new UrlPingResult("https://failure.com", false, TimeSpan.Zero, 0, "Connection failed");

            _mockPingStrategy.Setup(x => x.PingAsync("https://success.com"))
                .ReturnsAsync(successResult);
            _mockPingStrategy.Setup(x => x.PingAsync("https://failure.com"))
                .ReturnsAsync(failureResult);

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Equal(2, results.Count());
            Assert.Contains(results, r => r.IsSuccess && r.Url == "https://success.com");
            Assert.Contains(results, r => !r.IsSuccess && r