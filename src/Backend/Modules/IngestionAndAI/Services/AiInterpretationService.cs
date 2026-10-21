using System.Text.Json;
using System.Text.RegularExpressions;
using CazadorDescuentos.Modules.IngestionAndAI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class AiInterpretationService : IAiInterpretationService
{
    private readonly HttpClient _httpClient;
    private readonly INormalizationService _normalizationService;
    private readonly IBenefitClassificationService _benefitClassificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiInterpretationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AiInterpretationService(
        HttpClient httpClient,
        INormalizationService normalizationService,
        IBenefitClassificationService benefitClassificationService,
        IConfiguration configuration,
        ILogger<AiInterpretationService> logger)
    {
        _httpClient = httpClient;
        _normalizationService = normalizationService;
        _benefitClassificationService = benefitClassificationService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiScrapeInterpretationResponse> InterpretRawContentAsync(
        string brand,
        string sourceUrl,
        string rawContent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing raw scraped text for brand {Brand} from {SourceUrl}", brand, sourceUrl);

        var apiKey = _configuration["AI:ApiKey"] ?? _configuration["Gemini:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var apiResult = await CallExternalLlmAsync(apiKey, brand, sourceUrl, rawContent, cancellationToken);
                if (apiResult != null && apiResult.Offers.Count > 0)
                {
                    return apiResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External LLM call failed. Falling back to built-in intelligent extraction engine.");
            }
        }

        // Built-in intelligent rule & NLP extraction engine
        return ExtractWithBuiltInEngine(brand, sourceUrl, rawContent);
    }

    private async Task<AiScrapeInterpretationResponse?> CallExternalLlmAsync(
        string apiKey,
        string brand,
        string sourceUrl,
        string rawContent,
        CancellationToken cancellationToken)
    {
        var prompt = $$"""
        Sos un asistente de IA especializado en analizar y extraer ofertas de cadenas de comida rápida en Argentina.
        Marca: {{brand}}
        URL de origen: {{sourceUrl}}

        Analiza el siguiente texto extraído de la web y genera un JSON estrictamente estructurado según el siguiente formato:
        {
          "brand": "{{brand}}",
          "offers": [
            {
              "title": "Nombre limpio del producto",
              "description": "Breve descripción",
              "category": "Combo | Hamburguesa | Papas | Postre | Pollo | Bebida",
              "rawPrice": "$15.000",
              "normalizedPrice": 15000,
              "originalPrice": 20000,
              "benefitType": "TwoForOne | PercentageDiscount | FixedAmount | FreeGift | ComboSpecial",
              "benefitDescription": "ej. 2x1 o 30% OFF",
              "promoCode": "CUPONX",
              "channel": "App | Web | App / Web",
              "destinationUrl": "{{sourceUrl}}",
              "termsAndConditions": "Resumen claro de letra chica (días, medios de pago, etc.)",
              "rawValidUntil": "hasta 31/10",
              "validUntil": "YYYY-MM-DD"
            }
          ]
        }

        Texto a analizar:
        {{rawContent}}
        """;

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var text = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                var structured = JsonSerializer.Deserialize<AiScrapeInterpretationResponse>(text, JsonOptions);
                if (structured != null)
                {
                    // Post-process to ensure normalized values
                    foreach (var offer in structured.Offers)
                    {
                        offer.Title = _normalizationService.NormalizeName(offer.Title);
                        if (offer.NormalizedPrice == 0 && !string.IsNullOrWhiteSpace(offer.RawPrice))
                        {
                            offer.NormalizedPrice = _normalizationService.NormalizePrice(offer.RawPrice);
                        }
                        if (string.IsNullOrWhiteSpace(offer.DestinationUrl))
                        {
                            offer.DestinationUrl = sourceUrl;
                        }
                    }
                    return structured;
                }
            }
        }

        return null;
    }

    private AiScrapeInterpretationResponse ExtractWithBuiltInEngine(string brand, string sourceUrl, string rawContent)
    {
        var response = new AiScrapeInterpretationResponse
        {
            Brand = brand
        };

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return response;
        }

        // Split text by lines and paragraph blocks
        var lines = rawContent
            .Split(new[] { "\r\n", "\r", "\n", "---", "###" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 2)
            .ToList();

        // Scan blocks for distinct promotional offers
        var offerBlocks = new List<List<string>>();
        var currentBlock = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Ignore pure site headers like "McDonald's Promociones Web"
            bool isSiteHeader = line.StartsWith("#") || 
                               (line.Contains("promociones", StringComparison.OrdinalIgnoreCase) && !line.Contains('$') && !line.Contains("2x1"));

            if (isSiteHeader && currentBlock.Count == 0)
            {
                continue;
            }

            bool isBulletOrNewOffer = line.StartsWith("-") || line.StartsWith("*") ||
                                      Regex.IsMatch(line, @"^(\bcombo\b|\b2x1\b|\boferta\b|\bdescuento\b|\bcupón\b|\bcupon\b|\bmccombo\b|\bwhopper\b|\bmega\b)", RegexOptions.IgnoreCase);

            if (isBulletOrNewOffer && currentBlock.Count > 0)
            {
                offerBlocks.Add(currentBlock);
                currentBlock = new List<string> { line };
            }
            else
            {
                currentBlock.Add(line);
            }
        }

        if (currentBlock.Count > 0)
        {
            offerBlocks.Add(currentBlock);
        }

        foreach (var block in offerBlocks)
        {
            var combinedText = string.Join(" ", block);
            var titleLine = block.FirstOrDefault(l => l.StartsWith("-") || l.StartsWith("*") || l.Contains(':')) 
                            ?? block.FirstOrDefault() 
                            ?? brand + " Oferta";

            // Extract price
            var rawPrice = ExtractPriceString(combinedText);
            var normalizedPrice = _normalizationService.NormalizePrice(rawPrice);

            // Extract promo code
            var promoCode = ExtractPromoCode(combinedText);

            // Channel detection
            var channel = DetectChannel(combinedText);

            // Category classification
            var category = ClassifyCategory(combinedText);

            // Benefit classification
            var benefit = _benefitClassificationService.ClassifyBenefit(combinedText, normalizedPrice);

            // Terms and conditions summarization
            var terms = SummarizeTerms(combinedText);

            // Expiration date
            var rawDate = ExtractDateString(combinedText);
            var validUntilDate = _normalizationService.NormalizeDate(rawDate);
            var validUntilIso = validUntilDate.ToString("yyyy-MM-dd");

            var offer = new ExtractedOfferDto
            {
                Title = _normalizationService.NormalizeName(CleanTitle(titleLine)),
                Description = combinedText.Length > 250 ? combinedText[..247] + "..." : combinedText,
                Category = category,
                RawPrice = rawPrice,
                NormalizedPrice = normalizedPrice,
                OriginalPrice = normalizedPrice > 0 && benefit.DiscountPercentage.HasValue
                    ? (int)Math.Round(normalizedPrice / (1.0 - (benefit.DiscountPercentage.Value / 100.0)))
                    : null,
                BenefitType = benefit.BenefitType,
                BenefitDescription = benefit.BenefitDescription,
                PromoCode = promoCode,
                Channel = channel,
                DestinationUrl = sourceUrl,
                TermsAndConditions = terms,
                RawValidUntil = rawDate,
                ValidUntil = validUntilIso
            };

            // Avoid adding duplicates or completely empty offers
            if (!string.IsNullOrWhiteSpace(offer.Title) && offer.Title.Length >= 3)
            {
                response.Offers.Add(offer);
            }
        }

        return response;
    }

    private static string ExtractPriceString(string text)
    {
        var match = Regex.Match(text, @"(?:\$|ARS\s*|precio\s*:?\s*\$?)(\s*[\d\.\,]+(?:\s*ARS)?)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Value;
        }

        var bareNumberMatch = Regex.Match(text, @"\b\d{1,3}(?:\.\d{3})+\b");
        if (bareNumberMatch.Success)
        {
            return bareNumberMatch.Value;
        }

        return string.Empty;
    }

    private static string? ExtractPromoCode(string text)
    {
        var match = Regex.Match(text, @"(?:c[oó]digo|cup[oó]n|cup[oó]n\s*c[oó]digo)\s*[:\-]?\s*([A-Z0-9\-_]{4,15})", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.ToUpperInvariant();
        }

        var tagMatch = Regex.Match(text, @"\[([A-Z0-9\-_]{4,15})\]");
        if (tagMatch.Success)
        {
            return tagMatch.Groups[1].Value.ToUpperInvariant();
        }

        return null;
    }

    private static string DetectChannel(string text)
    {
        var lower = text.ToLowerInvariant();
        bool hasApp = lower.Contains("app") || lower.Contains("aplicación") || lower.Contains("aplicacion") || lower.Contains("móvil") || lower.Contains("movil");
        bool hasWeb = lower.Contains("web") || lower.Contains("sitio") || lower.Contains("online") || lower.Contains("página") || lower.Contains("pagina");

        if (hasApp && hasWeb) return "App / Web";
        if (hasApp) return "App";
        if (hasWeb) return "Web";
        return "App / Web";
    }

    private static string ClassifyCategory(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("papas") || lower.Contains("fries") || lower.Contains("aros de cebolla") || lower.Contains("nuggets"))
            return "Papas y Snacks";
        if (lower.Contains("postre") || lower.Contains("helado") || lower.Contains("sundae") || lower.Contains("mcflurry") || lower.Contains("cono") || lower.Contains("shake"))
            return "Postre";
        if (lower.Contains("desayuno") || lower.Contains("café") || lower.Contains("cafe") || lower.Contains("medialuna") || lower.Contains("tostado"))
            return "Desayuno y Merienda";
        if (lower.Contains("pollo") || lower.Contains("chicken") || lower.Contains("crispy"))
            return "Pollo";
        if (lower.Contains("combo") || lower.Contains("menu") || lower.Contains("menú"))
            return "Combo";
        if (lower.Contains("hamburguesa") || lower.Contains("burger") || lower.Contains("cheeseburger") || lower.Contains("whopper") || lower.Contains("mega"))
            return "Hamburguesa";
        if (lower.Contains("bebida") || lower.Contains("gaseosa") || lower.Contains("coca"))
            return "Bebida";

        return "Combo";
    }

    private static string SummarizeTerms(string text)
    {
        var termsList = new List<string>();

        // Days
        var dayMatch = Regex.Match(text, @"(lunes\s*a\s*viernes|s[aá]bados\s*y\s*domingos|fines\s*de\s*semana|todos\s*los\s*d[ií]as)", RegexOptions.IgnoreCase);
        if (dayMatch.Success)
        {
            termsList.Add($"Válido {dayMatch.Value.ToLowerInvariant()}");
        }

        // Payment method
        var paymentMatch = Regex.Match(text, @"(tarjeta\s+\w+|mercado\s*pago|modo|visa|mastercard|efectivo|debito|d[eé]bito|cr[eé]dito)", RegexOptions.IgnoreCase);
        if (paymentMatch.Success)
        {
            termsList.Add($"Pago con {paymentMatch.Value}");
        }

        // Consumption mode
        var modeMatch = Regex.Match(text, @"(take[\s\-]away|retiro\s*en\s*tienda|consumo\s*en\s*local|auto[\s\-]mac|automac|drive[\s\-]thru|delivery)", RegexOptions.IgnoreCase);
        if (modeMatch.Success)
        {
            termsList.Add($"Modalidad: {modeMatch.Value}");
        }

        if (termsList.Count == 0)
        {
            return "Válido en sucursales adheridas para consumo en local o take-away hasta agotar stock.";
        }

        return string.Join(". ", termsList) + ".";
    }

    private static string? ExtractDateString(string text)
    {
        var match = Regex.Match(text, @"(?:hasta|v[aá]lido\s*(?:hasta)?|vence|vigencia)\s*[:\-]?\s*(\d{1,2}[\/\-]\d{1,2}(?:[\/\-]\d{2,4})?|\d{4}-\d{2}-\d{2}|[a-zA-Záéíóú]+(?:\s+el\s+[a-zA-Záéíóú]+)?)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private static string CleanTitle(string text)
    {
        var part = text;
        var colonIdx = text.IndexOf(':');
        if (colonIdx > 2)
        {
            part = text[..colonIdx];
        }

        // Remove markdown bullets or leading punctuation
        var cleaned = Regex.Replace(part, @"^[\-\*\#\s\d\.\:]+", "").Trim();
        // Remove trailing price mentions in title
        cleaned = Regex.Replace(cleaned, @"\s*[\$\-].*$", "").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? text : cleaned;
    }
}
