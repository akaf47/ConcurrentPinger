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