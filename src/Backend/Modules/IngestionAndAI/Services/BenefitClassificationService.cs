using System.Text.RegularExpressions;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class BenefitClassificationService : IBenefitClassificationService
{
    private static readonly Regex PercentageRegex = new(@"(\d{1,2})\s*%\s*(?:off|descuento|de descuento)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TwoForOneRegex = new(@"(2\s*x\s*1|2por1|dos\s*por\s*uno|segunda\s*unidad\s*gratis)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FreeGiftRegex = new(@"(gratis|de\s*regalo|sin\s*cargo|obsequio|free\b|con\s*tu\s*compra\s*llevate)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public BenefitClassificationResult ClassifyBenefit(string text, int? normalizedPrice = null, int? originalPrice = null)
    {
        var input = text ?? string.Empty;

        // 1. Check for 2x1
        if (TwoForOneRegex.IsMatch(input))
        {
            return new BenefitClassificationResult(
                BenefitType: "TwoForOne",
                BenefitDescription: "2x1 (Segunda unidad gratis)",
                DiscountPercentage: 50
            );
        }

        // 2. Check for Percentage Discount
        var percentMatch = PercentageRegex.Match(input);
        if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out var percentVal) && percentVal > 0 && percentVal <= 100)
        {
            return new BenefitClassificationResult(
                BenefitType: "PercentageDiscount",
                BenefitDescription: $"{percentVal}% OFF",
                DiscountPercentage: percentVal
            );
        }

        // Check if original price and normalized price show a percentage discount
        if (originalPrice.HasValue && normalizedPrice.HasValue && originalPrice.Value > normalizedPrice.Value && originalPrice.Value > 0)
        {
            var calculatedPercent = (int)Math.Round((1.0 - ((double)normalizedPrice.Value / originalPrice.Value)) * 100);
            if (calculatedPercent > 0)
            {
                return new BenefitClassificationResult(
                    BenefitType: "PercentageDiscount",
                    BenefitDescription: $"{calculatedPercent}% OFF",
                    DiscountPercentage: calculatedPercent
                );
            }
        }

        // 3. Check for Free Gift
        if (FreeGiftRegex.IsMatch(input) && (input.Contains("papas", StringComparison.OrdinalIgnoreCase) ||
                                              input.Contains("bebida", StringComparison.OrdinalIgnoreCase) ||
                                              input.Contains("postre", StringComparison.OrdinalIgnoreCase) ||
                                              input.Contains("sundae", StringComparison.OrdinalIgnoreCase) ||
                                              input.Contains("cono", StringComparison.OrdinalIgnoreCase) ||
                                              input.Contains("regalo", StringComparison.OrdinalIgnoreCase)))
        {
            return new BenefitClassificationResult(
                BenefitType: "FreeGift",
                BenefitDescription: "Producto de regalo incluido",
                DiscountPercentage: null
            );
        }

        // 4. Fixed Amount / Special Price
        if (normalizedPrice.HasValue && normalizedPrice.Value > 0)
        {
            return new BenefitClassificationResult(
                BenefitType: "FixedAmount",
                BenefitDescription: $"Precio especial ${normalizedPrice.Value:N0}",
                DiscountPercentage: null
            );
        }

        // 5. Default: ComboSpecial
        return new BenefitClassificationResult(
            BenefitType: "ComboSpecial",
            BenefitDescription: "Combo promocional exclusivo",
            DiscountPercentage: null
        );
    }
}
