```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Moq;
using ConcurrentPinger.Core.Services;
using ConcurrentPinger.Core.Models;

namespace ConcurrentPinger.Core.Tests
{
    [TestClass]
    public class PingServiceTests
    {
        private Mock<IPingWrapper> _pingWrapperMock;
        private PingService _pingService;

        [TestInitialize]
        public void Setup()
        {
            _pingWrapperMock = new Mock<IPingWrapper>();
            _pingService = new PingService(_pingWrapperMock.Object);
        }

        [TestMethod]
        public async Task PingHost_ValidHost_ReturnsPingResult()
        {
            // Arrange
            var hostAddress = "8.8.8.8";
            var reply = new PingReply(IPStatus.Success, 100, new byte[32], new PingOptions());
            _pingWrapperMock.Setup(x => x.SendPingAsync(hostAddress))
                           .ReturnsAsync(reply);

            // Act
            var result = await _pingService.PingHostAsync(hostAddress);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(IPStatus.Success, result.Status);
            Assert.AreEqual(100, result.RoundtripTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PingHost_NullHost_ThrowsArgumentNullException()
        {
            // Act
            await _pingService.PingHostAsync(null);
        }

        [TestMethod]
        public async Task PingHost_NetworkError_ReturnsFailedResult()
        {
            // Arrange
            var hostAddress = "invalid.host";
            _pingWrapperMock.Setup(x => x.SendPingAsync(hostAddress))
                           .ThrowsAsync(new PingException("Network error"));

            // Act
            var result = await _pingService.PingHostAsync(hostAddress);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(IPStatus.Unknown, result.Status);
            Assert.IsTrue(result.RoundtripTime < 0);
        }
    }
}
```