using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services.Interfaces;

public interface ICalendarService
{
    // Events CRUD
    Task<CalendarEventListResult> GetEventsAsync(int companyId, int userId, string userRole, CalendarEventFilterRequest filter);
    Task<CalendarEventDetailResult> GetEventByIdAsync(int companyId, int eventId);
    Task<CreateCalendarEventResult> CreateEventAsync(int companyId, int userId, string userRole, CreateCalendarEventRequest request);
    Task<ServiceResult> UpdateEventAsync(int companyId, int userId, string userRole, int eventId, UpdateCalendarEventRequest request);
    Task<ServiceResult> DeleteEventAsync(int companyId, int userId, string userRole, int eventId);

    // Calendar helpers
    Task<CalendarEventListResult> GetEventsByDateRangeAsync(int companyId, int userId, string userRole, DateTime start, DateTime end, int? projectId);
    Task<List<CalendarProjectItem>> GetProjectsForFilterAsync(int companyId, int userId, string userRole);
}

// ─── Request DTOs ──────────────────────────────────────

public class CalendarEventFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ProjectId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Priority { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool AllDay { get; set; }
    public string? Location { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? Color { get; set; }
    public int? ProjectId { get; set; }
}

public class UpdateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool AllDay { get; set; }
    public string? Location { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? Color { get; set; }
    public int? ProjectId { get; set; }
}

// ─── Response DTOs ─────────────────────────────────────

public class CalendarEventListResult : ServiceResult
{
    public List<CalendarEventItem> Events { get; set; } = new();
    public int TotalCount { get; set; }
}

public class CalendarEventDetailResult : ServiceResult
{
    public CalendarEventItem? Event { get; set; }
}

public class CreateCalendarEventResult : ServiceResult
{
    public int EventId { get; set; }
}

public class CalendarEventItem
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool AllDay { get; set; }
    public string? Location { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? Color { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int CreatedBy { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CalendarProjectItem
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
