namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public record BenefitClassificationResult(
    string BenefitType,
    string BenefitDescription,
    int? DiscountPercentage
);

public interface IBenefitClassificationService
{
    BenefitClassificationResult ClassifyBenefit(string text, int? normalizedPrice = null, int? originalPrice = null);
}
