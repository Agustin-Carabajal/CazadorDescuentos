using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CazadorDescuentos.Modules.IngestionAndAI.Models;

[Table("RawScrapes")]
public class RawScrape
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string SourceUrl { get; set; } = string.Empty;

    [Required]
    public string RawContent { get; set; } = string.Empty;

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    public bool IsProcessed { get; set; } = false;

    public string? ExtractedJson { get; set; }
}
