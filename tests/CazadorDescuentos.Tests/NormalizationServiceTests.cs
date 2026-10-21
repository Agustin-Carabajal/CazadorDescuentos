using CazadorDescuentos.Modules.IngestionAndAI.Services;
using Xunit;

namespace CazadorDescuentos.Tests;

public class NormalizationServiceTests
{
    private readonly NormalizationService _sut = new();

    [Theory]
    [InlineData("$15.000", 15000)]
    [InlineData("15000ARS", 15000)]
    [InlineData("$ 4.500,00", 4500)]
    [InlineData("19.999", 19999)]
    [InlineData("Gratis", 0)]
    [InlineData("$0", 0)]
    [InlineData("Sin costo", 0)]
    [InlineData("8900", 8900)]
    public void NormalizePrice_ShouldReturnCleanInteger(string rawPrice, int expected)
    {
        var result = _sut.NormalizePrice(rawPrice);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeDate_ShouldFormatIsoDateProperly()
    {
        var fixedNow = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        // ISO format
        var isoResult = _sut.NormalizeDate("2026-11-15", fixedNow);
        Assert.Equal("2026-11-15", isoResult.ToString("yyyy-MM-dd"));

        // dd/MM/yyyy
        var ddMmResult = _sut.NormalizeDate("30/11/2026", fixedNow);
        Assert.Equal("2026-11-30", ddMmResult.ToString("yyyy-MM-dd"));

        // Short date "25/10"
        var shortDateResult = _sut.NormalizeDate("Válido hasta 25/10", fixedNow);
        Assert.Equal("2026-10-25", shortDateResult.ToString("yyyy-MM-dd"));

        // Natural language "hasta mañana"
        var tomorrowResult = _sut.NormalizeDate("hasta mañana", fixedNow);
        Assert.Equal("2026-10-22", tomorrowResult.ToString("yyyy-MM-dd"));
    }

    [Theory]
    [InlineData("  hamburguesa   doble  con  queso  ", "Hamburguesa Doble Con Queso")]
    [InlineData("combo mcflurry oreo!!", "Combo McFlurry Oreo")]
    [InlineData("bk stacker cuadruple", "BK Stacker Cuadruple")]
    [InlineData("2x1 en hamburguesas", "2x1 En Hamburguesas")]
    public void NormalizeName_ShouldCleanSpacesAndStrangeChars(string rawName, string expected)
    {
        var result = _sut.NormalizeName(rawName);
        Assert.Equal(expected, result);
    }
}
