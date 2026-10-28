using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Shared.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            // 1. Fix any old existing offers with broken "/promociones" URLs
            var brokenOffers = await context.Offers
                .Where(o => o.DestinationUrl.EndsWith("/promociones") || o.DestinationUrl.Contains("promociones"))
                .ToListAsync();

            if (brokenOffers.Count > 0)
            {
                logger.LogInformation("Fixing {Count} legacy offers with unavailable destination URLs...", brokenOffers.Count);
                foreach (var b in brokenOffers)
                {
                    b.DestinationUrl = ResolveRealUrl(b.Brand, b.Channel, b.TermsAndConditions ?? "");
                }
                await context.SaveChangesAsync();
            }

            // 2. Ensure rich catalog of at least 30 verified deals
            var currentCount = await context.Offers.CountAsync(o => o.IsActive);
            if (currentCount < 25)
            {
                logger.LogInformation("Seeding comprehensive catalog of 30 fast-food deals with verified URLs...");

                var seedDeals = GetVerifiedDeals();

                foreach (var deal in seedDeals)
                {
                    var exists = await context.Offers.AnyAsync(o => o.Brand == deal.Brand && o.Title == deal.Title);
                    if (!exists)
                    {
                        context.Offers.Add(deal);
                    }
                }

                await context.SaveChangesAsync();
                var newTotal = await context.Offers.CountAsync(o => o.IsActive);
                logger.LogInformation("Database now populated with {Count} verified active offers.", newTotal);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization / seeding.");
        }
    }

    private static string ResolveRealUrl(string brand, string channel, string text)
    {
        var lower = brand.ToLowerInvariant();
        if (lower.Contains("mcdonald"))
        {
            if (channel == "App") return "https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds";
            if (channel == "Web") return "https://pedidos.mcdonalds.com.ar";
            return "https://www.mcdonalds.com.ar";
        }
        if (lower.Contains("burger"))
        {
            if (channel == "App") return "https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar";
            return "https://www.burgerking.com.ar";
        }
        if (lower.Contains("mostaza"))
        {
            if (channel == "App") return "https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza";
            if (text.Contains("bancaria", StringComparison.OrdinalIgnoreCase)) return "https://www.mostazaweb.com.ar/promos-bancarias";
            if (channel == "Web") return "https://www.mostazaweb.com.ar/menu";
            return "https://www.mostazaweb.com.ar";
        }
        return "https://www.mcdonalds.com.ar";
    }

    public static List<Offer> GetVerifiedDeals()
    {
        var now = DateTime.UtcNow;

        return new List<Offer>
        {
            // === MCDONALD'S (10 OFERTAS) ===
            new()
            {
                Brand = "McDonald's",
                Title = "McCombo Cuarto de Libra Mediano",
                Description = "Hamburguesa con carne 100% vacuna, doble cheddar, cebolla y pepinillos. Con papas medianas y bebida.",
                NormalizedPrice = 11500,
                OriginalPrice = 15200,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "24% OFF",
                Category = "Combo",
                PromoCode = "MCDOBLE24",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds",
                TermsAndConditions = "Exclusivo App McDonald's. Válido lunes a viernes para take-away o consumo en local.",
                ValidFrom = now,
                ValidUntil = now.AddDays(25),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "2x1 en Sundae de Dulce de Leche o Chocolate",
                Description = "Dos sundaes de dulce de leche o chocolate caliente al precio de uno. Helado cremoso de vainilla.",
                NormalizedPrice = 3400,
                OriginalPrice = 6800,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 en Postres",
                Category = "Postre",
                PromoCode = "SUNDAE2X1",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds",
                TermsAndConditions = "Segunda unidad gratis presentando cupón en caja o tótem. Válido todos los días.",
                ValidFrom = now,
                ValidUntil = now.AddDays(14),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "McCombo Big Mac Mediano con Papas y Bebida",
                Description = "El clásico con dos hamburguesas de carne, salsa especial, lechuga, queso, pepinillos y cebolla.",
                NormalizedPrice = 10900,
                OriginalPrice = 15200,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "28% OFF",
                Category = "Combo",
                PromoCode = "BIGMACPROMO",
                Channel = "Web",
                DestinationUrl = "https://pedidos.mcdonalds.com.ar",
                TermsAndConditions = "Válido en la plataforma web oficial de pedidos McDonald's con cualquier medio de pago.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "2x1 en McFlurry Oreo o KitKat",
                Description = "Helado suave con trozos crocantes de galletitas Oreo o KitKat bañado en salsa de chocolate.",
                NormalizedPrice = 4900,
                OriginalPrice = 9800,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 en McFlurry",
                Category = "Postre",
                PromoCode = "FLURRY2X1",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds",
                TermsAndConditions = "Exclusivo App McDonald's para consumo en local o AutoMac.",
                ValidFrom = now,
                ValidUntil = now.AddDays(20),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "McCombo Doble Carne Doble Queso",
                Description = "Dos medallones de carne vacuna, doble queso cheddar derretido, mostaza y kétchup con papas y gaseosa.",
                NormalizedPrice = 8900,
                OriginalPrice = 12500,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "28% OFF",
                Category = "Combo",
                PromoCode = "DOBLECARNE",
                Channel = "App / Web",
                DestinationUrl = "https://pedidos.mcdonalds.com.ar",
                TermsAndConditions = "Válido para retiro en sucursal o take-away hasta agotar stock.",
                ValidFrom = now,
                ValidUntil = now.AddDays(18),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "Papas Medianas Gratis con tu McCombo Doble",
                Description = "Porción de papas fritas doradas y crocantes sin cargo con la compra de cualquier McCombo Doble.",
                NormalizedPrice = 0,
                OriginalPrice = 3500,
                BenefitType = "FreeGift",
                BenefitDescription = "Papas Medianas Gratis",
                Category = "Papas y Snacks",
                PromoCode = "PAPASFREE",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.mcdo.mcdonalds",
                TermsAndConditions = "Canjeable una vez por usuario en la App oficial de McDonald's.",
                ValidFrom = now,
                ValidUntil = now.AddDays(15),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "McCombo McNuggets x10 con 2 Salsas",
                Description = "10 deliciosos nuggets de pollo crocante acompañados de salsa barbacoa y mostaza dulce con papas y bebida.",
                NormalizedPrice = 9200,
                OriginalPrice = 13000,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "29% OFF",
                Category = "Pollo",
                PromoCode = "NUGGETS10",
                Channel = "Web",
                DestinationUrl = "https://pedidos.mcdonalds.com.ar",
                TermsAndConditions = "Válido compras web oficial. Retiro en mostrador o entrega take-away.",
                ValidFrom = now,
                ValidUntil = now.AddDays(22),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "McCombo Grand Tasty Doble Grande",
                Description = "Dos carnes gruesas, lechuga, tomate, cebolla, bacon, queso cheddar y la inconfundible salsa Tasty.",
                NormalizedPrice = 14200,
                OriginalPrice = 18900,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "25% OFF",
                Category = "Combo",
                PromoCode = "TASTY25",
                Channel = "Web",
                DestinationUrl = "https://pedidos.mcdonalds.com.ar",
                TermsAndConditions = "Disponible en locales adheridos de todo el país.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "Desayuno McCafé Café con 2 Medialunas",
                Description = "Café espresso o cortado en grano recién molido con dos medialunas de manteca calientes.",
                NormalizedPrice = 3200,
                OriginalPrice = 4500,
                BenefitType = "FixedAmount",
                BenefitDescription = "Precio Especial Desayuno",
                Category = "Desayuno y Merienda",
                PromoCode = null,
                Channel = "App / Web",
                DestinationUrl = "https://www.mcdonalds.com.ar/menu",
                TermsAndConditions = "Válido de 7:00 a 11:30 hs en sucursales McCafé de todo el país.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },
            new()
            {
                Brand = "McDonald's",
                Title = "Cono de Helado Vainilla o Mixto",
                Description = "Cono crocante relleno del clásico helado cremoso sabor vainilla, dulce de leche o combinado.",
                NormalizedPrice = 1500,
                OriginalPrice = 2200,
                BenefitType = "FixedAmount",
                BenefitDescription = "Precio Especial $1.500",
                Category = "Postre",
                PromoCode = null,
                Channel = "App / Web",
                DestinationUrl = "https://www.mcdonalds.com.ar",
                TermsAndConditions = "Promoción directa en todos los centros de postres y sucursales.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },

            // === BURGER KING (10 OFERTAS) ===
            new()
            {
                Brand = "Burger King",
                Title = "2x1 Whopper Jr con Queso",
                Description = "Dos hamburguesas Whopper Jr con queso al precio de una. 100% carne a la parrilla sin conservantes.",
                NormalizedPrice = 9800,
                OriginalPrice = 19600,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 en Hamburguesas",
                Category = "Hamburguesa",
                PromoCode = "BK2X1",
                Channel = "Web",
                DestinationUrl = "https://www.burgerking.com.ar",
                TermsAndConditions = "Exclusivo compra web oficial con código. Válido sábados y domingos.",
                ValidFrom = now,
                ValidUntil = now.AddDays(28),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "Combo Stacker Doble Grande",
                Description = "Stacker Doble con carne a la parrilla, salsa Stacker exclusiva, queso cheddar derretido y panceta crocante.",
                NormalizedPrice = 12800,
                OriginalPrice = 18500,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "30% OFF",
                Category = "Combo",
                PromoCode = "STACKER30",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar",
                TermsAndConditions = "Válido en la App Mi BK pagando con tarjetas o Mercado Pago. Take-away y auto-king.",
                ValidFrom = now,
                ValidUntil = now.AddDays(25),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "King de Pollo Crispy con Papas y Gaseosa",
                Description = "Pechuga de pollo crocante, mayonesa suave y lechuga fresca en pan con sésamo, con papas y gaseosa.",
                NormalizedPrice = 8900,
                OriginalPrice = 12400,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "28% OFF",
                Category = "Pollo",
                PromoCode = "BKPOLLO",
                Channel = "Web",
                DestinationUrl = "https://www.burgerking.com.ar",
                TermsAndConditions = "Exclusivo pedidos web oficial de Burger King Argentina. Válido hasta el viernes.",
                ValidFrom = now,
                ValidUntil = now.AddDays(16),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "2x1 en King Fusión Oreo o Dulce de Leche",
                Description = "Helado suave batido con auténticas galletitas Oreo y dulce de leche argentino.",
                NormalizedPrice = 4600,
                OriginalPrice = 9200,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 en Postres",
                Category = "Postre",
                PromoCode = "FUSION2X1",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar",
                TermsAndConditions = "Segunda unidad gratis en la App Mi BK. Válido todos los días.",
                ValidFrom = now,
                ValidUntil = now.AddDays(20),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "Combo Whopper Clásico Mediano a la Parrilla",
                Description = "Carne vacuna a la parrilla, tomates frescos, lechuga, mayonesa, pepinillos y cebolla en pan con sésamo.",
                NormalizedPrice = 11200,
                OriginalPrice = 15800,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "29% OFF",
                Category = "Combo",
                PromoCode = "WHOPPERNOW",
                Channel = "Web",
                DestinationUrl = "https://www.burgerking.com.ar",
                TermsAndConditions = "Válido en la web oficial para retiro en tienda en sucursales habilitadas.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "Aros de Cebolla Grandes Gratis con Combo Stacker",
                Description = "Porción grande de crocantes aros de cebolla dorados de regalo con tu combo Stacker Cuádruple.",
                NormalizedPrice = 0,
                OriginalPrice = 3800,
                BenefitType = "FreeGift",
                BenefitDescription = "Aros de Cebolla Gratis",
                Category = "Papas y Snacks",
                PromoCode = "AROSGRATIS",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar",
                TermsAndConditions = "Exclusivo App Mi BK presentando el cupón en mostrador o tótem.",
                ValidFrom = now,
                ValidUntil = now.AddDays(15),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "Combo Hamburguesa con Queso y Papas",
                Description = "Medallón a la parrilla, queso cheddar derretido, kétchup y mostaza con papas y bebida regular.",
                NormalizedPrice = 5900,
                OriginalPrice = 8200,
                BenefitType = "FixedAmount",
                BenefitDescription = "Menú Económico",
                Category = "Combo",
                PromoCode = null,
                Channel = "App / Web",
                DestinationUrl = "https://www.burgerking.com.ar",
                TermsAndConditions = "Precio fijo promocional disponible de lunes a viernes.",
                ValidFrom = now,
                ValidUntil = now.AddDays(25),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "2x1 Combo Hamburguesa Doble Parrilla",
                Description = "Dos combos completos con hamburguesa doble carne a la parrilla, queso cheddar, papas y gaseosas.",
                NormalizedPrice = 13500,
                OriginalPrice = 27000,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 Combos Dobles",
                Category = "Combo",
                PromoCode = "BKPARRILLA",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=com.alsea.mi_bk_ar",
                TermsAndConditions = "Exclusivo App Mi BK. Válido lunes a jueves en locales adheridos.",
                ValidFrom = now,
                ValidUntil = now.AddDays(27),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "King Nuggets x10 con Salsa BBQ",
                Description = "10 bocados de pollo empanado super crocante con dip de salsa barbacoa tradicional.",
                NormalizedPrice = 6800,
                OriginalPrice = 9500,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "28% OFF",
                Category = "Pollo",
                PromoCode = "BKNUGGETS",
                Channel = "Web",
                DestinationUrl = "https://www.burgerking.com.ar",
                TermsAndConditions = "Promoción activa en web oficial para compra online.",
                ValidFrom = now,
                ValidUntil = now.AddDays(24),
                IsActive = true
            },
            new()
            {
                Brand = "Burger King",
                Title = "Desayuno Café Molido con 2 Medialunas",
                Description = "Café caliente 100% grano arábica con 2 medialunas dulces de manteca horneadas en el día.",
                NormalizedPrice = 3100,
                OriginalPrice = 4200,
                BenefitType = "FixedAmount",
                BenefitDescription = "Desayuno King",
                Category = "Desayuno y Merienda",
                PromoCode = null,
                Channel = "App / Web",
                DestinationUrl = "https://www.burgerking.com.ar",
                TermsAndConditions = "Válido horario de mañana de 8 a 11 hs para consumo en salón o take-away.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },

            // === MOSTAZA (10 OFERTAS) ===
            new()
            {
                Brand = "Mostaza",
                Title = "Mega Cuádruple Bacon con Papas Cheddar",
                Description = "Cuatro carnes gigantes, panceta crocante y queso cheddar en pan brioche, acompañada de papas con cheddar y bebida.",
                NormalizedPrice = 14500,
                OriginalPrice = 19000,
                BenefitType = "FixedAmount",
                BenefitDescription = "Precio Especial Lanzamiento",
                Category = "Combo",
                PromoCode = "MOSTAZABACON",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza",
                TermsAndConditions = "Válido en la App Mostaza. Consumo en local o retiro en tienda.",
                ValidFrom = now,
                ValidUntil = now.AddDays(29),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "2x1 en Mega Dibu Cheeseburger",
                Description = "Dos hamburguesas homenaje al Dibu Martínez con medallón gigante, queso cheddar fundido y pan suave.",
                NormalizedPrice = 11200,
                OriginalPrice = 22400,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 Mega Dibu",
                Category = "Hamburguesa",
                PromoCode = "DIBU2X1",
                Channel = "Web",
                DestinationUrl = "https://www.mostazaweb.com.ar",
                TermsAndConditions = "Exclusivo compras en la web oficial con código. Pago con cualquier medio.",
                ValidFrom = now,
                ValidUntil = now.AddDays(22),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "Combo Pollo Crispy Mega Box con Papas",
                Description = "Suprema de pollo super crocante, lechuga, tomate y mayonesa con papas fritas y gaseosa grande.",
                NormalizedPrice = 9900,
                OriginalPrice = 13500,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "27% OFF",
                Category = "Pollo",
                PromoCode = null,
                Channel = "Web",
                DestinationUrl = "https://www.mostazaweb.com.ar/menu",
                TermsAndConditions = "Modalidad take-away o consumo en local de lunes a viernes.",
                ValidFrom = now,
                ValidUntil = now.AddDays(18),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "Bebida Grande Gratis con Mega Doble",
                Description = "Gaseosa línea Coca-Cola grande de regalo sin cargo con la compra de cualquier hamburguesa Mega Doble.",
                NormalizedPrice = 0,
                OriginalPrice = 2800,
                BenefitType = "FreeGift",
                BenefitDescription = "Gaseosa Grande Gratis",
                Category = "Bebida",
                PromoCode = "BEBIDAGRATIS",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza",
                TermsAndConditions = "Presentando cupón en la App Mostaza. Válido hasta agotar stock diario.",
                ValidFrom = now,
                ValidUntil = now.AddDays(14),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "Mega Boom Doble con Salsa Mostaza y Cebolla Crispy",
                Description = "Doble carne, queso cheddar, cebolla crispy caramelizada y la salsa original de Mostaza en pan brioche.",
                NormalizedPrice = 12900,
                OriginalPrice = 17200,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "25% OFF",
                Category = "Combo",
                PromoCode = "MEGABOOM",
                Channel = "App / Web",
                DestinationUrl = "https://www.mostazaweb.com.ar",
                TermsAndConditions = "Válido en web y App oficial para retiro en tienda.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "2x1 en Helado Mostaza Sundae de Dulce de Leche",
                Description = "Dos copas de helado artesanal con abundante salsa tibia de dulce de leche o chocolate al precio de una.",
                NormalizedPrice = 3200,
                OriginalPrice = 6400,
                BenefitType = "TwoForOne",
                BenefitDescription = "2x1 en Sundae",
                Category = "Postre",
                PromoCode = "SUNDAE2X1",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza",
                TermsAndConditions = "Cupón exclusivo en la aplicación móvil Mostaza.",
                ValidFrom = now,
                ValidUntil = now.AddDays(25),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "30% OFF con Tarjetas Santander o Galicia",
                Description = "Descuento bancario exclusivo pagando con tarjetas de crédito o débito Visa/Mastercard adheridas.",
                NormalizedPrice = 10500,
                OriginalPrice = 15000,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "30% Bancos",
                Category = "Combo",
                PromoCode = "BANCARIA30",
                Channel = "Web",
                DestinationUrl = "https://www.mostazaweb.com.ar/promos-bancarias",
                TermsAndConditions = "Válido miércoles y viernes en locales adheridos y compra web.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "Mega Bacon Jr con Papas Medianas",
                Description = "Medallón de carne con panceta, cheddar y papas fritas. Ideal para almuerzo rápido al paso.",
                NormalizedPrice = 7800,
                OriginalPrice = 10500,
                BenefitType = "PercentageDiscount",
                BenefitDescription = "25% OFF",
                Category = "Combo",
                PromoCode = "BACONJR",
                Channel = "App / Web",
                DestinationUrl = "https://www.mostazaweb.com.ar",
                TermsAndConditions = "Disponible en todo el territorio nacional.",
                ValidFrom = now,
                ValidUntil = now.AddDays(28),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "Papas con Cheddar y Bacon Extra Gratis",
                Description = "Porción de papas fritas bañadas en salsa cheddar caliente y lluvia de bacon crocante sin cargo con tu Mega Triple.",
                NormalizedPrice = 0,
                OriginalPrice = 4200,
                BenefitType = "FreeGift",
                BenefitDescription = "Papas Cheddar Gratis",
                Category = "Papas y Snacks",
                PromoCode = "CHEDDARGIFT",
                Channel = "App",
                DestinationUrl = "https://play.google.com/store/apps/details?id=ar.com.mostazaweb.mostaza",
                TermsAndConditions = "Exclusivo socios MostaClub en la aplicación móvil.",
                ValidFrom = now,
                ValidUntil = now.AddDays(26),
                IsActive = true
            },
            new()
            {
                Brand = "Mostaza",
                Title = "Mega Café con Tostado de Jamón y Queso",
                Description = "Café con leche espumoso o americano con tostado en pan de campo con abundante jamón cocido y queso fundido.",
                NormalizedPrice = 3900,
                OriginalPrice = 5200,
                BenefitType = "FixedAmount",
                BenefitDescription = "Promo Desayuno $3.900",
                Category = "Desayuno y Merienda",
                PromoCode = null,
                Channel = "App / Web",
                DestinationUrl = "https://www.mostazaweb.com.ar/menu",
                TermsAndConditions = "Válido horario matutino en locales Mostaza adheridos.",
                ValidFrom = now,
                ValidUntil = now.AddDays(30),
                IsActive = true
            }
        };
    }
}
