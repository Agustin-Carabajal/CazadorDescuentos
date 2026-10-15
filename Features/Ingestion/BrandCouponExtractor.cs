using System.Diagnostics;

namespace CazadorDescuentos.Features.Ingestion;

public class BrandCouponExtractor
{
    private readonly IAdbDriver _driver;
    private readonly ExtractionLogger? _customLogger;

    public BrandCouponExtractor(IAdbDriver driver, ExtractionLogger? logger = null)
    {
        _driver = driver;
        _customLogger = logger;
    }

    public async Task<BatchExtractionResult> ExtractAllBrandsAsync(BrandExtractorConfig config, CancellationToken cancellationToken = default)
    {
        var logger = _customLogger ?? new ExtractionLogger(config.LogFilePath);
        var batchResult = new BatchExtractionResult
        {
            StartTime = DateTime.UtcNow
        };

        logger.LogInfo($"Iniciando ciclo de extracción programada de cupones para {config.Brands.Count} marcas.");

        // Ensure output directory exists
        if (!Directory.Exists(config.OutputDirectory))
        {
            Directory.CreateDirectory(config.OutputDirectory);
            logger.LogInfo($"Directorio de salida creado en: {Path.GetFullPath(config.OutputDirectory)}");
        }

        // Verify device readiness if not in simulated mode
        if (!config.SimulateMode)
        {
            var isAvailable = await _driver.IsDeviceAvailableAsync(config.AndroidSerial, cancellationToken);
            if (!isAvailable)
            {
                var errorMsg = $"No se encontró un emulador o dispositivo Android activo mediante ADB. Configurado: serial='{config.AndroidSerial ?? "cualquiera"}'.";
                logger.LogError(errorMsg);
                // Return result with device error for all brands without crashing the application
                foreach (var brand in config.Brands)
                {
                    batchResult.Results.Add(new BrandExtractionResult
                    {
                        BrandId = brand.Id.ToLowerInvariant(),
                        DisplayName = brand.DisplayName,
                        Success = false,
                        ErrorMessage = errorMsg
                    });
                }
                batchResult.EndTime = DateTime.UtcNow;
                return batchResult;
            }
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        foreach (var brand in config.Brands)
        {
            var brandResult = await ExtractBrandWithTimeoutAsync(brand, config, timestamp, logger, cancellationToken);
            batchResult.Results.Add(brandResult);
        }

        batchResult.EndTime = DateTime.UtcNow;
        var totalCaptured = batchResult.TotalCaptures;
        var successes = batchResult.Results.Count(r => r.Success);
        var failures = batchResult.Results.Count(r => !r.Success);

        logger.LogInfo($"Lote de extracción finalizado. Éxitos: {successes}, Fallos: {failures}, Capturas generadas: {totalCaptured}.");

        return batchResult;
    }

    private async Task<BrandExtractionResult> ExtractBrandWithTimeoutAsync(
        BrandSpec brand,
        BrandExtractorConfig config,
        string timestamp,
        ExtractionLogger logger,
        CancellationToken parentToken)
    {
        var brandId = brand.Id.ToLowerInvariant();
        var result = new BrandExtractionResult
        {
            BrandId = brandId,
            DisplayName = brand.DisplayName
        };

        var stopwatch = Stopwatch.StartNew();

        // 30 seconds timeout per brand as required by Historia 1
        var timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

        try
        {
            logger.LogInfo($"[{brand.DisplayName}] Iniciando apertura y navegación de la app (paquete: {brand.PackageName}). Timeout: {config.TimeoutSeconds}s.");

            // 1. Launch application
            await _driver.LaunchAppAsync(brand.PackageName, brand.ActivityName, config.AndroidSerial, linkedCts.Token);

            // Wait a brief moment for the promotions/coupons view to load
            await Task.Delay(2000, linkedCts.Token);

            var scrollCount = Math.Max(1, brand.ScrollCount > 0 ? brand.ScrollCount : config.ScrollCount);

            // 2. Perform sequential capture and scroll
            for (int i = 1; i <= scrollCount; i++)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                // Format: {marca}_{timestamp}_{indice}.png
                var filename = $"{brandId}_{timestamp}_{i}.png";
                var destinationPath = Path.Combine(config.OutputDirectory, filename);

                logger.LogInfo($"[{brand.DisplayName}] Tomando captura de pantalla {i}/{scrollCount}: {filename}");
                var screenshotBytes = await _driver.CaptureScreenshotAsync(config.AndroidSerial, linkedCts.Token);

                await File.WriteAllBytesAsync(destinationPath, screenshotBytes, linkedCts.Token);
                result.CapturedFiles.Add(destinationPath);

                if (i < scrollCount)
                {
                    logger.LogInfo($"[{brand.DisplayName}] Realizando scroll vertical en la pestaña de cupones ({i}/{scrollCount})...");
                    // Standard vertical scroll swipe on 1080x1920 or scaled
                    await _driver.PerformScrollAsync(500, 1500, 500, 500, 400, config.AndroidSerial, linkedCts.Token);
                    await Task.Delay(1000, linkedCts.Token);
                }
            }

            // Close / stop app to free emulator memory
            try
            {
                await _driver.ForceStopAppAsync(brand.PackageName, config.AndroidSerial, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[{brand.DisplayName}] No se pudo detener la app tras la captura: {ex.Message}");
            }

            result.Success = true;
            logger.LogInfo($"[{brand.DisplayName}] Extracción completada exitosamente con {result.CapturedFiles.Count} capturas generadas.");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !parentToken.IsCancellationRequested)
        {
            // Acceptance criterion: Si una aplicación no responde o se cuelga (timeout de 30 segundos),
            // el script registra el error en un log y continúa automáticamente con la siguiente marca.
            result.TimedOut = true;
            result.Success = false;
            result.ErrorMessage = $"Timeout de {config.TimeoutSeconds}s excedido: la aplicación de '{brand.DisplayName}' no respondió o se colgó.";

            logger.LogError($"[TIMEOUT] [{brand.DisplayName}] {result.ErrorMessage}. Continuando automáticamente con la siguiente marca (Aislamiento de lote).");

            // Attempt to force stop hung app
            try
            {
                await _driver.ForceStopAppAsync(brand.PackageName, config.AndroidSerial, CancellationToken.None);
            }
            catch
            {
                // Best effort
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;

            logger.LogError($"[{brand.DisplayName}] Error durante la extracción: {ex.Message}. Continuando con la siguiente marca.", ex);

            try
            {
                await _driver.ForceStopAppAsync(brand.PackageName, config.AndroidSerial, CancellationToken.None);
            }
            catch
            {
                // Best effort
            }
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }
}
