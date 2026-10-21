using CazadorDescuentos.Modules.IngestionAndAI.Services;
using CazadorDescuentos.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CazadorDescuentos.Modules.IngestionAndAI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IScraperService _scraperService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(
        IScraperService scraperService,
        ApplicationDbContext context,
        ILogger<IngestionController> logger)
    {
        _scraperService = scraperService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("trigger-scrape")]
    public async Task<IActionResult> TriggerScrapeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual scrape triggered via API.");
        var count = await _scraperService.ScrapeAndProcessAllAsync(cancellationToken);
        return Ok(new
        {
            success = true,
            message = $"Extracción y procesamiento completado. Nuevas/actualizadas ofertas: {count}",
            processedOffers = count
        });
    }

    [HttpGet("scrapes")]
    public async Task<IActionResult> GetScrapesAsync(CancellationToken cancellationToken)
    {
        var scrapes = await _context.RawScrapes
            .OrderByDescending(s => s.ScrapedAt)
            .Take(20)
            .Select(s => new
            {
                s.Id,
                s.Brand,
                s.SourceUrl,
                s.ScrapedAt,
                s.IsProcessed,
                ContentPreview = s.RawContent.Length > 200 ? s.RawContent.Substring(0, 200) + "..." : s.RawContent
            })
            .ToListAsync(cancellationToken);

        return Ok(scrapes);
    }
}
