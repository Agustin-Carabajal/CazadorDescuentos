using CazadorDescuentos.Modules.IngestionAndAI.Services;
using Xunit;

namespace CazadorDescuentos.Tests;

public class BenefitClassificationServiceTests
{
    private readonly BenefitClassificationService _sut = new();

    [Fact]
    public void ClassifyBenefit_ShouldIdentifyTwoForOne()
    {
        var result1 = _sut.ClassifyBenefit("2x1 en hamburguesas doble carne");
        Assert.Equal("TwoForOne", result1.BenefitType);

        var result2 = _sut.ClassifyBenefit("Llevá dos por uno en Sundae");
        Assert.Equal("TwoForOne", result2.BenefitType);

        var result3 = _sut.ClassifyBenefit("Segunda unidad gratis en combo");
        Assert.Equal("TwoForOne", result3.BenefitType);
    }

    [Fact]
    public void ClassifyBenefit_ShouldIdentifyPercentageDiscount()
    {
        var result = _sut.ClassifyBenefit("30% OFF en Stacker Doble");
        Assert.Equal("PercentageDiscount", result.BenefitType);
        Assert.Equal(30, result.DiscountPercentage);

        var result2 = _sut.ClassifyBenefit("Descuento especial", normalizedPrice: 7000, originalPrice: 10000);
        Assert.Equal("PercentageDiscount", result2.BenefitType);
        Assert.Equal(30, result2.DiscountPercentage);
    }

    [Fact]
    public void ClassifyBenefit_ShouldIdentifyFreeGift()
    {
        var result = _sut.ClassifyBenefit("Papas medianas gratis con tu combo");
        Assert.Equal("FreeGift", result.BenefitType);
        Assert.Contains("regalo", result.BenefitDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClassifyBenefit_ShouldIdentifyFixedAmount()
    {
        var result = _sut.ClassifyBenefit("Combo del día a solo $ 9.500", normalizedPrice: 9500);
        Assert.Equal("FixedAmount", result.BenefitType);
    }
}
