namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public interface INormalizationService
{
    int NormalizePrice(string? rawPrice);
    DateTime NormalizeDate(string? rawDate, DateTime? referenceDate = null);
    string NormalizeName(string? rawName);
}
