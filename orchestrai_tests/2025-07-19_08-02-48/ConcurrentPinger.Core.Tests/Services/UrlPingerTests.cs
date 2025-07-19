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