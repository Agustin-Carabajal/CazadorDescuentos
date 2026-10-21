using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Models;

[Table("Offers")]
public class Offer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int NormalizedPrice { get; set; }

    public int? OriginalPrice { get; set; }

    [Required]
    [MaxLength(100)]
    public string BenefitType { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? BenefitDescription { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "Combo";

    [MaxLength(100)]
    public string? PromoCode { get; set; }

    [Required]
    [MaxLength(50)]
    public string Channel { get; set; } = "App / Web";

    [Required]
    [MaxLength(500)]
    public string DestinationUrl { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? TermsAndConditions { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    public DateTime ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;

    public int ReportCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserReport> UserReports { get; set; } = new List<UserReport>();
}
