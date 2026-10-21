using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Services;

public interface IReportService
{
    Task<UserReport> CreateReportAsync(CreateReportDto dto, string? reporterIp = null, CancellationToken cancellationToken = default);
    Task<List<UserReport>> GetRecentReportsAsync(CancellationToken cancellationToken = default);
}
