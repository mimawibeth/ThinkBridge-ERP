using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize(Policy = "TeamMemberOnly")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(ICalendarService calendarService, ILogger<CalendarController> logger)
    {
        _calendarService = calendarService;
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

    // ──────────────────────────────────────────────
    // Events
    // ──────────────────────────────────────────────

    /// <summary>
    /// Get calendar events with filtering
    /// </summary>
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] int? projectId,
        [FromQuery] string? search,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var filter = new CalendarEventFilterRequest
        {
            StartDate = start,
            EndDate = end,
            ProjectId = projectId,
            SearchTerm = search,
            Priority = priority,
            Page = page,
            PageSize = pageSize
        };

        var result = await _calendarService.GetEventsAsync(companyId, userId, userRole, filter);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Events,
            totalCount = result.TotalCount
        });
    }

    /// <summary>
    /// Get events by date range (for calendar view)
    /// </summary>
    [HttpGet("events/range")]
    public async Task<IActionResult> GetEventsByRange(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int? projectId)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _calendarService.GetEventsByDateRangeAsync(companyId, userId, userRole, start, end, projectId);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Events
        });
    }

    /// <summary>
    /// Get a single event by ID
    /// </summary>
    [HttpGet("events/{id}")]
    public async Task<IActionResult> GetEventById(int id)
    {
        var companyId = GetCurrentCompanyId();

        var result = await _calendarService.GetEventByIdAsync(companyId, id);

        if (!result.Success)
            return NotFound(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Event
        });
    }

    /// <summary>
    /// Create a new calendar event (CompanyAdmin / ProjectManager only)
    /// </summary>
    [HttpPost("events")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateCalendarEventRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { success = false, message = "Event title is required." });

        var result = await _calendarService.CreateEventAsync(companyId, userId, userRole, request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            eventId = result.EventId,
            message = "Event created successfully."
        });
    }

    /// <summary>
    /// Update an existing calendar event (CompanyAdmin / ProjectManager only)
    /// </summary>
    [HttpPut("events/{id}")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateCalendarEventRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _calendarService.UpdateEventAsync(companyId, userId, userRole, id, request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            message = "Event updated successfully."
        });
    }

    /// <summary>
    /// Delete a calendar event (CompanyAdmin / ProjectManager only)
    /// </summary>
    [HttpDelete("events/{id}")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _calendarService.DeleteEventAsync(companyId, userId, userRole, id);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            message = "Event deleted successfully."
        });
    }

    /// <summary>
    /// Get projects for the filter dropdown
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var projects = await _calendarService.GetProjectsForFilterAsync(companyId, userId, userRole);

        return Ok(new
        {
            success = true,
            data = projects
        });
    }
}
