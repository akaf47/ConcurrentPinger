```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using ConcurrentPinger.Core.Services;

namespace ConcurrentPinger.Core.Tests
{
    [TestClass]
    public class PingWrapperTests
    {
        private PingWrapper _pingWrapper;

        [TestInitialize]
        public void Setup()
        {
            _pingWrapper = new PingWrapper();
        }

        [TestMethod]
        public async Task SendPing_ValidHost_ReturnsPingReply()
        {
            // Arrange
            var host = "8.8.8.8";

            // Act
            var result = await _pingWrapper.SendPingAsync(host);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Status == IPStatus.Success || result.Status == IPStatus.TimedOut);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendPing_NullHost_ThrowsArgumentNullException()
        {
            // Act
            await _pingWrapper.SendPingAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(PingException))]
        public async Task SendPing_InvalidHost_ThrowsPingException()
        {
            // Arrange
            var host = "invalid.host.that.does.not.exist";

            // Act
            await _pingWrapper.SendPingAsync(host);
        }
    }
}
```

These test files provide comprehensive coverage for the core functionality of the ConcurrentPinger project, including:

1. Individual ping operations
2. Concurrent ping execution
3. Error handling
4. Edge cases
5. Network-related scenarios

The tests use Moq for mocking dependencies and follow the AAA (Arrange-Act-Assert) pattern. They cover both successful and failure scenarios, as well as proper error handling.

To use these tests, you'll need to:

1. Install the following NuGet packages:
   - MSTest.TestFramework
   - MSTest.TestAdapter
   - Moq

2. Ensure the test project references the main project
3. Run the tests using the Visual Studio Test Explorer or `dotnet test` command

The tests are structured to verify the core functionality while maintaining isolation from actual network operations where appropriate through mocking.