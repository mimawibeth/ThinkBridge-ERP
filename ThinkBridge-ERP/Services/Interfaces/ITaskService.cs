namespace ThinkBridge_ERP.Services.Interfaces;

public interface ITaskService
{
    Task<TaskListResult> GetTasksAsync(int companyId, int userId, string userRole, TaskFilterRequest filter);
    Task<TaskDetailResult> GetTaskByIdAsync(int companyId, int userId, string userRole, int taskId);
    Task<CreateTaskResult> CreateTaskAsync(int companyId, int userId, CreateTaskRequest request);
    Task<ServiceResult> UpdateTaskAsync(int companyId, int userId, string userRole, int taskId, UpdateTaskRequest request);
    Task<ServiceResult> DeleteTaskAsync(int companyId, int userId, int taskId);
    Task<ServiceResult> UpdateTaskStatusAsync(int companyId, int userId, string userRole, int taskId, string newStatus);
    Task<TaskStatsResult> GetTaskStatsAsync(int companyId, int userId, string userRole, int? projectId = null);
    Task<List<TaskListItem>> GetTasksByProjectAsync(int companyId, int userId, string userRole, int projectId);

    // Task Comments
    Task<TaskCommentListResult> GetTaskCommentsAsync(int companyId, int userId, string userRole, int taskId);
    Task<CreateTaskCommentResult> AddTaskCommentAsync(int companyId, int userId, string userRole, int taskId, string content, List<int>? mentionedUserIds = null);
    Task<ServiceResult> UpdateTaskCommentAsync(int companyId, int userId, string userRole, int commentId, string content);
    Task<ServiceResult> DeleteTaskCommentAsync(int companyId, int userId, string userRole, int commentId);
    Task<List<TaskMentionableUser>> GetMentionableUsersAsync(int companyId, int userId, string userRole, int taskId);
}

// Request DTOs
public class TaskFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? ProjectId { get; set; }
    public int? AssignedToUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateTaskRequest
{
    public int ProjectID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public List<int>? AssigneeIds { get; set; }
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public List<int>? AssigneeIds { get; set; }
}

// Response DTOs
public class TaskListResult : ServiceResult
{
    public List<TaskListItem> Tasks { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class TaskListItem
{
    public int TaskID { get; set; }
    public int ProjectID { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TaskAssigneeInfo> Assignees { get; set; } = new();
    public int UpdateCount { get; set; }
}

public class TaskAssigneeInfo
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string AvatarColor { get; set; } = "#0B4F6C";
}

public class TaskDetailResult : ServiceResult
{
    public TaskListItem? Task { get; set; }
    public List<TaskUpdateInfo> Updates { get; set; } = new();
}

public class TaskUpdateInfo
{
    public int UpdateID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#0B4F6C";
    public string UpdateText { get; set; } = string.Empty;
    public string? NewStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaskResult : ServiceResult
{
    public int? TaskId { get; set; }
}

public class TaskStatsResult : ServiceResult
{
    public int TotalTasks { get; set; }
    public int TodoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
}

// Task Comment DTOs
public class TaskCommentListResult : ServiceResult
{
    public List<TaskCommentItem> Comments { get; set; } = new();
    public int TotalCount { get; set; }
}

public class TaskCommentItem
{
    public int TaskCommentID { get; set; }
    public int TaskID { get; set; }
    public int UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#0B4F6C";
    public string Content { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class CreateTaskCommentResult : ServiceResult
{
    public TaskCommentItem? Comment { get; set; }
}

public class TaskMentionableUser
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#0B4F6C";
}

public class AddTaskCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public List<int>? MentionedUserIds { get; set; }
}

public class UpdateTaskCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
