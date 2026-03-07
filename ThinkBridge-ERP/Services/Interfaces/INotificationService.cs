namespace ThinkBridge_ERP.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationListResult> GetNotificationsAsync(int userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task<ServiceResult> MarkAsReadAsync(int userId, int notificationId);
    Task<ServiceResult> MarkAllAsReadAsync(int userId);
    Task<ServiceResult> DeleteNotificationAsync(int userId, int notificationId);
    Task SendNotificationAsync(int userId, string notifType, string message);
    Task SendBulkNotificationAsync(IEnumerable<int> userIds, string notifType, string message);
}

// ─── Response DTOs ──────────────────────────────────────

public class NotificationListResult : ServiceResult
{
    public List<NotificationItem> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class NotificationItem
{
    public int NotificationID { get; set; }
    public string NotifType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
