using ConcurrentPinger.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var urls = new[]
{
    "https://google.com",
    "https://github.com",
    "https://microsoft.com",
    "https://stackoverflow.com",
    "https://nonexistent.site"
};

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient<HttpPingStrategy>();
        services.AddSingleton<IPingStrategy, HttpPingStrategy>();
        services.AddSingleton<IUrlPinger, UrlPinger>();
        services.AddLogging(config => config.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss ";
        }));
    })
    .Build();

using var scope = host.Services.CreateScope();
var pinger = scope.ServiceProvider.GetRequiredService<IUrlPinger>();
var results = await pinger.PingManyAsync(urls);

Console.WriteLine("\n--- Summary ---");
foreach (var result in results)
{
    Console.WriteLine($"{result.Url} -> {(result.Success ? "OK" : "FAIL")} in {result.ResponseTimeMs}ms ({result.ErrorMessage ?? "No error"})");
}
