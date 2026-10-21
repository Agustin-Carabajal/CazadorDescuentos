using CazadorDescuentos.Modules.IngestionAndAI.Models;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public record ScrapeTarget(string Brand, string Url);

public interface IScraperService
{
    IReadOnlyList<ScrapeTarget> GetDefaultTargets();
    Task<RawScrape> ScrapeBrandAsync(ScrapeTarget target, CancellationToken cancellationToken = default);
    Task<int> ScrapeAndProcessAllAsync(CancellationToken cancellationToken = default);
}
