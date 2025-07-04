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