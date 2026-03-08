using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Services;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "ProjectManagerOnly")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly PdfReportService _pdfReportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, PdfReportService pdfReportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _pdfReportService = pdfReportService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private int GetCurrentCompanyId()
    {
        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CompanyID")?.Value;
        return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
    }

    private string GetCurrentUserRole()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "PrimaryRole")?.Value ?? "TeamMember";
    }

    /// <summary>
    /// Get report dashboard KPI cards
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.GetReportDashboardAsync(companyId, userId, role, dateFrom, dateTo);
        return result.Success
            ? Ok(new { success = true, data = result })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get project progress data
    /// </summary>
    [HttpGet("project-progress")]
    public async Task<IActionResult> GetProjectProgress([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.GetProjectProgressAsync(companyId, userId, role, dateFrom, dateTo);
        return result.Success
            ? Ok(new { success = true, data = result.Projects })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get task distribution data
    /// </summary>
    [HttpGet("task-distribution")]
    public async Task<IActionResult> GetTaskDistribution([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.GetTaskDistributionAsync(companyId, userId, role, dateFrom, dateTo);
        return result.Success
            ? Ok(new { success = true, data = result })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get team performance table data
    /// </summary>
    [HttpGet("team-performance")]
    public async Task<IActionResult> GetTeamPerformance([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.GetTeamPerformanceAsync(companyId, userId, role, dateFrom, dateTo);
        return result.Success
            ? Ok(new { success = true, data = result.Members })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get saved reports list
    /// </summary>
    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedReports()
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.GetSavedReportsAsync(companyId, userId);
        return result.Success
            ? Ok(new { success = true, data = result.Reports })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Save a report
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveReport([FromBody] SaveReportRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (string.IsNullOrWhiteSpace(request.ReportName))
            return BadRequest(new { success = false, message = "Report name is required." });

        var result = await _reportService.SaveReportAsync(companyId, userId, request);
        return result.Success
            ? Ok(new { success = true, data = new { reportId = result.ReportId } })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Delete a saved report
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReport(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.DeleteReportAsync(companyId, userId, id);
        return result.Success
            ? Ok(new { success = true })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get Company Admin report (users, subscription status)
    /// </summary>
    [HttpGet("company-overview")]
    [Authorize(Policy = "CompanyAdminOnly")]
    public async Task<IActionResult> GetCompanyReport()
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _reportService.GetCompanyReportAsync(companyId);
        return result.Success
            ? Ok(new { success = true, data = result })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    // ========================
    // PDF DOWNLOAD ENDPOINTS
    // ========================

    /// <summary>
    /// Download Project Manager PDF report (role-scoped)
    /// </summary>
    [HttpGet("download-pdf")]
    public async Task<IActionResult> DownloadPmPdf([FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var fullName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Unknown";
        var companyName = User.Claims.FirstOrDefault(c => c.Type == "CompanyName")?.Value ?? "Unknown";

        if (companyId == 0) return BadRequest("Invalid company context.");

        try
        {
            var period = "month";
            if (dateFrom.HasValue && dateTo.HasValue)
            {
                var days = (dateTo.Value - dateFrom.Value).TotalDays;
                period = days <= 7 ? "week" : days <= 31 ? "month" : days <= 93 ? "quarter" : "year";
            }
            var pdfBytes = await _pdfReportService.GenerateProjectManagerPdfAsync(companyId, userId, role, fullName, companyName, period);
            var fileName = $"PM-Report-{DateTime.UtcNow:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PM PDF report.");
            return StatusCode(500, "Failed to generate PDF report.");
        }
    }

    /// <summary>
    /// Download Company Admin PDF report (company-scoped)
    /// </summary>
    [HttpGet("company-overview/download-pdf")]
    [Authorize(Policy = "CompanyAdminOnly")]
    public async Task<IActionResult> DownloadCompanyAdminPdf()
    {
        var companyId = GetCurrentCompanyId();
        var fullName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Unknown";
        var companyName = User.Claims.FirstOrDefault(c => c.Type == "CompanyName")?.Value ?? "Unknown";

        if (companyId == 0) return BadRequest("Invalid company context.");

        try
        {
            var pdfBytes = await _pdfReportService.GenerateCompanyAdminPdfAsync(companyId, fullName, companyName);
            var fileName = $"Company-Report-{DateTime.UtcNow:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Company Admin PDF report.");
            return StatusCode(500, "Failed to generate PDF report.");
        }
    }
}
