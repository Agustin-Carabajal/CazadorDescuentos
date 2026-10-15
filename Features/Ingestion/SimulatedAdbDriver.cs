namespace CazadorDescuentos.Features.Ingestion;

public class SimulatedAdbDriver : IAdbDriver
{
    public string? HangOnBrandPackage { get; set; }
    public TimeSpan HangDelay { get; set; } = TimeSpan.FromSeconds(35);
    public bool ShouldFailLaunch { get; set; } = false;
    public List<string> ExecutedCommands { get; } = new();

    // 1x1 minimal valid PNG byte array
    private static readonly byte[] MinimalValidPng = new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D,                         // IHDR chunk length (13 bytes)
        0x49, 0x48, 0x44, 0x52,                         // "IHDR"
        0x00, 0x00, 0x00, 0x64,                         // Width: 100
        0x00, 0x00, 0x00, 0x64,                         // Height: 100
        0x08, 0x02, 0x00, 0x00, 0x00,                   // 8-bit truecolor
        0x5B, 0xDE, 0xB3, 0xA9,                         // CRC
        0x00, 0x00, 0x00, 0x0C,                         // IDAT chunk length
        0x49, 0x44, 0x41, 0x54,                         // "IDAT"
        0x78, 0x9C, 0x63, 0x60, 0x00, 0x00, 0x00, 0x02,
        0x00, 0x01, 0xE2, 0x21,                         // Deflated data & check
        0xBC, 0x33,                                     // CRC
        0x00, 0x00, 0x00, 0x00,                         // IEND chunk length
        0x49, 0x45, 0x4E, 0x44,                         // "IEND"
        0xAE, 0x42, 0x60, 0x82                          // CRC
    };

    public Task<bool> IsDeviceAvailableAsync(string? serial, CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add($"is_device_available:{serial ?? "default"}");
        return Task.FromResult(true);
    }

    public async Task<bool> LaunchAppAsync(string packageName, string? activityName, string? serial, CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add($"launch_app:{packageName}/{activityName ?? "main"}");

        if (ShouldFailLaunch)
        {
            throw new InvalidOperationException($"Error simulado al iniciar la aplicación {packageName}.");
        }

        if (!string.IsNullOrEmpty(HangOnBrandPackage) && packageName.Equals(HangOnBrandPackage, StringComparison.OrdinalIgnoreCase))
        {
            // Simulate hung app: wait beyond timeout or until cancellation
            await Task.Delay(HangDelay, cancellationToken);
        }

        return true;
    }

    public Task<bool> ForceStopAppAsync(string packageName, string? serial, CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add($"force_stop:{packageName}");
        return Task.FromResult(true);
    }

    public Task<bool> PerformScrollAsync(int startX, int startY, int endX, int endY, int durationMs, string? serial, CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add($"scroll:{startX},{startY}->{endX},{endY}:{durationMs}ms");
        return Task.FromResult(true);
    }

    public Task<byte[]> CaptureScreenshotAsync(string? serial, CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add("capture_screenshot");
        return Task.FromResult(MinimalValidPng);
    }

    public Task<string> ExecuteAdbCommandAsync(string arguments, CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add($"exec:{arguments}");
        return Task.FromResult("simulated_output");
    }
}
