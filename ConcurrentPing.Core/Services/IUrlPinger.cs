using ConcurrentPinger.Core.Models;

namespace ConcurrentPinger.Core.Services;

public interface IUrlPinger
{
    Task<UrlPingResult> PingAsync(string url);
    Task<List<UrlPingResult>> PingManyAsync(IEnumerable<string> urls, int maxConcurrency = 5);
}