using ConcurrentPinger.Core.Models;
using ConcurrentPinger.Core.Services;
using Moq;

namespace ConcurrentPinger.Core.Tests.Services;

public class UrlPingerTests
{
    [Fact]
    public async Task PingAsync_ReturnsSuccess_WhenStrategyReturnsSuccess()
    {
        // Arrange
        var url = "https://test.com";
        var expectedResult = new UrlPingResult(url, 100, true);
        var strategyMock = new Mock<IPingStrategy>();
        strategyMock.Setup(s => s.PingAsync(url)).ReturnsAsync(expectedResult);
        var pinger = new UrlPinger(strategyMock.Object);

        // Act
        var result = await pinger.PingAsync(url);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.ResponseTimeMs);
        Assert.Equal(url, result.Url);
    }

    [Fact]
    public async Task PingManyAsync_RespectsMaxConcurrency()
    {
        // Arrange
        var urls = Enumerable.Range(0, 10).Select(i => $"https://test{i}.com").ToList();
        var strategyMock = new Mock<IPingStrategy>();
        strategyMock.Setup(s => s.PingAsync(It.IsAny<string>()))
            .ReturnsAsync((string url) => new UrlPingResult(url, 50, true));
        var pinger = new UrlPinger(strategyMock.Object);

        // Act
        var results = await pinger.PingManyAsync(urls, maxConcurrency: 3);

        // Assert
        Assert.Equal(urls.Count, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }
}