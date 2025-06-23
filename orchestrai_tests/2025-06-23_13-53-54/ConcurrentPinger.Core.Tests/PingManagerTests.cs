```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ConcurrentPinger.Core.Services;
using ConcurrentPinger.Core.Models;

namespace ConcurrentPinger.Core.Tests
{
    [TestClass]
    public class PingManagerTests
    {
        private Mock<IPingService> _pingServiceMock;
        private PingManager _pingManager;

        [TestInitialize]
        public void Setup()
        {
            _pingServiceMock = new Mock<IPingService>();
            _pingManager = new PingManager(_pingServiceMock.Object);
        }

        [TestMethod]
        public async Task PingMultipleHosts_ValidHosts_ReturnsAllResults()
        {
            // Arrange
            var hosts = new[] { "8.8.8.8", "8.8.4.4" };
            _pingServiceMock.Setup(x => x.PingHostAsync(It.IsAny<string>()))
                           .ReturnsAsync(new PingResult { Status = System.Net.NetworkInformation.IPStatus.Success });

            // Act
            var results = await _pingManager.PingHostsAsync(hosts);

            // Assert
            Assert.AreEqual(hosts.Length, results.Count());
            Assert.IsTrue(results.All(r => r.Status == System.Net.NetworkInformation.IPStatus.Success));
        }

        [TestMethod]
        public async Task PingMultipleHosts_EmptyHostList_ReturnsEmptyResults()
        {
            // Arrange
            var hosts = Array.Empty<string>();

            // Act
            var results = await _pingManager.PingHostsAsync(hosts);

            // Assert
            Assert.AreEqual(0, results.Count());
        }

        [TestMethod]
        public async Task PingMultipleHosts_MixedResults_ReturnsAllAttempts()
        {
            // Arrange
            var hosts = new[] { "success.host", "failed.host" };
            _pingServiceMock.Setup(x => x.PingHostAsync("success.host"))
                           .ReturnsAsync(new PingResult { Status = System.Net.NetworkInformation.IPStatus.Success });
            _pingServiceMock.Setup(x => x.PingHostAsync("failed.host"))
                           .ReturnsAsync(new PingResult { Status = System.Net.NetworkInformation.IPStatus.TimedOut });

            // Act
            var results = await _pingManager.PingHostsAsync(hosts);

            // Assert
            Assert.AreEqual(2, results.Count());
            Assert.IsTrue(results.Any(r => r.Status == System.Net.NetworkInformation.IPStatus.Success));
            Assert.IsTrue(results.Any(r => r.Status == System.Net.NetworkInformation.IPStatus.TimedOut));
        }

        [TestMethod]
        public async Task PingMultipleHosts_ConcurrentExecution_CompletesAllPings()
        {
            // Arrange
            var hosts = Enumerable.Range(0, 100).Select(i => $"host{i}").ToArray();
            _pingServiceMock.Setup(x => x.PingHostAsync(It.IsAny<string>()))
                           .ReturnsAsync(new PingResult { Status = System.Net.NetworkInformation.IPStatus.Success });

            // Act
            var results = await _pingManager.PingHostsAsync(hosts);

            // Assert
            Assert.AreEqual(hosts.Length, results.Count());
        }
    }
}
```