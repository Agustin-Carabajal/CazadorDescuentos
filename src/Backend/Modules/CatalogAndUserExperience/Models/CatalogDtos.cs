using System.ComponentModel.DataAnnotations;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Models;

public class OfferFilterDto
{
    public string? Brand { get; set; }
    public string? BenefitType { get; set; }
    public string? Category { get; set; }
    public string? Channel { get; set; }
    public string? SearchQuery { get; set; }
    public string? SortBy { get; set; } // "price_asc", "price_desc", "newest"
}

public class OfferResponseDto
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int NormalizedPrice { get; set; }
    public int? OriginalPrice { get; set; }
    public string BenefitType { get; set; } = string.Empty;
    public string? BenefitDescription { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? TermsAndConditions { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; }
    public int ReportCount { get; set; }
}

public class CreateReportDto
{
    [Required]
    public int OfferId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Comments { get; set; }
}
