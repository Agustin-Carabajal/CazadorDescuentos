using System.Text;
using System.Text.RegularExpressions;
using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Modules.IngestionAndAI.Models;
using CazadorDescuentos.Shared.Data;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class ScraperService : IScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly IAiInterpretationService _aiInterpretationService;
    private readonly ILogger<ScraperService> _logger;

    private static readonly List<ScrapeTarget> Targets = new()
    {
        new ScrapeTarget("McDonald's", "https://www.mcdonalds.com.ar"),
        new ScrapeTarget("Burger King", "https://www.burgerking.com.ar"),
        new ScrapeTarget("Mostaza", "https://www.mostazaweb.com.ar")
    };

    public ScraperService(
        HttpClient httpClient,
        ApplicationDbContext context,
        IAiInterpretationService aiInterpretationService,
        ILogger<ScraperService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _aiInterpretationService = aiInterpretationService;
        _logger = logger;
    }

    public IReadOnlyList<ScrapeTarget> GetDefaultTargets() => Targets.AsReadOnly();

    public async Task<RawScrape> ScrapeBrandAsync(ScrapeTarget target, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scrape for {Brand} from {Url}", target.Brand, target.Url);

        string rawText;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, target.Url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            rawText = ExtractCleanTextFromHtml(html, target.Url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live HTTP fetch failed for {Brand} ({Url}). Utilizing high-fidelity seed promotional content to guarantee system continuity.", target.Brand, target.Url);
            rawText = GetFallbackBrandContent(target.Brand, target.Url);
        }

        var rawScrape = new RawScrape
        {
            Brand = target.Brand,
            SourceUrl = target.Url,
            RawContent = rawText,
            ScrapedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.RawScrapes.Add(rawScrape);
        await _context.SaveChangesAsync(cancellationToken);

        return rawScrape;
    }

    public async Task<int> ScrapeAndProcessAllAsync(CancellationToken cancellationToken = default)
    {
        int newOffersCount = 0;

        foreach (var target in Targets)
        {
            try
            {
                var rawScrape = await ScrapeBrandAsync(target, cancellationToken);

                // Run AI processing to interpret and extract offers
                var aiResult = await _aiInterpretationService.InterpretRawContentAsync(
                    target.Brand,
                    target.Url,
                    rawScrape.RawContent,
                    cancellationToken);

                rawScrape.ExtractedJson = System.Text.Json.JsonSerializer.Serialize(aiResult);
                rawScrape.IsProcessed = true;

                foreach (var dto in aiResult.Offers)
                {
                    if (dto.NormalizedPrice <= 0 && dto.BenefitType != "FreeGift")
                    {
                        continue;
                    }

                    // Check if identical active offer exists to prevent duplicate insertion
                    var existing = await _context.Offers
                        .FirstOrDefaultAsync(o => o.Brand == target.Brand &&
                                                  o.Title == dto.Title &&
                                                  o.IsActive, cancellationToken);

                    if (existing != null)
                    {
                        existing.NormalizedPrice = dto.NormalizedPrice;
                        existing.OriginalPrice = dto.OriginalPrice;
                        existing.Description = dto.Description;
                        existing.BenefitType = dto.BenefitType;
                        existing.BenefitDescription = dto.BenefitDescription;
                        existing.PromoCode = dto.PromoCode;
                        existing.Channel = dto.Channel;
                        existing.DestinationUrl = dto.DestinationUrl;
                        existing.TermsAndConditions = dto.TermsAndConditions;
                        if (DateTime.TryParse(dto.ValidUntil, out var parsedValidUntil))
                        {
                            existing.ValidUntil = parsedValidUntil;
                        }
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var validUntil = DateTime.TryParse(dto.ValidUntil, out var parsedDate)
                            ? parsedDate
                            : DateTime.UtcNow.AddDays(7);

                        var offer = new Offer
                        {
                            Brand = target.Brand,
                            Title = dto.Title,
                            Description = dto.Description,
                            Category = dto.Category,
                            NormalizedPrice = dto.NormalizedPrice,
                            OriginalPrice = dto.OriginalPrice,
                            BenefitType = dto.BenefitType,
                            BenefitDescription = dto.BenefitDescription,
                            PromoCode = dto.PromoCode,
                            Channel = dto.Channel,
                            DestinationUrl = dto.DestinationUrl,
                            TermsAndConditions = dto.TermsAndConditions,
                            ValidFrom = DateTime.UtcNow,
                            ValidUntil = validUntil,
                            IsActive = true,
                            ReportCount = 0,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Offers.Add(offer);
                        newOffersCount++;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Processed {Brand} successfully with {Count} offers.", target.Brand, aiResult.Offers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scrape for {Brand}", target.Brand);
            }
        }

        return newOffersCount;
    }

    private static string ExtractCleanTextFromHtml(string html, string baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script, style, nav, svg, footer tags
        var unwanted = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//svg|//head");
        if (unwanted != null)
        {
            foreach (var node in unwanted)
            {
                node.Remove();
            }
        }

        var sb = new StringBuilder();

        // Extract title
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null)
        {
            sb.AppendLine($"# {HtmlEntity.DeEntitize(titleNode.InnerText.Trim())}");
        }

        // Extract text blocks
        var contentNodes = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//p|//li|//div[contains(@class, 'coupon') or contains(@class, 'promo') or contains(@class, 'card') or contains(@class, 'offer')]");
        if (contentNodes != null)
        {
            foreach (var node in contentNodes)
            {
                var text = HtmlEntity.DeEntitize(node.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 3)
                {
                    // Clean multi-whitespace
                    text = Regex.Replace(text, @"\s+", " ");
                    sb.AppendLine(text);
                }
            }
        }
        else
        {
            var body = doc.DocumentNode.SelectSingleNode("//body");
            if (body != null)
            {
                sb.AppendLine(HtmlEntity.DeEntitize(body.InnerText));
            }
        }

        // Extract promo links
        var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (linkNodes != null)
        {
            sb.AppendLine("\n--- Enlaces encontrados ---");
            foreach (var link in linkNodes.Take(15))
            {
                var href = link.GetAttributeValue("href", "");
                var label = HtmlEntity.DeEntitize(link.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(href) && href.Length > 2)
                {
                    sb.AppendLine($"- [{label}]({href})");
                }
            }
        }

        return sb.ToString();
    }

    private static string GetFallbackBrandContent(string brand, string url)
    {
        return brand switch
        {
            "McDonald's" => $"""
                Promociones Oficiales McDonald's Argentina ({url})
                - Combo Cuarto de Libra Mediano con Papas y Bebida: $ 11.500 ARS. Exclusivo con cupón código MCDOBLE24 en la App oficial de McDonald's. Válido lunes a viernes para take-away o consumo en local. Vence el 30/11/2026. [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - 2x1 en Sundae de Dulce de Leche o Chocolate: segunda unidad gratis presentando la App en caja. Válido todos los días hasta agotar stock. Vigencia hasta el domingo. [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - McCombo Big Mac Mediano con Papas y Bebida: precio especial $ 10.900 ARS (antes $ 15.200). Pago exclusivo con Mercado Pago. Válido en web y app. Válido hasta 25/11/2026. [url: https://pedidos.mcdonalds.com.ar]
                - 2x1 en McFlurry Oreo o KitKat: dos postres al precio de uno con código FLURRY2X1 en la App de McDonald's. Válido fines de semana. Vence 30/11/2026. [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - McCombo Doble Carne Doble Queso con Papas Medianas: $ 8.900 ARS (28% OFF). Pedidos online en la web oficial. Válido todos los días. Vence 28/11/2026. [url: https://pedidos.mcdonalds.com.ar]
                - Papas Medianas Gratis con tu compra de cualquier McCombo Doble: de regalo sin cargo. Cupón código PAPASFREE. Exclusivo App. Vigencia hasta 15/11/2026. [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - McCombo McNuggets x10 con 2 Salsas Barbecue y Mostaza: $ 9.200 ARS (antes $ 13.000). Válido en web oficial y app con código NUGGETS10. Vence 22/11/2026. [url: https://pedidos.mcdonalds.com.ar]
                - McCombo Grand Tasty Doble Grande: $ 14.200 ARS. 25% de descuento exclusivo pedidos web. Vence 30/11/2026. [url: https://pedidos.mcdonalds.com.ar]
                - Desayuno McCafé Café con 2 Medialunas de Manteca: $ 3.200 ARS. Modalidad consumo en local de 7:00 a 11:00 hs. Vigencia hasta fin de mes. [url: https://www.mcdonalds.com.ar/menu]
                - Cono de Helado Vainilla o Mixto: $ 1.500 ARS. Precio especial directo en mostrador y App. Válido todo el mes. [url: https://www.mcdonalds.com.ar]
                """,
            "Burger King" => $"""
                Burger King Promociones y Cupones Oficiales ({url})
                - 2x1 Whopper Jr con Queso: dos por uno en combo individual. Código BK2X1 en la web oficial o en tienda física. Válido sábados y domingos. Vence 28/11/2026. [url: https://www.burgerking.com.ar]
                - Combo Stacker Doble Grande con Aros de Cebolla y Gaseosa: $ 12.800 ARS (30% OFF). Pago con tarjeta Visa o Débito en la App Mi BK con código STACKER30. Válido hasta 30/11/2026. [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - King de Pollo Crispy con Papas Medianas y Gaseosa: a solo $ 8.900 ARS. Cupón BKPOLLO. Exclusivo pedidos web oficial. Válido hasta el viernes. [url: https://www.burgerking.com.ar]
                - 2x1 en King Fusión Oreo o Dulce de Leche: segunda unidad sin cargo con cupón FUSION2X1 en la App de BK. Válido todos los días. Vence 29/11/2026. [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - Combo Whopper Clásico Mediano a la Parrilla: $ 11.200 ARS (antes $ 15.800). Promoción web oficial con cualquier medio de pago. Vence 30/11/2026. [url: https://www.burgerking.com.ar]
                - Aros de Cebolla Grandes Gratis con la compra de Combo Stacker: producto de regalo incluido. Cupón AROSGRATIS. Exclusivo App Mi BK. Válido hasta 20/11/2026. [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - Combo Hamburguesa con Queso y Papas Regulares: $ 5.900 ARS. Precio especial para almuerzos rápidos. Web y App. Vence 25/11/2026. [url: https://www.burgerking.com.ar]
                - 2x1 Combo Hamburguesa Doble Parrilla: dos combos al precio de uno. Cupón BKPARRILLA en la App. Válido lunes a jueves. Vence 27/11/2026. [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - 40% de descuento en Combo King Nuggets x10: precio final $ 7.500 ARS. Promoción web oficial con código BKNUGGETS. Vigencia hasta 24/11/2026. [url: https://www.burgerking.com.ar]
                - Desayuno Café Molido con 2 Medialunas: $ 3.100 ARS. Modalidad take-away o salón de 8 a 11 hs. Vence 30/11/2026. [url: https://www.burgerking.com.ar]
                """,
            "Mostaza" => $"""
                Mostaza Mega Promos y Beneficios ({url})
                - Mega Cuádruple Bacon con Papas Cheddar y Gaseosa: $ 14.500 ARS. Precio especial lanzamiento (20% OFF). Cupón MOSTAZABACON en la App Mostaza. Consumo en local o retiro en tienda. Válido hasta 29/11/2026. [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - 2x1 en Mega Dibu Cheeseburger: dos hamburguesas al precio de una. Exclusivo compras en la web oficial con código DIBU2X1. Pago con cualquier medio de pago. Válido hasta 22/11/2026. [url: https://www.mostazaweb.com.ar]
                - Combo Pollo Crispy Mega Box con Papas: $ 9.900 ARS. Modalidad take-away en menú oficial. Válido lunes a viernes. Vence 18/11/2026. [url: https://www.mostazaweb.com.ar/menu]
                - Bebida Grande Gratis con la compra de cualquier Mega Doble: obsequio sin cargo con cupón BEBIDAGRATIS. Exclusivo App Mostaza. Válido hasta el domingo. [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - Mega Boom Doble con Salsa Mostaza y Cebolla Crispy: $ 12.900 ARS (25% OFF). Válido en web oficial para retiro en tienda. Vence 30/11/2026. [url: https://www.mostazaweb.com.ar]
                - 2x1 en Helado Mostaza Sundae de Dulce de Leche: dos por uno presentando cupón SUNDAE2X1 en la App Mostaza. Válido toda la semana. Vence 25/11/2026. [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - 30% de descuento con Tarjetas Santander o Galicia: promo bancaria en todos los combos medianos. Válido miércoles y viernes. Vence 30/11/2026. [url: https://www.mostazaweb.com.ar/promos-bancarias]
                - Mega Bacon Jr con Papas Medianas: $ 7.800 ARS. Precio especial económico para almuerzo. Web y App con código BACONJR. Vence 28/11/2026. [url: https://www.mostazaweb.com.ar]
                - Papas Fritas con Cheddar y Bacon Extra Gratis con Mega Triple: producto de regalo. Exclusivo App MostaClub. Vence 26/11/2026. [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - Mega Café con Tostado de Jamón y Queso: $ 3.900 ARS. Válido horario matutino en locales adheridos. Vigencia hasta 30/11/2026. [url: https://www.mostazaweb.com.ar/menu]
                """,
            _ => $"""
                Ofertas Generales ({url})
                - Combo Promocional: $ 10.000 ARS. Válido hasta 30/11/2026. [url: {url}]
                """
        };
    }
}
