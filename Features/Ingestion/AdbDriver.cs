using System.Diagnostics;

namespace CazadorDescuentos.Features.Ingestion;

public class AdbDriver : IAdbDriver
{
    private readonly string _adbPath;

    public AdbDriver(string adbPath = "adb")
    {
        _adbPath = string.IsNullOrWhiteSpace(adbPath) ? "adb" : adbPath;
    }

    private string BuildTargetArgs(string? serial, string commandArgs)
    {
        return string.IsNullOrWhiteSpace(serial)
            ? commandArgs
            : $"-s {serial} {commandArgs}";
    }

    public async Task<string> ExecuteAdbCommandAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException($"ADB exited with code {process.ExitCode}: {error.Trim()}");
        }

        return output;
    }

    public async Task<bool> IsDeviceAvailableAsync(string? serial, CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await ExecuteAdbCommandAsync("devices", cancellationToken);
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // Skip "List of devices attached"
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[1].Trim().Equals("device", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(serial) || parts[0].Trim().Equals(serial, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LaunchAppAsync(string packageName, string? activityName, string? serial, CancellationToken cancellationToken = default)
    {
        string cmd;
        if (!string.IsNullOrWhiteSpace(activityName))
        {
            cmd = BuildTargetArgs(serial, $"shell am start -n {packageName}/{activityName}");
        }
        else
        {
            cmd = BuildTargetArgs(serial, $"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");
        }

        await ExecuteAdbCommandAsync(cmd, cancellationToken);
        return true;
    }

    public async Task<bool> ForceStopAppAsync(string packageName, string? serial, CancellationToken cancellationToken = default)
    {
        var cmd = BuildTargetArgs(serial, $"shell am force-stop {packageName}");
        await ExecuteAdbCommandAsync(cmd, cancellationToken);
        return true;
    }

    public async Task<bool> PerformScrollAsync(int startX, int startY, int endX, int endY, int durationMs, string? serial, CancellationToken cancellationToken = default)
    {
        var cmd = BuildTargetArgs(serial, $"shell input swipe {startX} {startY} {endX} {endY} {durationMs}");
        await ExecuteAdbCommandAsync(cmd, cancellationToken);
        return true;
    }

    public async Task<byte[]> CaptureScreenshotAsync(string? serial, CancellationToken cancellationToken = default)
    {
        var args = BuildTargetArgs(serial, "exec-out screencap -p");
        var psi = new ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var ms = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var bytes = ms.ToArray();
        if (bytes.Length > 0 && IsValidPngHeader(bytes))
        {
            return bytes;
        }

        // Fallback: capture on device and pull
        var tempRemotePath = $"/sdcard/screencap_{Guid.NewGuid():N}.png";
        var tempLocalFile = Path.GetTempFileName();
        try
        {
            await ExecuteAdbCommandAsync(BuildTargetArgs(serial, $"shell screencap -p {tempRemotePath}"), cancellationToken);
            await ExecuteAdbCommandAsync(BuildTargetArgs(serial, $"pull {tempRemotePath} \"{tempLocalFile}\""), cancellationToken);
            await ExecuteAdbCommandAsync(BuildTargetArgs(serial, $"shell rm {tempRemotePath}"), cancellationToken);

            if (File.Exists(tempLocalFile))
            {
                return await File.ReadAllBytesAsync(tempLocalFile, cancellationToken);
            }
        }
        finally
        {
            if (File.Exists(tempLocalFile))
            {
                try { File.Delete(tempLocalFile); } catch { }
            }
        }

        throw new InvalidOperationException("No se pudo obtener la captura de pantalla desde ADB.");
    }

    private static bool IsValidPngHeader(byte[] bytes)
    {
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length < 8) return false;
        return bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
            && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
    }
}
