namespace CazadorDescuentos.Features.Ingestion;

public class BrandSpec
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string? ActivityName { get; set; }
    public int ScrollCount { get; set; } = 3;
}

public class BrandExtractorConfig
{
    public string AdbPath { get; set; } = "adb";
    public string? AndroidSerial { get; set; }
    public string OutputDirectory { get; set; } = "capturas";
    public int TimeoutSeconds { get; set; } = 30;
    public int ScrollCount { get; set; } = 3;
    public string LogFilePath { get; set; } = Path.Combine("logs", "ingestion.log");
    public bool SimulateMode { get; set; } = false;

    public List<BrandSpec> Brands { get; set; } = new()
    {
        new BrandSpec
        {
            Id = "mcdonalds",
            DisplayName = "McDonald's",
            PackageName = "com.mcdonalds.app",
            ScrollCount = 3
        },
        new BrandSpec
        {
            Id = "burgerking",
            DisplayName = "Burger King",
            PackageName = "ar.com.burgerking",
            ScrollCount = 3
        },
        new BrandSpec
        {
            Id = "mostaza",
            DisplayName = "Mostaza",
            PackageName = "com.mostaza.mostaza",
            ScrollCount = 3
        }
    };

    public static BrandExtractorConfig LoadFromEnvironment(string? envFilePath = null)
    {
        var config = new BrandExtractorConfig();

        var path = envFilePath ?? Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(path))
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim();

                switch (key.ToUpperInvariant())
                {
                    case "ADB_PATH" when !string.IsNullOrEmpty(value):
                        config.AdbPath = value;
                        break;
                    case "ANDROID_SERIAL" when !string.IsNullOrEmpty(value):
                        config.AndroidSerial = value;
                        break;
                    case "OUTPUT_DIRECTORY" when !string.IsNullOrEmpty(value):
                        config.OutputDirectory = value;
                        break;
                    case "TIMEOUT_SECONDS" when int.TryParse(value, out var timeout):
                        config.TimeoutSeconds = timeout;
                        break;
                    case "SCROLL_COUNT" when int.TryParse(value, out var scroll):
                        config.ScrollCount = scroll;
                        break;
                    case "LOG_FILE" when !string.IsNullOrEmpty(value):
                        config.LogFilePath = value;
                        break;
                    case "SIMULATE_MODE" when bool.TryParse(value, out var simulate):
                        config.SimulateMode = simulate;
                        break;
                    case "MCDONALDS_PACKAGE" when !string.IsNullOrEmpty(value):
                        var mcd = config.Brands.FirstOrDefault(b => b.Id == "mcdonalds");
                        if (mcd != null) mcd.PackageName = value;
                        break;
                    case "BURGERKING_PACKAGE" when !string.IsNullOrEmpty(value):
                        var bk = config.Brands.FirstOrDefault(b => b.Id == "burgerking");
                        if (bk != null) bk.PackageName = value;
                        break;
                    case "MOSTAZA_PACKAGE" when !string.IsNullOrEmpty(value):
                        var mz = config.Brands.FirstOrDefault(b => b.Id == "mostaza");
                        if (mz != null) mz.PackageName = value;
                        break;
                }
            }
        }

        return config;
    }
}
