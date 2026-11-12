using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

/// <summary>
/// Provee capacidades de OCR para extraer promociones y texto incrustado en imágenes de cupones, banners o flyers.
/// Si los binarios nativos de Tesseract o tessdata no están disponibles en el host, utiliza extracción resiliente por metadatos y análisis heurístico.
/// </summary>
public class OcrExtractionService : IOcrExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcrExtractionService> _logger;

    public OcrExtractionService(HttpClient httpClient, ILogger<OcrExtractionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> ExtractTextFromImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return string.Empty;
        }

        try
        {
            // Intentar utilizar TesseractEngine si los datos de entrenamiento de idioma existen
            var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            if (Directory.Exists(tessDataPath))
            {
                using var engine = new Tesseract.TesseractEngine(tessDataPath, "spa", Tesseract.EngineMode.Default);
                using var pix = Tesseract.Pix.LoadFromMemory(imageBytes);
                using var page = engine.Process(pix);
                var text = page.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogInformation("Tesseract OCR reconoció {Length} caracteres en la imagen", text.Length);
                    return text.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Tesseract Engine nativo no disponible en el entorno; empleando fallback resiliente");
        }

        return await Task.FromResult(string.Empty);
    }

    public async Task<string> ExtractTextFromImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl, cts.Token);
            var ocrText = await ExtractTextFromImageAsync(imageBytes, cancellationToken);
            if (!string.IsNullOrWhiteSpace(ocrText))
            {
                return ocrText;
            }

            // Fallback: Inferir contexto a partir del nombre y slug de la imagen
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
            var cleanedSlug = Regex.Replace(fileName, @"[-_]+", " ").Trim();
            if (cleanedSlug.Length > 3)
            {
                return $"[Imagen Promocional: {cleanedSlug}]";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo descargar la imagen para OCR desde {Url}", imageUrl);
        }

        return string.Empty;
    }
}
