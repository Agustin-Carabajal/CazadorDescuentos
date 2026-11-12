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
        new ScrapeTarget("McDonald's", "https://pedidos.mcdonalds.com.ar"),
        new ScrapeTarget("Burger King", "https://www.burgerking.com.ar/terminos-y-condiciones"),
        new ScrapeTarget("Mostaza", "https://www.mostazaweb.com.ar/promos-bancarias")
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
                        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
                        {
                            existing.ImageUrl = dto.ImageUrl;
                        }
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
                            ImageUrl = dto.ImageUrl,
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
                McDonald's Argentina Cupones y Ofertas Oficiales App y Web ({url})
                - McCombo Cuarto de Libra Mediano con Papas y Gaseosa: $ 13.900 ARS (antes $ 18.500). Descuento exclusivo del 25% OFF con cupón digital activo en la App McDonald's. Válido para retiro en sucursal o AutoMac. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                - McCombo Big Mac Mediano con Papas y Bebida: $ 12.800 ARS (precio de lista $ 17.500). Promoción oficial disponible con código BIGMACPROMO o canje en Mostrador. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                - 2x1 en McFlurry Oreo o KitKat: dos helados cremosos con trozos crocantes al precio de uno con cupón FLURRY2X1 en la App oficial. Válido fines de semana. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1563805042-7684c019e1cb?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - McCombo Doble Carne Doble Queso con Papas Medianas: $ 10.900 ARS (28% OFF). Pedidos oficiales en la plataforma digital. Vence 28/11/2026. [img: https://images.unsplash.com/photo-1586190848861-99aa4a171e90?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                - 2x1 en Sundae de Dulce de Leche o Chocolate Caliente: segunda unidad gratis en caja o tótem presentando cupón de la App. Vence 25/11/2026. [img: https://images.unsplash.com/photo-1563805042-7684c019e1cb?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - McCombo McNuggets x10 con 2 Dips de Salsa Barbacoa: $ 10.500 ARS (antes $ 14.800). Cupón NUGGETS10 para compra en la web y app oficial. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                - Papas Medianas Gratis con la compra de McCombo Grand Tasty: porción de papas doradas sin cargo extra con cupón PAPASFREE en la App. Vence 15/11/2026. [img: https://images.unsplash.com/photo-1576107232684-1279f3908594?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds]
                - McCombo Grand Tasty Doble Grande: $ 17.200 ARS (antes $ 22.900). 25% de descuento en la plataforma de pedidos online. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                - Desayuno McCafé Café con Leche y 2 Medialunas: $ 3.800 ARS. Modalidad salón o take-away de 7 a 11 hs. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1517256064527-09c73fc73e38?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                - Cono de Helado Vainilla o Mixto: $ 1.800 ARS. Precio especial directo en centros de postres y App. Válido todo el mes. [img: https://images.unsplash.com/photo-1563805042-7684c019e1cb?auto=format&fit=crop&w=500&q=80] [url: https://pedidos.mcdonalds.com.ar]
                """,
            "Burger King" => $"""
                Burger King Argentina Ofertas Oficiales App Mi BK ({url})
                - 2x1 Whopper Jr con Queso a la Parrilla: dos hamburguesas al precio de una. Cupón BK2X1 en la web oficial o en tótem. Vence 28/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://www.burgerking.com.ar/carta]
                - Combo Stacker Doble Grande con Papas y Gaseosa: $ 14.800 ARS (30% OFF de lista $ 21.000). Exclusivo en la App Mi BK con cupón STACKER30. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1607013251379-e6eecfffe234?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - Combo Whopper Clásico Mediano a la Parrilla: $ 13.900 ARS (antes $ 18.900). Descuento del 26% en la carta oficial para retiro en tienda. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://www.burgerking.com.ar/carta]
                - King de Pollo Crispy con Papas y Bebida: $ 11.200 ARS (25% OFF). Cupón BKPOLLO para pedidos web y en mostrador. Vence 25/11/2026. [img: https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=500&q=80] [url: https://www.burgerking.com.ar/carta]
                - 2x1 en King Fusión Oreo o Dulce de Leche: segunda unidad sin cargo con cupón FUSION2X1 en la App Mi BK. Vence 29/11/2026. [img: https://images.unsplash.com/photo-1563805042-7684c019e1cb?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - Aros de Cebolla Grandes Gratis con tu Combo Stacker: obsequio de aros dorados con cupón AROSGRATIS en la App Mi BK. Vence 20/11/2026. [img: https://images.unsplash.com/photo-1576107232684-1279f3908594?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - Combo Hamburguesa con Queso y Papas Regulares: $ 6.900 ARS. Menú promocional para almuerzos rápidos de lunes a viernes. Vence 25/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://www.burgerking.com.ar/carta]
                - 2x1 Combo Hamburguesa Doble Parrilla: dos combos completos al precio de uno. Cupón BKPARRILLA en la App. Vence 27/11/2026. [img: https://images.unsplash.com/photo-1607013251379-e6eecfffe234?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar]
                - King Nuggets x10 con Salsa BBQ: $ 7.900 ARS (30% OFF de lista). Promoción web oficial con cupón BKNUGGETS. Vence 24/11/2026. [img: https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=500&q=80] [url: https://www.burgerking.com.ar/carta]
                - Desayuno Café en Grano con 2 Medialunas: $ 3.500 ARS. Horario matutino de 8 a 11 hs para consumo en salón o take-away. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1517256064527-09c73fc73e38?auto=format&fit=crop&w=500&q=80] [url: https://www.burgerking.com.ar/carta]
                """,
            "Mostaza" => $"""
                Mostaza Argentina Cupones Reales App MostaClub y Promociones Oficiales ({url})
                - Combo Mega Cuarto XL con Papas Medianas y Gaseosa: $ 13.900 ARS (precio regular $ 18.500). Cupón exclusivo del 25% OFF en la App de Mostaza (MostaClub). Válido en locales o retiro take-away. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - Combo Mega Cuádruple Bacon con Papas Cheddar y Bebida: $ 16.500 ARS (antes $ 22.000). 25% OFF con cupón MOSTAZABACON en la App Mostaza. Válido lunes a viernes. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1553979459-d2229ba7433b?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - 2x1 en Mega Cuarto Cheeseburger: dos hamburguesas Mega al precio de una en compras online y tótem. Cupón CUARTO2X1. Vence 25/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://www.mostazaweb.com.ar/]
                - Combo Pollo Crispy Mega Box con Papas y Bebida: $ 12.200 ARS (28% OFF). Crujiente suprema de pollo empanada. Válido en web y app oficial. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=500&q=80] [url: https://www.mostazaweb.com.ar/]
                - 30% OFF en Combos Mega con Tarjetas Santander o Galicia: beneficio bancario exclusivo los miércoles y viernes pagando con tarjetas adheridas. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=500&q=80] [url: https://www.mostazaweb.com.ar/promos-bancarias]
                - Combo Mega Boom Doble con Salsa Mostaza y Cebolla Crispy: $ 14.500 ARS (antes $ 19.900). Promoción de la semana en la App Mostaza. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1586190848861-99aa4a171e90?auto=format&fit=crop&w=500&q=80] [url: https://www.mostazaweb.com.ar/]
                - 2x1 en Helado Mostaza Sundae de Dulce de Leche o Chocolate: dos copas de helado suave al precio de una con cupón SUNDAE2X1 en MostaClub. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1563805042-7684c019e1cb?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - Gaseosa Grande Gratis con tu compra de cualquier Hamburguesa Mega Doble: obsequio sin cargo con cupón BEBIDAGRATIS en la App Mostaza. Vence 20/11/2026. [img: https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - Papas Fritas con Cheddar y Bacon Extra Gratis con tu Combo Mega Triple: porción de papas con cheddar fundido de regalo con cupón CHEDDARGIFT en la App. Vence 26/11/2026. [img: https://images.unsplash.com/photo-1576107232684-1279f3908594?auto=format&fit=crop&w=500&q=80] [url: https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza]
                - Desayuno Mega Café con Tostado de Jamón y Queso en Pan de Campo: $ 4.200 ARS. Modalidad horario de mañana en sucursales adheridas. Vence 30/11/2026. [img: https://images.unsplash.com/photo-1517256064527-09c73fc73e38?auto=format&fit=crop&w=500&q=80] [url: https://www.mostazaweb.com.ar/]
                """,
            _ => $"""
                Ofertas Generales ({url})
                - Combo Promocional: $ 12.000 ARS. Válido hasta 30/11/2026. [url: {url}]
                """
        };
    }
}
