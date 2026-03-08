using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

public class CalendarService : ICalendarService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<CalendarService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────
    // Get Events (with filtering)
    // ──────────────────────────────────────────────

    public async Task<CalendarEventListResult> GetEventsAsync(int companyId, int userId, string userRole, CalendarEventFilterRequest filter)
    {
        try
        {
            var query = _context.CalendarEvents
                .Include(e => e.Creator)
                .Include(e => e.Project)
                .Where(e => e.CompanyID == companyId);

            // Apply filters
            if (filter.ProjectId.HasValue)
                query = query.Where(e => e.ProjectID == filter.ProjectId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Priority))
                query = query.Where(e => e.Priority == filter.Priority);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(term) ||
                                         (e.Description != null && e.Description.ToLower().Contains(term)) ||
                                         (e.Location != null && e.Location.ToLower().Contains(term)));
            }

            if (filter.StartDate.HasValue)
                query = query.Where(e => e.EndDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.StartDate <= filter.EndDate.Value);

            var totalCount = await query.CountAsync();

            var events = await query
                .OrderBy(e => e.StartDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(e => MapToItem(e))
                .ToListAsync();

            return new CalendarEventListResult { Success = true, Events = events, TotalCount = totalCount };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching calendar events for company {CompanyId}", companyId);
            return new CalendarEventListResult { Success = false, ErrorMessage = "Failed to load calendar events." };
        }
    }

    public async Task<CalendarEventListResult> GetEventsByDateRangeAsync(int companyId, int userId, string userRole, DateTime start, DateTime end, int? projectId)
    {
        var filter = new CalendarEventFilterRequest
        {
            StartDate = start,
            EndDate = end,
            ProjectId = projectId,
            PageSize = 500 // Calendar view can show many events
        };
        return await GetEventsAsync(companyId, userId, userRole, filter);
    }

    // ──────────────────────────────────────────────
    // Get Event By Id
    // ──────────────────────────────────────────────

    public async Task<CalendarEventDetailResult> GetEventByIdAsync(int companyId, int eventId)
    {
        try
        {
            var evt = await _context.CalendarEvents
                .Include(e => e.Creator)
                .Include(e => e.Project)
                .FirstOrDefaultAsync(e => e.EventID == eventId && e.CompanyID == companyId);

            if (evt == null)
                return new CalendarEventDetailResult { Success = false, ErrorMessage = "Event not found." };

            return new CalendarEventDetailResult { Success = true, Event = MapToItem(evt) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching calendar event {EventId}", eventId);
            return new CalendarEventDetailResult { Success = false, ErrorMessage = "Failed to load event details." };
        }
    }

    // ──────────────────────────────────────────────
    // Create Event
    // ──────────────────────────────────────────────

    public async Task<CreateCalendarEventResult> CreateEventAsync(int companyId, int userId, string userRole, CreateCalendarEventRequest request)
    {
        try
        {
            // Only CompanyAdmin and ProjectManager can create
            if (userRole != "CompanyAdmin" && userRole != "ProjectManager")
                return new CreateCalendarEventResult { Success = false, ErrorMessage = "You do not have permission to create events." };

            if (string.IsNullOrWhiteSpace(request.Title))
                return new CreateCalendarEventResult { Success = false, ErrorMessage = "Event title is required." };

            if (request.EndDate < request.StartDate)
                return new CreateCalendarEventResult { Success = false, ErrorMessage = "End date must be after start date." };

            var calendarEvent = new CalendarEvent
            {
                CompanyID = companyId,
                CreatedBy = userId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AllDay = request.AllDay,
                Location = request.Location?.Trim(),
                Priority = request.Priority ?? "Medium",
                Color = request.Color,
                ProjectID = request.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarEvents.Add(calendarEvent);

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = "Created calendar event",
                EntityName = "CalendarEvent",
                EntityID = 0, // Will be set after save
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Update audit log EntityID
            var auditLog = await _context.AuditLogs
                .Where(a => a.UserID == userId && a.EntityName == "CalendarEvent" && a.EntityID == 0)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            if (auditLog != null)
            {
                auditLog.EntityID = calendarEvent.EventID;
                await _context.SaveChangesAsync();
            }

            // Notify team members about new event
            await NotifyTeamAboutEventAsync(companyId, userId, calendarEvent.Title, "created");

            _logger.LogInformation("User {UserId} created calendar event {EventId} for company {CompanyId}", userId, calendarEvent.EventID, companyId);
            return new CreateCalendarEventResult { Success = true, EventId = calendarEvent.EventID };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar event for company {CompanyId}", companyId);
            return new CreateCalendarEventResult { Success = false, ErrorMessage = "Failed to create event." };
        }
    }

    // ──────────────────────────────────────────────
    // Update Event
    // ──────────────────────────────────────────────

    public async Task<ServiceResult> UpdateEventAsync(int companyId, int userId, string userRole, int eventId, UpdateCalendarEventRequest request)
    {
        try
        {
            // Only CompanyAdmin and ProjectManager can edit
            if (userRole != "CompanyAdmin" && userRole != "ProjectManager")
                return new ServiceResult { Success = false, ErrorMessage = "You do not have permission to edit events." };

            var evt = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.EventID == eventId && e.CompanyID == companyId);

            if (evt == null)
                return new ServiceResult { Success = false, ErrorMessage = "Event not found." };

            if (string.IsNullOrWhiteSpace(request.Title))
                return new ServiceResult { Success = false, ErrorMessage = "Event title is required." };

            if (request.EndDate < request.StartDate)
                return new ServiceResult { Success = false, ErrorMessage = "End date must be after start date." };

            evt.Title = request.Title.Trim();
            evt.Description = request.Description?.Trim();
            evt.StartDate = request.StartDate;
            evt.EndDate = request.EndDate;
            evt.AllDay = request.AllDay;
            evt.Location = request.Location?.Trim();
            evt.Priority = request.Priority ?? "Medium";
            evt.Color = request.Color;
            evt.ProjectID = request.ProjectId;
            evt.UpdatedAt = DateTime.UtcNow;

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = "Updated calendar event",
                EntityName = "CalendarEvent",
                EntityID = eventId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify about update
            await NotifyTeamAboutEventAsync(companyId, userId, evt.Title, "updated");

            _logger.LogInformation("User {UserId} updated calendar event {EventId}", userId, eventId);
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar event {EventId}", eventId);
            return new ServiceResult { Success = false, ErrorMessage = "Failed to update event." };
        }
    }

    // ──────────────────────────────────────────────
    // Delete Event
    // ──────────────────────────────────────────────

    public async Task<ServiceResult> DeleteEventAsync(int companyId, int userId, string userRole, int eventId)
    {
        try
        {
            // Only CompanyAdmin and ProjectManager can delete
            if (userRole != "CompanyAdmin" && userRole != "ProjectManager")
                return new ServiceResult { Success = false, ErrorMessage = "You do not have permission to delete events." };

            var evt = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.EventID == eventId && e.CompanyID == companyId);

            if (evt == null)
                return new ServiceResult { Success = false, ErrorMessage = "Event not found." };

            var eventTitle = evt.Title;
            _context.CalendarEvents.Remove(evt);

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = "Deleted calendar event",
                EntityName = "CalendarEvent",
                EntityID = eventId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify about deletion
            await NotifyTeamAboutEventAsync(companyId, userId, eventTitle, "deleted");

            _logger.LogInformation("User {UserId} deleted calendar event {EventId}", userId, eventId);
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar event {EventId}", eventId);
            return new ServiceResult { Success = false, ErrorMessage = "Failed to delete event." };
        }
    }

    // ──────────────────────────────────────────────
    // Get Projects for Filter
    // ──────────────────────────────────────────────

    public async Task<List<CalendarProjectItem>> GetProjectsForFilterAsync(int companyId, int userId, string userRole)
    {
        try
        {
            var query = _context.Projects.Where(p => p.CompanyID == companyId && p.Status != "Archived");

            // TeamMember only sees projects they're a member of
            if (userRole == "TeamMember")
            {
                query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            else if (userRole == "ProjectManager")
            {
                query = query.Where(p => p.CreatedBy == userId || p.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            return await query
                .OrderBy(p => p.ProjectName)
                .Select(p => new CalendarProjectItem
                {
                    ProjectId = p.ProjectID,
                    ProjectName = p.ProjectName
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching projects for calendar filter");
            return new List<CalendarProjectItem>();
        }
    }

    // ──────────────────────────────────────────────
    // Private Helpers
    // ──────────────────────────────────────────────

    private static CalendarEventItem MapToItem(CalendarEvent e)
    {
        return new CalendarEventItem
        {
            EventId = e.EventID,
            Title = e.Title,
            Description = e.Description,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            AllDay = e.AllDay,
            Location = e.Location,
            Priority = e.Priority,
            Color = e.Color,
            ProjectId = e.ProjectID,
            ProjectName = e.Project?.ProjectName,
            CreatedBy = e.CreatedBy,
            CreatorName = e.Creator != null ? $"{e.Creator.Fname} {e.Creator.Lname}" : "Unknown",
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    private async System.Threading.Tasks.Task NotifyTeamAboutEventAsync(int companyId, int userId, string eventTitle, string action)
    {
        try
        {
            // Notify all company users (except the actor) about event changes
            var userIds = await _context.Users
                .Where(u => u.CompanyID == companyId && u.UserID != userId && u.Status == "Active")
                .Select(u => u.UserID)
                .ToListAsync();

            if (userIds.Any())
            {
                var creator = await _context.Users.FindAsync(userId);
                var creatorName = creator != null ? $"{creator.Fname} {creator.Lname}" : "Someone";
                var message = $"{creatorName} {action} a calendar event: \"{eventTitle}\"";
                await _notificationService.SendBulkNotificationAsync(userIds, "Calendar", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send calendar notification for company {CompanyId}", companyId);
        }
    }
}
