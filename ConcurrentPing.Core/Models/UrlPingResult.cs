namespace ConcurrentPinger.Core.Models;

public record UrlPingResult(string Url, long ResponseTimeMs, bool Success, string? ErrorMessage = null);
