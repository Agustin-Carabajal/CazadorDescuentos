using CazadorDescuentos.Modules.IngestionAndAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CazadorDescuentos.Tests;

public class AiInterpretationServiceTests
{
    private readonly AiInterpretationService _sut;

    public AiInterpretationServiceTests()
    {
        var config = new ConfigurationBuilder().Build();
        var norm = new NormalizationService();
        var benefit = new BenefitClassificationService();
        var httpClient = new HttpClient();
        var logger = NullLogger<AiInterpretationService>.Instance;

        _sut = new AiInterpretationService(httpClient, norm, benefit, config, logger);
    }

    [Fact]
    public async Task InterpretRawContentAsync_ShouldExtractStructuredJsonOffers()
    {
        var rawText = """
        McDonald's Promociones Web
        - McCombo Cuarto de Libra: $ 11.500 ARS. Exclusivo cupón MCDOBLE en la App. Válido lunes a viernes para take-away. Vence 30/11/2026.
        - 2x1 en Sundae de Dulce de Leche: segunda unidad gratis. Exclusivo Web. Válido fines de semana. Vence 25/11/2026.
        """;

        var result = await _sut.InterpretRawContentAsync("McDonald's", "https://mcdonalds.com.ar", rawText);

        Assert.NotNull(result);
        Assert.Equal("McDonald's", result.Brand);
        Assert.True(result.Offers.Count >= 2);

        var firstOffer = result.Offers[0];
        Assert.Contains("Cuarto De Libra", firstOffer.Title);
        Assert.Equal(11500, firstOffer.NormalizedPrice);
        Assert.Equal("App", firstOffer.Channel);
        Assert.Equal("MCDOBLE", firstOffer.PromoCode);
        Assert.Contains("take-away", firstOffer.TermsAndConditions?.ToLowerInvariant() ?? "");

        var secondOffer = result.Offers[1];
        Assert.Equal("TwoForOne", secondOffer.BenefitType);
        Assert.Equal("Web", secondOffer.Channel);
    }
}
