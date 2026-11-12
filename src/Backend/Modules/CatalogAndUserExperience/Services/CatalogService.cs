using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Services;

public class CatalogService : ICatalogService
{
    private readonly ApplicationDbContext _context;

    public CatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OfferResponseDto>> GetActiveOffersAsync(
        OfferFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Offers
            .AsNoTracking()
            .Where(o => o.IsActive);

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Brand) && !filter.Brand.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.Brand == filter.Brand);
            }

            if (!string.IsNullOrWhiteSpace(filter.BenefitType))
            {
                query = query.Where(o => o.BenefitType == filter.BenefitType);
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(o => o.Category == filter.Category);
            }

            if (!string.IsNullOrWhiteSpace(filter.Channel))
            {
                query = query.Where(o => o.Channel == filter.Channel);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var search = filter.SearchQuery.Trim();
                query = query.Where(o => o.Title.Contains(search) ||
                                         o.Description.Contains(search) ||
                                         o.Brand.Contains(search) ||
                                         (o.PromoCode != null && o.PromoCode.Contains(search)));
            }

            query = filter.SortBy switch
            {
                "price_asc" => query.OrderBy(o => o.NormalizedPrice),
                "price_desc" => query.OrderByDescending(o => o.NormalizedPrice),
                "brand" => query.OrderBy(o => o.Brand).ThenBy(o => o.NormalizedPrice),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };
        }
        else
        {
            query = query.OrderBy(o => o.NormalizedPrice);
        }

        // Direct projection using .Select to avoid AutoMapper
        return await query.Select(o => new OfferResponseDto
        {
            Id = o.Id,
            Brand = o.Brand,
            Title = o.Title,
            Description = o.Description,
            NormalizedPrice = o.NormalizedPrice,
            OriginalPrice = o.OriginalPrice,
            BenefitType = o.BenefitType,
            BenefitDescription = o.BenefitDescription,
            Category = o.Category,
            PromoCode = o.PromoCode,
            Channel = o.Channel,
            DestinationUrl = o.DestinationUrl,
            ImageUrl = o.ImageUrl,
            TermsAndConditions = o.TermsAndConditions,
            ValidUntil = o.ValidUntil,
            IsActive = o.IsActive,
            ReportCount = o.ReportCount
        }).ToListAsync(cancellationToken);
    }

    public async Task<OfferResponseDto?> GetOfferByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OfferResponseDto
            {
                Id = o.Id,
                Brand = o.Brand,
                Title = o.Title,
                Description = o.Description,
                NormalizedPrice = o.NormalizedPrice,
                OriginalPrice = o.OriginalPrice,
                BenefitType = o.BenefitType,
                BenefitDescription = o.BenefitDescription,
                Category = o.Category,
                PromoCode = o.PromoCode,
                Channel = o.Channel,
                DestinationUrl = o.DestinationUrl,
                ImageUrl = o.ImageUrl,
                TermsAndConditions = o.TermsAndConditions,
                ValidUntil = o.ValidUntil,
                IsActive = o.IsActive,
                ReportCount = o.ReportCount
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
