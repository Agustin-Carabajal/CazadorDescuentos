using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserReport> CreateReportAsync(
        CreateReportDto dto,
        string? reporterIp = null,
        CancellationToken cancellationToken = default)
    {
        var offer = await _context.Offers.FirstOrDefaultAsync(o => o.Id == dto.OfferId, cancellationToken);
        if (offer == null)
        {
            throw new KeyNotFoundException($"Offer with ID {dto.OfferId} not found.");
        }

        var report = new UserReport
        {
            OfferId = dto.OfferId,
            Reason = dto.Reason,
            Comments = dto.Comments,
            ReporterIp = reporterIp,
            ReportedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _context.UserReports.Add(report);

        // Increment report count on the offer
        offer.ReportCount += 1;
        offer.UpdatedAt = DateTime.UtcNow;

        // If report states offer is expired or reports exceed threshold, flag or deactivate
        if (dto.Reason.Equals("Expired", StringComparison.OrdinalIgnoreCase) || offer.ReportCount >= 3)
        {
            _logger.LogWarning("Offer {OfferId} received alert report ({Reason}). Total reports: {Count}", offer.Id, dto.Reason, offer.ReportCount);
            if (offer.ReportCount >= 5)
            {
                offer.IsActive = false; // Auto-deactivate heavily reported anomalous offers
                _logger.LogWarning("Offer {OfferId} auto-deactivated due to multiple user reports.", offer.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("New user report created for Offer {OfferId} with reason: {Reason}", dto.OfferId, dto.Reason);

        return report;
    }

    public async Task<List<UserReport>> GetRecentReportsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserReports
            .Include(r => r.Offer)
            .OrderByDescending(r => r.ReportedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
    }
}
