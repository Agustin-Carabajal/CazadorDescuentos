using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Modules.CatalogAndUserExperience.Services;
using Microsoft.AspNetCore.Mvc;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Controllers;

public class CatalogController : Controller
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [Route("/")]
    [Route("/Catalog")]
    [Route("/Catalog/Index")]
    public async Task<IActionResult> Index(
        [FromQuery] string? brand,
        [FromQuery] string? benefitType,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var filter = new OfferFilterDto
        {
            Brand = brand,
            BenefitType = benefitType,
            Category = category,
            SearchQuery = search,
            SortBy = sort
        };

        var offers = await _catalogService.GetActiveOffersAsync(filter, cancellationToken);

        ViewBag.SelectedBrand = brand ?? "all";
        ViewBag.SelectedBenefitType = benefitType ?? "all";
        ViewBag.SelectedCategory = category ?? "all";
        ViewBag.SearchQuery = search ?? "";
        ViewBag.SortBy = sort ?? "newest";

        return View(offers);
    }

    [HttpGet]
    [Route("api/catalog/offers")]
    public async Task<IActionResult> GetOffersApi([FromQuery] OfferFilterDto filter, CancellationToken cancellationToken)
    {
        var offers = await _catalogService.GetActiveOffersAsync(filter, cancellationToken);
        return Ok(offers);
    }

    [HttpGet]
    [Route("api/catalog/offers/{id:int}")]
    public async Task<IActionResult> GetOfferByIdApi(int id, CancellationToken cancellationToken)
    {
        var offer = await _catalogService.GetOfferByIdAsync(id, cancellationToken);
        if (offer == null)
        {
            return NotFound(new { message = $"Oferta #{id} no encontrada." });
        }
        return Ok(offer);
    }
}
