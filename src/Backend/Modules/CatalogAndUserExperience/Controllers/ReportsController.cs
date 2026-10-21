using CazadorDescuentos.Modules.CatalogAndUserExperience.Models;
using CazadorDescuentos.Modules.CatalogAndUserExperience.Services;
using Microsoft.AspNetCore.Mvc;

namespace CazadorDescuentos.Modules.CatalogAndUserExperience.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    [Route("Reports")]
    [Route("Reports/Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reports = await _reportService.GetRecentReportsAsync(cancellationToken);
        return View(reports);
    }

    [HttpPost]
    [Route("Reports/Create")]
    [Route("api/reports")]
    public async Task<IActionResult> Create([FromBody] CreateReportDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var report = await _reportService.CreateReportAsync(dto, ip, cancellationToken);
            return Ok(new
            {
                success = true,
                message = "Reporte registrado correctamente. Gracias por colaborar con la comunidad.",
                reportId = report.Id
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error al procesar el reporte: " + ex.Message });
        }
    }

    [HttpGet]
    [Route("api/reports")]
    public async Task<IActionResult> GetReportsApi(CancellationToken cancellationToken)
    {
        var reports = await _reportService.GetRecentReportsAsync(cancellationToken);
        return Ok(reports);
    }
}
