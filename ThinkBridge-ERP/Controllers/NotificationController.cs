using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "TeamMemberOnly")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get notifications for current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var userId = GetCurrentUserId();
        if (userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var result = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Notifications,
            unreadCount = result.UnreadCount,
            pagination = new { page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount, totalPages = result.TotalPages }
        });
    }

    /// <summary>
    /// Get unread count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { success = true, data = count });
    }

    /// <summary>
    /// Mark single notification as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var result = await _notificationService.MarkAsReadAsync(userId, id);
        return result.Success
            ? Ok(new { success = true })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var result = await _notificationService.MarkAllAsReadAsync(userId);
        return result.Success
            ? Ok(new { success = true })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var result = await _notificationService.DeleteNotificationAsync(userId, id);
        return result.Success
            ? Ok(new { success = true })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }
}
