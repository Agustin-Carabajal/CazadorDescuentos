using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class ScraperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScraperBackgroundService> _logger;
    private readonly TimeSpan _scrapeInterval = TimeSpan.FromHours(4);

    public ScraperBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScraperBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScraperBackgroundService is starting.");

        // Initial brief pause so the web server can boot up first
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Scheduled scraping execution started.");
                using var scope = _scopeFactory.CreateScope();
                var scraperService = scope.ServiceProvider.GetRequiredService<IScraperService>();
                var count = await scraperService.ScrapeAndProcessAllAsync(stoppingToken);
                _logger.LogInformation("Scheduled scraping executed successfully. Processed {Count} offers.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during scheduled scraping execution.");
            }

            try
            {
                await Task.Delay(_scrapeInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ScraperBackgroundService is stopping.");
    }
}
