using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using Microsoft.EntityFrameworkCore;

namespace CazadorDescuentos.Shared.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (!await context.Offers.AnyAsync())
            {
                logger.LogInformation("Database is empty. Seeding initial baseline deals for McDonald's, Burger King, and Mostaza...");

                var seedOffers = new List<Offer>
                {
                    new()
                    {
                        Brand = "McDonald's",
                        Title = "Combo Cuarto de Libra Mediano",
                        Description = "Incluye hamburguesa Cuarto de Libra con Queso, papas medianas y gaseosa a elección.",
                        NormalizedPrice = 11500,
                        OriginalPrice = 15000,
                        BenefitType = "PercentageDiscount",
                        BenefitDescription = "23% OFF",
                        Category = "Combo",
                        PromoCode = "MCDOBLE24",
                        Channel = "App",
                        DestinationUrl = "https://www.mcdonalds.com.ar/promociones",
                        TermsAndConditions = "Exclusivo App McDonald's. Válido lunes a viernes para take-away o consumo en local.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(15),
                        IsActive = true
                    },
                    new()
                    {
                        Brand = "McDonald's",
                        Title = "2x1 en Sundae de Dulce de Leche",
                        Description = "Dos sundaes de dulce de leche o chocolate al precio de uno.",
                        NormalizedPrice = 3200,
                        OriginalPrice = 6400,
                        BenefitType = "TwoForOne",
                        BenefitDescription = "2x1 en Postres",
                        Category = "Postre",
                        PromoCode = "SUNDAE2X1",
                        Channel = "App",
                        DestinationUrl = "https://www.mcdonalds.com.ar/promociones",
                        TermsAndConditions = "Segunda unidad gratis presentando cupón en caja. Válido todos los días.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(7),
                        IsActive = true
                    },
                    new()
                    {
                        Brand = "Burger King",
                        Title = "2x1 Whopper Jr con Queso",
                        Description = "Dos Whopper Jr con queso al precio de una. Carne 100% vacuna a la parrilla.",
                        NormalizedPrice = 9800,
                        OriginalPrice = 19600,
                        BenefitType = "TwoForOne",
                        BenefitDescription = "2x1 en Hamburguesas",
                        Category = "Hamburguesa",
                        PromoCode = "BK2X1",
                        Channel = "Web",
                        DestinationUrl = "https://www.burgerking.com.ar/promociones",
                        TermsAndConditions = "Exclusivo compra web oficial con código. Válido sábados y domingos.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(20),
                        IsActive = true
                    },
                    new()
                    {
                        Brand = "Burger King",
                        Title = "Combo Stacker Doble Grande",
                        Description = "Stacker Doble con salsa especial, queso cheddar y panceta. Con papas o aros de cebolla y gaseosa.",
                        NormalizedPrice = 12800,
                        OriginalPrice = 18500,
                        BenefitType = "PercentageDiscount",
                        BenefitDescription = "30% OFF",
                        Category = "Combo",
                        PromoCode = "STACKER30",
                        Channel = "App / Web",
                        DestinationUrl = "https://www.burgerking.com.ar/promociones",
                        TermsAndConditions = "Pago con tarjeta Visa o Débito. Modalidad take-away y auto-king.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(10),
                        IsActive = true
                    },
                    new()
                    {
                        Brand = "Mostaza",
                        Title = "Mega Cuádruple Bacon con Papas Cheddar",
                        Description = "Cuatro carnes, panceta crocante y queso cheddar en pan brioche, acompañada de papas con cheddar y bebida.",
                        NormalizedPrice = 14500,
                        OriginalPrice = 19000,
                        BenefitType = "FixedAmount",
                        BenefitDescription = "Precio Especial Lanzamiento",
                        Category = "Combo",
                        PromoCode = "MOSTAZABACON",
                        Channel = "App",
                        DestinationUrl = "https://www.mostazaweb.com.ar/promociones",
                        TermsAndConditions = "Válido en la App Mostaza. Consumo en local o retiro en tienda.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(25),
                        IsActive = true
                    },
                    new()
                    {
                        Brand = "Mostaza",
                        Title = "2x1 Mega Dibu Cheeseburger",
                        Description = "Dos combos Mega Dibu Cheeseburger al precio de uno.",
                        NormalizedPrice = 11200,
                        OriginalPrice = 22400,
                        BenefitType = "TwoForOne",
                        BenefitDescription = "2x1 Mega Dibu",
                        Category = "Hamburguesa",
                        PromoCode = "DIBU2X1",
                        Channel = "Web",
                        DestinationUrl = "https://www.mostazaweb.com.ar/promociones",
                        TermsAndConditions = "Exclusivo compras web oficial. Pago con cualquier medio.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(12),
                        IsActive = true
                    },
                    new()
                    {
                        Brand = "Mostaza",
                        Title = "Bebida Grande Gratis con Mega Doble",
                        Description = "Gaseosa línea Coca-Cola grande de regalo con la compra de cualquier hamburguesa Mega Doble.",
                        NormalizedPrice = 0,
                        OriginalPrice = 2800,
                        BenefitType = "FreeGift",
                        BenefitDescription = "Gaseosa Grande Gratis",
                        Category = "Bebida",
                        PromoCode = "BEBIDAGRATIS",
                        Channel = "App",
                        DestinationUrl = "https://www.mostazaweb.com.ar/promociones",
                        TermsAndConditions = "Presentando cupón en la App. Válido hasta agotar stock diario.",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(14),
                        IsActive = true
                    }
                };

                context.Offers.AddRange(seedOffers);
                await context.SaveChangesAsync();
                logger.LogInformation("Database seeded with {Count} initial baseline offers.", seedOffers.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization / seeding.");
        }
    }
}
