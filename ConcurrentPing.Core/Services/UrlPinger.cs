using ConcurrentPinger.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ConcurrentPinger.Core.Services;

public class UrlPinger(HttpClient httpClient, ILogger<UrlPinger> logger)
{
    public async Task<UrlPingResult> PingAsync(string url)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var response = await httpClient.GetAsync(url);
            sw.Stop();

            logger.LogInformation("Pinged {Url} in {Time}ms (Status: {StatusCode})", url, sw.ElapsedMilliseconds, response.StatusCode);

            return new UrlPingResult(url, sw.ElapsedMilliseconds, response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ping {Url}", url);
            return new UrlPingResult(url, 0, false, ex.Message);
        }
    }

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
