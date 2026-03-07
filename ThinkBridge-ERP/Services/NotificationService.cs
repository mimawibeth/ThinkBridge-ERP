using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace ThinkBridge_ERP.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationListResult> GetNotificationsAsync(int userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();
            var unreadCount = await query.CountAsync(n => !n.IsRead);

            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationItem
                {
                    NotificationID = n.NotificationID,
                    NotifType = n.NotifType ?? "info",
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new NotificationListResult
            {
                Success = true,
                Notifications = notifications,
                UnreadCount = unreadCount,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return new NotificationListResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserID == userId && !n.IsRead);
    }

    public async Task<ServiceResult> MarkAsReadAsync(int userId, int notificationId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

            if (notification == null)
                return new ServiceResult { Success = false, ErrorMessage = "Notification not found." };

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", notificationId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<ServiceResult> DeleteNotificationAsync(int userId, int notificationId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

            if (notification == null)
                return new ServiceResult { Success = false, ErrorMessage = "Notification not found." };

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {Id}", notificationId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task SendNotificationAsync(int userId, string notifType, string message)
    {
        try
        {
            _context.Notifications.Add(new Notification
            {
                UserID = userId,
                NotifType = notifType,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }

    public async Task SendBulkNotificationAsync(IEnumerable<int> userIds, string notifType, string message)
    {
        try
        {
            foreach (var userId in userIds.Distinct())
            {
                _context.Notifications.Add(new Notification
                {
                    UserID = userId,
                    NotifType = notifType,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
        }
    }
}
