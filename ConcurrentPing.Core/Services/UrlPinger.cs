using ConcurrentPinger.Core.Models;
using System.Collections.Concurrent;

namespace ConcurrentPinger.Core.Services;

public class UrlPinger(IPingStrategy pingStrategy) : IUrlPinger
{
    public async Task<UrlPingResult> PingAsync(string url)
        => await pingStrategy.PingAsync(url);

    public async Task<List<UrlPingResult>> PingManyAsync(IEnumerable<string> urls, int maxConcurrency = 5)
    {
        var results = new ConcurrentBag<UrlPingResult>();
        var semaphore = new SemaphoreSlim(maxConcurrency);

        await Parallel.ForEachAsync(urls, async (url, token) =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                var result = await PingAsync(url);
                results.Add(result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return [.. results];
    }
}
