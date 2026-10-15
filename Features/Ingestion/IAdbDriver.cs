namespace CazadorDescuentos.Features.Ingestion;

public interface IAdbDriver
{
    Task<bool> IsDeviceAvailableAsync(string? serial, CancellationToken cancellationToken = default);
    Task<bool> LaunchAppAsync(string packageName, string? activityName, string? serial, CancellationToken cancellationToken = default);
    Task<bool> ForceStopAppAsync(string packageName, string? serial, CancellationToken cancellationToken = default);
    Task<bool> PerformScrollAsync(int startX, int startY, int endX, int endY, int durationMs, string? serial, CancellationToken cancellationToken = default);
    Task<byte[]> CaptureScreenshotAsync(string? serial, CancellationToken cancellationToken = default);
    Task<string> ExecuteAdbCommandAsync(string arguments, CancellationToken cancellationToken = default);
}
