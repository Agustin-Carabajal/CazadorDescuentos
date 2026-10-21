using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Modules.IngestionAndAI.Services;
using CazadorDescuentos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CazadorDescuentos.Tests;

public class OfferCleanupServiceTests
{
    [Fact]
    public async Task CleanExpiredOffersAsync_ShouldDeactivateExpiredOffers()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        int activeOfferId;
        int expiredOfferId;

        // Seed 1 active unexpired offer, and 1 active expired offer
        using (var seedScope = scopeFactory.CreateScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var activeOffer = new Offer
            {
                Brand = "Burger King",
                Title = "Whopper Jr",
                NormalizedPrice = 5000,
                BenefitType = "FixedAmount",
                Category = "Hamburguesa",
                Channel = "App",
                DestinationUrl = "https://bk.com.ar",
                ValidUntil = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var expiredOffer = new Offer
            {
                Brand = "McDonald's",
                Title = "Big Mac Expirado",
                NormalizedPrice = 8000,
                BenefitType = "FixedAmount",
                Category = "Hamburguesa",
                Channel = "Web",
                DestinationUrl = "https://mcdonalds.com.ar",
                ValidUntil = DateTime.UtcNow.AddDays(-2), // Expired 2 days ago
                IsActive = true
            };

            context.Offers.AddRange(activeOffer, expiredOffer);
            await context.SaveChangesAsync();

            activeOfferId = activeOffer.Id;
            expiredOfferId = expiredOffer.Id;
        }

        var sut = new OfferCleanupBackgroundService(scopeFactory, NullLogger<OfferCleanupBackgroundService>.Instance);
        var deactivatedCount = await sut.CleanExpiredOffersAsync();

        Assert.Equal(1, deactivatedCount);

        using (var verifyScope = scopeFactory.CreateScope())
        {
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reloadedExpired = await verifyContext.Offers.FindAsync(expiredOfferId);
            var reloadedActive = await verifyContext.Offers.FindAsync(activeOfferId);

            Assert.NotNull(reloadedExpired);
            Assert.False(reloadedExpired.IsActive); // Successfully deactivated!

            Assert.NotNull(reloadedActive);
            Assert.True(reloadedActive.IsActive); // Stays active
        }
    }
}
