using CazadorDescuentos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class OfferCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OfferCleanupBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public OfferCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OfferCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OfferCleanupBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanExpiredOffersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up expired offers.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("OfferCleanupBackgroundService is stopping.");
    }

    public async Task<int> CleanExpiredOffersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var expiredOffers = await context.Offers
            .Where(o => o.IsActive && o.ValidUntil < now)
            .ToListAsync(cancellationToken);

        if (expiredOffers.Count > 0)
        {
            foreach (var offer in expiredOffers)
            {
                offer.IsActive = false;
                offer.UpdatedAt = now;
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully deactivated {Count} expired offers exceeding TTL.", expiredOffers.Count);
        }
        else
        {
            _logger.LogInformation("TTL evaluation complete: No expired active offers found.");
        }

        return expiredOffers.Count;
    }
}
