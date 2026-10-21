using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Services;

public interface ICatalogService
{
    Task<List<OfferResponseDto>> GetActiveOffersAsync(OfferFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<OfferResponseDto?> GetOfferByIdAsync(int id, CancellationToken cancellationToken = default);
}
