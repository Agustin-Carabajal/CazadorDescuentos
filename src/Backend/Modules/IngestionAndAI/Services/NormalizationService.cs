using System.Globalization;
using System.Text.RegularExpressions;

namespace CazadorDescuentos.Modules.IngestionAndAI.Services;

public class NormalizationService : INormalizationService
{
    private static readonly Regex PriceRegex = new(@"[\d]+(?:[.,]\d+)*", RegexOptions.Compiled);
    private static readonly Regex StrangeCharsRegex = new(@"[^\w\s\-\+\'\.\/]", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    public int NormalizePrice(string? rawPrice)
    {
        if (string.IsNullOrWhiteSpace(rawPrice))
        {
            return 0;
        }

        var trimmed = rawPrice.Trim().ToUpperInvariant();

        if (trimmed.Contains("GRATIS") || trimmed.Equals("0"))
        {
            return 0;
        }

        // Match numeric parts
        var match = PriceRegex.Match(trimmed);
        if (!match.Success)
        {
            return 0;
        }

        var numberStr = match.Value;

        // Argentine/Latin formatting: 15.000 or 15000 or 4.500,00 or 4500.50
        if (numberStr.Contains('.') && numberStr.Contains(','))
        {
            // e.g. 15.000,50 -> strip thousands dot, truncate decimal
            numberStr = numberStr.Replace(".", "");
            var commaIndex = numberStr.IndexOf(',');
            if (commaIndex >= 0)
            {
                numberStr = numberStr[..commaIndex];
            }
        }
        else if (numberStr.Contains('.'))
        {
            // Could be thousands separator (15.000) or decimal (15.50)
            var parts = numberStr.Split('.');
            if (parts.Length == 2 && parts[1].Length == 3)
            {
                // Thousands separator e.g. 15.000 -> 15000
                numberStr = parts[0] + parts[1];
            }
            else if (parts.Length > 2)
            {
                numberStr = string.Concat(parts);
            }
            else
            {
                // e.g. 15.50 -> 15
                numberStr = parts[0];
            }
        }
        else if (numberStr.Contains(','))
        {
            var parts = numberStr.Split(',');
            if (parts.Length == 2 && parts[1].Length == 3)
            {
                numberStr = parts[0] + parts[1];
            }
            else
            {
                numberStr = parts[0];
            }
        }

        if (int.TryParse(numberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
        {
            return parsedInt;
        }

        return 0;
    }

    public DateTime NormalizeDate(string? rawDate, DateTime? referenceDate = null)
    {
        var now = referenceDate ?? DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(rawDate))
        {
            // Default TTL for quick-service deals without explicit date: 7 days
            return now.AddDays(7).Date;
        }

        var cleaned = rawDate.Trim();

        // 1. Try ISO standard YYYY-MM-DD
        if (DateTime.TryParseExact(cleaned, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
        {
            return DateTime.SpecifyKind(isoDate, DateTimeKind.Utc);
        }

        // 2. Try common formats (dd/MM/yyyy, dd-MM-yyyy, dd/MM, etc.)
        var formats = new[]
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy/MM/dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"
        };

        if (DateTime.TryParseExact(cleaned, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFormatDate))
        {
            return DateTime.SpecifyKind(parsedFormatDate, DateTimeKind.Utc);
        }

        // 3. Match natural language patterns e.g. "20/10" or "hasta 20/10"
        var shortDateMatch = Regex.Match(cleaned, @"(\d{1,2})[\/\-](\d{1,2})(?:[\/\-](\d{2,4}))?");
        if (shortDateMatch.Success)
        {
            var day = int.Parse(shortDateMatch.Groups[1].Value);
            var month = int.Parse(shortDateMatch.Groups[2].Value);
            var year = shortDateMatch.Groups[3].Success ? int.Parse(shortDateMatch.Groups[3].Value) : now.Year;
            if (year < 100) year += 2000;

            try
            {
                return DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
            }
            catch
            {
                // Fall through if invalid day/month
            }
        }

        // 4. Handle relative days in Spanish ("hasta el domingo", "hasta mañana", "válido hoy")
        var lower = cleaned.ToLowerInvariant();
        if (lower.Contains("hoy"))
        {
            return now.Date;
        }
        if (lower.Contains("mañana"))
        {
            return now.AddDays(1).Date;
        }
        if (lower.Contains("domingo"))
        {
            return NextDayOfWeek(now, DayOfWeek.Sunday);
        }
        if (lower.Contains("sabado") || lower.Contains("sábado"))
        {
            return NextDayOfWeek(now, DayOfWeek.Saturday);
        }
        if (lower.Contains("viernes"))
        {
            return NextDayOfWeek(now, DayOfWeek.Friday);
        }
        if (lower.Contains("jueves"))
        {
            return NextDayOfWeek(now, DayOfWeek.Thursday);
        }
        if (lower.Contains("miercoles") || lower.Contains("miércoles"))
        {
            return NextDayOfWeek(now, DayOfWeek.Wednesday);
        }
        if (lower.Contains("martes"))
        {
            return NextDayOfWeek(now, DayOfWeek.Tuesday);
        }
        if (lower.Contains("lunes"))
        {
            return NextDayOfWeek(now, DayOfWeek.Monday);
        }
        if (lower.Contains("fin de mes"))
        {
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            return DateTime.SpecifyKind(new DateTime(now.Year, now.Month, daysInMonth), DateTimeKind.Utc);
        }

        // Fallback default
        return now.AddDays(7).Date;
    }

    public string NormalizeName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        // Clean strange non-standard punctuation / promotional noise
        var text = StrangeCharsRegex.Replace(rawName, " ");
        text = MultipleSpacesRegex.Replace(text, " ").Trim();

        // Title case words, preserving brand specifics like "Mc", "BK"
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (word.StartsWith("mc", StringComparison.OrdinalIgnoreCase) && word.Length > 2)
            {
                words[i] = "Mc" + char.ToUpperInvariant(word[2]) + (word.Length > 3 ? word[3..].ToLowerInvariant() : "");
            }
            else if (word.Equals("bk", StringComparison.OrdinalIgnoreCase))
            {
                words[i] = "BK";
            }
            else if (word.Equals("2x1", StringComparison.OrdinalIgnoreCase))
            {
                words[i] = "2x1";
            }
            else if (word.Length > 1)
            {
                words[i] = char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
            }
            else
            {
                words[i] = word.ToUpperInvariant();
            }
        }

        return string.Join(" ", words);
    }

    private static DateTime NextDayOfWeek(DateTime from, DayOfWeek targetDay)
    {
        var start = from.Date;
        int daysToAdd = ((int)targetDay - (int)start.DayOfWeek + 7) % 7;
        if (daysToAdd == 0) daysToAdd = 7;
        return DateTime.SpecifyKind(start.AddDays(daysToAdd), DateTimeKind.Utc);
    }
}
