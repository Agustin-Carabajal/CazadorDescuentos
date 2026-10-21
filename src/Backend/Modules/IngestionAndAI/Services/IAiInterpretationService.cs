using CazadorDescuentos.Modules.IngestionAndAI.Models;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public interface IAiInterpretationService
{
    Task<AiScrapeInterpretationResponse> InterpretRawContentAsync(string brand, string sourceUrl, string rawContent, CancellationToken cancellationToken = default);
}
