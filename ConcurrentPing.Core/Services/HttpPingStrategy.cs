using ConcurrentPinger.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ConcurrentPinger.Core.Services;

public class HttpPingStrategy(HttpClient httpClient, ILogger<HttpPingStrategy> logger) : IPingStrategy
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
}