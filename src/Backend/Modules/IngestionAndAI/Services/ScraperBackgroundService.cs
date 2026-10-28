using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class ScraperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScraperBackgroundService> _logger;
    private readonly TimeSpan _scrapeInterval = TimeSpan.FromMinutes(5);

    public static DateTime LastRunTimeUtc { get; private set; } = DateTime.UtcNow;
    public static DateTime NextRunTimeUtc { get; private set; } = DateTime.UtcNow.AddMinutes(5);
    public static bool IsCurrentlyRunning { get; private set; } = false;

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
                IsCurrentlyRunning = true;
                _logger.LogInformation("Scheduled scraping execution started (5-minute cycle).");
                using var scope = _scopeFactory.CreateScope();
                var scraperService = scope.ServiceProvider.GetRequiredService<IScraperService>();
                var count = await scraperService.ScrapeAndProcessAllAsync(stoppingToken);
                LastRunTimeUtc = DateTime.UtcNow;
                NextRunTimeUtc = DateTime.UtcNow.Add(_scrapeInterval);
                _logger.LogInformation("Scheduled scraping executed successfully. Processed {Count} offers. Next run at: {NextRun}", count, NextRunTimeUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during scheduled scraping execution.");
            }
            finally
            {
                IsCurrentlyRunning = false;
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
