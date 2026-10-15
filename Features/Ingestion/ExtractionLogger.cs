using System.Text;

namespace CazadorDescuentos.Features.Ingestion;

public class ExtractionLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public ExtractionLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);
    public void LogError(string message, Exception? ex = null)
    {
        var sb = new StringBuilder();
        sb.Append(message);
        if (ex != null)
        {
            sb.Append($" | Excepción: {ex.GetType().Name}: {ex.Message}");
        }
        WriteLog("ERROR", sb.ToString());
    }

    private void WriteLog(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logLine = $"[{timestamp}] [{level}] {message}";

        Console.WriteLine(logLine);

        lock (_lock)
        {
            try
            {
                EnsureDirectoryExists();
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ExtractionLogger] No se pudo escribir en el archivo de log: {ex.Message}");
            }
        }
    }
}
