namespace CazadorDescuentos.Features.Ingestion;

public class BrandExtractionResult
{
    public string BrandId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool TimedOut { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> CapturedFiles { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class BatchExtractionResult
{
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; }
    public List<BrandExtractionResult> Results { get; set; } = new();
    public bool AllSucceeded => Results.Count > 0 && Results.All(r => r.Success);
    public int TotalCaptures => Results.Sum(r => r.CapturedFiles.Count);
}
