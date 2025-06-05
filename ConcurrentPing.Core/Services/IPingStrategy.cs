using ConcurrentPinger.Core.Models;

namespace ConcurrentPinger.Core.Services;

public interface IPingStrategy
{
    Task<UrlPingResult> PingAsync(string url);
}