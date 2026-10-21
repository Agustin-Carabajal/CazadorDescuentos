using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Models;

[Table("UserReports")]
public class UserReport
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OfferId { get; set; }

    [ForeignKey(nameof(OfferId))]
    public Offer? Offer { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Comments { get; set; }

    [MaxLength(50)]
    public string? ReporterIp { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public bool IsResolved { get; set; } = false;
}
