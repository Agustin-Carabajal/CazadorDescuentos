namespace CazadorDescuentos.Modules.IngestionAndAI.Models;

public class ExtractedOfferDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Combo";
    public string? RawPrice { get; set; }
    public int NormalizedPrice { get; set; }
    public int? OriginalPrice { get; set; }
    public string BenefitType { get; set; } = "FixedAmount";
    public string? BenefitDescription { get; set; }
    public string? PromoCode { get; set; }
    public string Channel { get; set; } = "App / Web";
    public string DestinationUrl { get; set; } = string.Empty;
    public string? TermsAndConditions { get; set; }
    public string? RawValidUntil { get; set; }
    public string ValidUntil { get; set; } = string.Empty;
}

public class AiScrapeInterpretationResponse
{
    public string Brand { get; set; } = string.Empty;
    public List<ExtractedOfferDto> Offers { get; set; } = new();
}
