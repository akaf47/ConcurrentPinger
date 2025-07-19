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