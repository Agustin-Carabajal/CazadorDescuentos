namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public interface IOcrExtractionService
{
    Task<string> ExtractTextFromImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
    Task<string> ExtractTextFromImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
}
