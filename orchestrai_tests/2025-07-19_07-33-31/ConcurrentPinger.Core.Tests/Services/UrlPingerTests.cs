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
            _mockPingStrategy.Verify(x => x.PingAsync(url), Times.Once);
        }

        [Fact]
        public async Task PingUrlsAsync_MultipleUrls_ReturnsAllResults()
        {
            // Arrange
            var urls = new[] { "https://example1.com", "https://example2.com", "https://example3.com" };
            var expectedResults = urls.Select(url => 
                new UrlPingResult(url, true, TimeSpan.FromMilliseconds(100), 200, "")).ToList();

            for (int i = 0; i < urls.Length; i++)
            {
                _mockPingStrategy.Setup(x => x.PingAsync(urls[i]))
                    .ReturnsAsync(expectedResults[i]);
            }

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Equal(3, results.Count());
            Assert.All(results, result => Assert.True(result.IsSuccess));
            
            foreach (var url in urls)
            {
                _mockPingStrategy.Verify(x => x.PingAsync(url), Times.Once);
            }
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
            _mockPingStrategy.Verify(x => x.PingAsync(It.IsAny<string>()), Times.Never);
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
            var successResult = new UrlPingResult(urls[0], true, TimeSpan.FromMilliseconds(100), 200, "");
            var failureResult = new UrlPingResult(urls[1], false, TimeSpan.Zero, 0, "Connection failed");

            _mockPingStrategy.Setup(x => x.PingAsync(urls[0])).ReturnsAsync(successResult);
            _mockPingStrategy.Setup(x => x.PingAsync(urls[1])).ReturnsAsync(failureResult);

            // Act
            var results = await _urlPinger.PingUrlsAsync(urls);

            // Assert
            Assert.Equal(2, results.Count());
            Assert.Contains(results, r => r.IsSuccess && r.Url == urls[0]);
            Assert.Contains(results, r => !r.IsSuccess && r.Url == urls[1]);
        }

        [Fact]
        public async Task PingUrlsAsync_ConcurrentExecution_ExecutesInParallel()
        {
            // Arrange
            var urls = new[] { "https://example1.com", "https://example2.com