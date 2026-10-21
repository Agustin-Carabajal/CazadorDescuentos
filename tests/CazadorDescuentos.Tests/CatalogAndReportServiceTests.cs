using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Modules.CatalogAndUserExperience.Services;
using CazadorDescuentos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CazadorDescuentos.Tests;

public class CatalogAndReportServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CatalogService_ShouldFilterByBrandAndSortByPrice()
    {
        var context = CreateInMemoryDbContext();

        var mcdonalds = new Offer
        {
            Brand = "McDonald's",
            Title = "Cuarto de Libra",
            NormalizedPrice = 11000,
            BenefitType = "FixedAmount",
            Category = "Hamburguesa",
            Channel = "App",
            DestinationUrl = "https://mcdonalds.com.ar",
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsActive = true
        };

        var bk1 = new Offer
        {
            Brand = "Burger King",
            Title = "Stacker Doble",
            NormalizedPrice = 12000,
            BenefitType = "FixedAmount",
            Category = "Hamburguesa",
            Channel = "App",
            DestinationUrl = "https://bk.com.ar",
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsActive = true
        };

        var bk2 = new Offer
        {
            Brand = "Burger King",
            Title = "Whopper Jr",
            NormalizedPrice = 6000,
            BenefitType = "FixedAmount",
            Category = "Hamburguesa",
            Channel = "Web",
            DestinationUrl = "https://bk.com.ar",
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsActive = true
        };

        context.Offers.AddRange(mcdonalds, bk1, bk2);
        await context.SaveChangesAsync();

        var sut = new CatalogService(context);

        // Filter by Burger King, sort by price_asc
        var result = await sut.GetActiveOffersAsync(new OfferFilterDto
        {
            Brand = "Burger King",
            SortBy = "price_asc"
        });

        Assert.Equal(2, result.Count);
        Assert.Equal("Whopper Jr", result[0].Title);
        Assert.Equal(6000, result[0].NormalizedPrice);
        Assert.Equal("Stacker Doble", result[1].Title);
        Assert.Equal(12000, result[1].NormalizedPrice);
    }

    [Fact]
    public async Task ReportService_ShouldRecordUserReportAndUpdateOffer()
    {
        var context = CreateInMemoryDbContext();

        var offer = new Offer
        {
            Brand = "Mostaza",
            Title = "Mega Dibu",
            NormalizedPrice = 14000,
            BenefitType = "FixedAmount",
            Category = "Hamburguesa",
            Channel = "App",
            DestinationUrl = "https://mostazaweb.com.ar",
            ValidUntil = DateTime.UtcNow.AddDays(5),
            IsActive = true,
            ReportCount = 0
        };

        context.Offers.Add(offer);
        await context.SaveChangesAsync();

        var sut = new ReportService(context, NullLogger<ReportService>.Instance);

        var report = await sut.CreateReportAsync(new CreateReportDto
        {
            OfferId = offer.Id,
            Reason = "Expired",
            Comments = "Fui a la sucursal y el cupón ya no tenía validez."
        }, reporterIp: "127.0.0.1");

        Assert.NotNull(report);
        Assert.Equal(offer.Id, report.OfferId);
        Assert.Equal("Expired", report.Reason);

        var updatedOffer = await context.Offers.FindAsync(offer.Id);
        Assert.NotNull(updatedOffer);
        Assert.Equal(1, updatedOffer.ReportCount);

        var reports = await sut.GetRecentReportsAsync();
        Assert.Single(reports);
        Assert.Equal("Expired", reports[0].Reason);
    }
}
