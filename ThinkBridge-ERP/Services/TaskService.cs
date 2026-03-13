using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;
using Task = ThinkBridge_ERP.Models.Entities.Task;

namespace ThinkBridge_ERP.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaskService> _logger;
    private readonly INotificationService _notificationService;

    public TaskService(ApplicationDbContext context, ILogger<TaskService> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async System.Threading.Tasks.Task<TaskListResult> GetTasksAsync(int companyId, int userId, string userRole, TaskFilterRequest filter)
    {
        try
        {
            IQueryable<Task> query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Creator)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskUpdates)
                .Where(t => t.Project.CompanyID == companyId);

            // Role-based scoping
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserID == userId));
            }
            else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Project.CreatedBy == userId ||
                    t.Project.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            // Project filter
            if (filter.ProjectId.HasValue && filter.ProjectId.Value > 0)
            {
                query = query.Where(t => t.ProjectID == filter.ProjectId.Value);
            }

            // Assigned-to filter
            if (filter.AssignedToUserId.HasValue && filter.AssignedToUserId.Value > 0)
            {
                query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserID == filter.AssignedToUserId.Value));
            }

            // Search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(search) ||
                    (t.Description != null && t.Description.ToLower().Contains(search)));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(filter.Status) && !filter.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Status == filter.Status);
            }
            else
            {
                // Exclude archived tasks by default
                query = query.Where(t => t.Status != "Archived");
            }

            // Priority filter
            if (!string.IsNullOrWhiteSpace(filter.Priority) && !filter.Priority.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Priority == filter.Priority);
            }

            var totalCount = await query.CountAsync();

            var tasks = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(t => new TaskListItem
                {
                    TaskID = t.TaskID,
                    ProjectID = t.ProjectID,
                    ProjectName = t.Project.ProjectName,
                    ProjectCode = t.Project.ProjectCode ?? $"PRJ-{t.ProjectID:D3}",
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority ?? "Medium",
                    StartDate = t.StartDate,
                    DueDate = t.DueDate,
                    EstimatedHours = t.EstimatedHours,
                    ActualHours = t.ActualHours,
                    CreatedByName = t.Creator.Fname + " " + t.Creator.Lname,
                    CreatedAt = t.CreatedAt,
                    UpdateCount = t.TaskUpdates.Count,
                    Assignees = t.TaskAssignments.Select(ta => new TaskAssigneeInfo
                    {
                        UserID = ta.UserID,
                        FullName = ta.User.Fname + " " + ta.User.Lname,
                        Initials = (ta.User.Fname.Substring(0, 1) + ta.User.Lname.Substring(0, 1)).ToUpper(),
                        AvatarUrl = ta.User.AvatarUrl,
                        AvatarColor = ta.User.AvatarColor
                    }).ToList()
                })
                .ToListAsync();

            return new TaskListResult
            {
                Success = true,
                Tasks = tasks,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks for company {CompanyId}", companyId);
            return new TaskListResult { Success = false, ErrorMessage = "An error occurred while loading tasks." };
        }
    }

    public async System.Threading.Tasks.Task<TaskDetailResult> GetTaskByIdAsync(int companyId, int userId, string userRole, int taskId)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Creator)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskUpdates).ThenInclude(tu => tu.User)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null)
                return new TaskDetailResult { Success = false, ErrorMessage = "Task not found." };

            // Access check
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase) &&
                !task.TaskAssignments.Any(ta => ta.UserID == userId))
                return new TaskDetailResult { Success = false, ErrorMessage = "Access denied." };

            if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase) &&
                task.Project.CreatedBy != userId &&
                !task.Project.ProjectMembers.Any(pm => pm.UserID == userId))
                return new TaskDetailResult { Success = false, ErrorMessage = "Access denied." };

            var item = MapToListItem(task);
            var updates = task.TaskUpdates
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new TaskUpdateInfo
                {
                    UpdateID = u.UpdateID,
                    UserName = u.User.Fname + " " + u.User.Lname,
                    Initials = (u.User.Fname.Substring(0, 1) + u.User.Lname.Substring(0, 1)).ToUpper(),
                    AvatarColor = u.User.AvatarColor,
                    UpdateText = u.UpdateText,
                    NewStatus = u.NewStatus,
                    CreatedAt = u.CreatedAt
                }).ToList();

            return new TaskDetailResult { Success = true, Task = item, Updates = updates };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId}", taskId);
            return new TaskDetailResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<CreateTaskResult> CreateTaskAsync(int companyId, int userId, CreateTaskRequest request)
    {
        try
        {
            // Verify project belongs to company and user has access
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectID == request.ProjectID && p.CompanyID == companyId);

            if (project == null)
                return new CreateTaskResult { Success = false, ErrorMessage = "Project not found." };

            // Validate task dates
            if (request.StartDate.HasValue && request.DueDate.HasValue && request.DueDate.Value < request.StartDate.Value)
                return new CreateTaskResult { Success = false, ErrorMessage = "Invalid date: Due date cannot be earlier than the start date." };
            if (request.DueDate.HasValue && project.DueDate.HasValue && request.DueDate.Value > project.DueDate.Value)
                return new CreateTaskResult { Success = false, ErrorMessage = "Invalid date: Due date must be within the project timeline." };
            if (request.StartDate.HasValue && project.StartDate.HasValue && request.StartDate.Value < project.StartDate.Value)
                return new CreateTaskResult { Success = false, ErrorMessage = "Invalid date: Start date cannot be earlier than the project start date." };

            var task = new Task
            {
                ProjectID = request.ProjectID,
                Title = request.Title,
                Description = request.Description,
                Status = "To Do",
                Priority = request.Priority ?? "Medium",
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                EstimatedHours = request.EstimatedHours,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Add assignees
            if (request.AssigneeIds != null && request.AssigneeIds.Any())
            {
                foreach (var assigneeId in request.AssigneeIds)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == assigneeId && u.CompanyID == companyId);
                    if (user != null)
                    {
                        _context.TaskAssignments.Add(new TaskAssignment
                        {
                            TaskID = task.TaskID,
                            UserID = assigneeId,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }
                await _context.SaveChangesAsync();

                // Notify assignees
                var creator = await _context.Users.FindAsync(userId);
                var creatorName = creator?.FullName ?? "Someone";
                await _notificationService.SendBulkNotificationAsync(
                    request.AssigneeIds,
                    "task",
                    $"{creatorName} assigned you to task \"{request.Title}\""
                );
            }

            // Recalculate project progress
            await RecalculateProjectProgressAsync(request.ProjectID);

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Created task '{request.Title}'",
                EntityName = "Task",
                EntityID = task.TaskID,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task '{Title}' created for project {ProjectId} by user {UserId}", request.Title, request.ProjectID, userId);
            return new CreateTaskResult { Success = true, TaskId = task.TaskID };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task for project {ProjectId}", request.ProjectID);
            return new CreateTaskResult { Success = false, ErrorMessage = "An error occurred while creating the task." };
        }
    }

    public async System.Threading.Tasks.Task<ServiceResult> UpdateTaskAsync(int companyId, int userId, string userRole, int taskId, UpdateTaskRequest request)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null)
                return new ServiceResult { Success = false, ErrorMessage = "Task not found." };

            // PM can only edit tasks in projects they created; CompanyAdmin can edit any task in their company
            if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase) && task.Project.CreatedBy != userId)
                return new ServiceResult { Success = false, ErrorMessage = "Only the project creator can edit tasks." };

            // Validate task dates
            var effectiveStart = request.StartDate ?? task.StartDate;
            var effectiveDue = request.DueDate ?? task.DueDate;
            if (effectiveStart.HasValue && effectiveDue.HasValue && effectiveDue.Value < effectiveStart.Value)
                return new ServiceResult { Success = false, ErrorMessage = "Invalid date: Due date cannot be earlier than the start date." };
            if (effectiveDue.HasValue && task.Project.DueDate.HasValue && effectiveDue.Value > task.Project.DueDate.Value)
                return new ServiceResult { Success = false, ErrorMessage = "Invalid date: Due date must be within the project timeline." };
            if (effectiveStart.HasValue && task.Project.StartDate.HasValue && effectiveStart.Value < task.Project.StartDate.Value)
                return new ServiceResult { Success = false, ErrorMessage = "Invalid date: Start date cannot be earlier than the project start date." };

            string? oldStatus = null;
            if (!string.IsNullOrWhiteSpace(request.Title)) task.Title = request.Title;
            if (request.Description != null) task.Description = request.Description;
            if (!string.IsNullOrWhiteSpace(request.Priority)) task.Priority = request.Priority;
            if (request.StartDate.HasValue) task.StartDate = request.StartDate.Value;
            if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;
            if (request.EstimatedHours.HasValue) task.EstimatedHours = request.EstimatedHours.Value;
            if (request.ActualHours.HasValue) task.ActualHours = request.ActualHours.Value;
            task.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != task.Status)
            {
                oldStatus = task.Status;
                task.Status = request.Status;

                // Log status change
                _context.TaskUpdates.Add(new TaskUpdate
                {
                    TaskID = taskId,
                    UserID = userId,
                    UpdateText = $"Status changed from '{oldStatus}' to '{request.Status}'",
                    NewStatus = request.Status,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Update assignees if provided (PM only)
            if (request.AssigneeIds != null && userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                var existing = task.TaskAssignments.ToList();
                _context.TaskAssignments.RemoveRange(existing);

                foreach (var assigneeId in request.AssigneeIds)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == assigneeId && u.CompanyID == companyId);
                    if (user != null)
                    {
                        _context.TaskAssignments.Add(new TaskAssignment
                        {
                            TaskID = task.TaskID,
                            UserID = assigneeId,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Updated task '{task.Title}'",
                EntityName = "Task",
                EntityID = taskId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Recalculate project progress if status changed
            if (oldStatus != null)
            {
                await RecalculateProjectProgressAsync(task.ProjectID);
            }

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", taskId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while updating the task." };
        }
    }

    public async System.Threading.Tasks.Task<ServiceResult> UpdateTaskStatusAsync(int companyId, int userId, string userRole, int taskId, string newStatus)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null)
                return new ServiceResult { Success = false, ErrorMessage = "Task not found." };

            // TeamMember: can only update status if assigned
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase) &&
                !task.TaskAssignments.Any(ta => ta.UserID == userId))
                return new ServiceResult { Success = false, ErrorMessage = "You can only update tasks assigned to you." };

            var oldStatus = task.Status;
            task.Status = newStatus;
            task.UpdatedAt = DateTime.UtcNow;

            // Log status change
            _context.TaskUpdates.Add(new TaskUpdate
            {
                TaskID = taskId,
                UserID = userId,
                UpdateText = $"Status changed from '{oldStatus}' to '{newStatus}'",
                NewStatus = newStatus,
                CreatedAt = DateTime.UtcNow
            });

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Changed task '{task.Title}' status to '{newStatus}'",
                EntityName = "Task",
                EntityID = taskId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify task assignees about status change
            var assigneeIds = task.TaskAssignments
                .Where(ta => ta.UserID != userId)
                .Select(ta => ta.UserID)
                .ToList();
            if (assigneeIds.Any())
            {
                var updater = await _context.Users.FindAsync(userId);
                var updaterName = updater?.FullName ?? "Someone";
                await _notificationService.SendBulkNotificationAsync(
                    assigneeIds,
                    "task",
                    $"{updaterName} changed \"{task.Title}\" status to {newStatus}"
                );
            }

            // Recalculate project progress
            await RecalculateProjectProgressAsync(task.ProjectID);

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task status {TaskId}", taskId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<ServiceResult> DeleteTaskAsync(int companyId, int userId, int taskId)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .Include(t => t.TaskUpdates)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null)
                return new ServiceResult { Success = false, ErrorMessage = "Task not found." };

            var taskTitle = task.Title;
            var projectId = task.ProjectID;

            _context.TaskUpdates.RemoveRange(task.TaskUpdates);
            _context.TaskAssignments.RemoveRange(task.TaskAssignments);
            _context.Tasks.Remove(task);

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Deleted task '{taskTitle}'",
                EntityName = "Task",
                EntityID = taskId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Recalculate project progress
            await RecalculateProjectProgressAsync(projectId);

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", taskId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while deleting the task." };
        }
    }

    public async System.Threading.Tasks.Task<TaskStatsResult> GetTaskStatsAsync(int companyId, int userId, string userRole, int? projectId = null)
    {
        try
        {
            IQueryable<Task> query = _context.Tasks
                .Include(t => t.Project)
                .Where(t => t.Project.CompanyID == companyId);

            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserID == userId));
            }
            else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Project.CreatedBy == userId ||
                    t.Project.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(t => t.ProjectID == projectId.Value);
            }

            // Exclude archived tasks from stats
            query = query.Where(t => t.Status != "Archived");

            var tasks = await query.ToListAsync();

            return new TaskStatsResult
            {
                Success = true,
                TotalTasks = tasks.Count,
                TodoTasks = tasks.Count(t => t.Status == "To Do"),
                InProgressTasks = tasks.Count(t => t.Status == "In Progress"),
                CompletedTasks = tasks.Count(t => t.Status == "Completed"),
                OverdueTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != "Completed")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task stats");
            return new TaskStatsResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<List<TaskListItem>> GetTasksByProjectAsync(int companyId, int userId, string userRole, int projectId)
    {
        try
        {
            IQueryable<Task> query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Creator)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskUpdates)
                .Where(t => t.ProjectID == projectId && t.Project.CompanyID == companyId);

            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserID == userId));
            }

            return await query
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new TaskListItem
                {
                    TaskID = t.TaskID,
                    ProjectID = t.ProjectID,
                    ProjectName = t.Project.ProjectName,
                    ProjectCode = t.Project.ProjectCode ?? $"PRJ-{t.ProjectID:D3}",
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority ?? "Medium",
                    StartDate = t.StartDate,
                    DueDate = t.DueDate,
                    EstimatedHours = t.EstimatedHours,
                    ActualHours = t.ActualHours,
                    CreatedByName = t.Creator.Fname + " " + t.Creator.Lname,
                    CreatedAt = t.CreatedAt,
                    UpdateCount = t.TaskUpdates.Count,
                    Assignees = t.TaskAssignments.Select(ta => new TaskAssigneeInfo
                    {
                        UserID = ta.UserID,
                        FullName = ta.User.Fname + " " + ta.User.Lname,
                        Initials = (ta.User.Fname.Substring(0, 1) + ta.User.Lname.Substring(0, 1)).ToUpper(),
                        AvatarUrl = ta.User.AvatarUrl,
                        AvatarColor = ta.User.AvatarColor
                    }).ToList()
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks for project {ProjectId}", projectId);
            return new List<TaskListItem>();
        }
    }

    /// <summary>
    /// Recalculates project progress based on completed tasks / total tasks * 100
    /// </summary>
    private async System.Threading.Tasks.Task RecalculateProjectProgressAsync(int projectId)
    {
        try
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return;

            var totalTasks = await _context.Tasks.CountAsync(t => t.ProjectID == projectId);
            var completedTasks = await _context.Tasks.CountAsync(t => t.ProjectID == projectId && t.Status == "Completed");

            project.Progress = totalTasks > 0
                ? Math.Round((decimal)completedTasks / totalTasks * 100, 2)
                : 0;

            // Auto-update project status based on progress
            if (project.Progress == 100 && project.Status != "Completed" && project.Status != "Archived")
            {
                project.Status = "Completed";
            }
            else if (project.Progress > 0 && project.Progress < 100 && project.Status == "Planning")
            {
                project.Status = "In Progress";
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} progress recalculated: {Progress}%", projectId, project.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating progress for project {ProjectId}", projectId);
        }
    }

    private TaskListItem MapToListItem(Task t)
    {
        return new TaskListItem
        {
            TaskID = t.TaskID,
            ProjectID = t.ProjectID,
            ProjectName = t.Project.ProjectName,
            ProjectCode = t.Project.ProjectCode ?? $"PRJ-{t.ProjectID:D3}",
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority ?? "Medium",
            StartDate = t.StartDate,
            DueDate = t.DueDate,
            EstimatedHours = t.EstimatedHours,
            ActualHours = t.ActualHours,
            CreatedByName = t.Creator.Fname + " " + t.Creator.Lname,
            CreatedAt = t.CreatedAt,
            UpdateCount = t.TaskUpdates.Count,
            Assignees = t.TaskAssignments.Select(ta => new TaskAssigneeInfo
            {
                UserID = ta.UserID,
                FullName = ta.User.Fname + " " + ta.User.Lname,
                Initials = (ta.User.Fname.Substring(0, 1) + ta.User.Lname.Substring(0, 1)).ToUpper(),
                AvatarUrl = ta.User.AvatarUrl,
                AvatarColor = ta.User.AvatarColor
            }).ToList()
        };
    }

    // ========== Task Comments ==========

    public async System.Threading.Tasks.Task<TaskCommentListResult> GetTaskCommentsAsync(int companyId, int userId, string userRole, int taskId)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null)
                return new TaskCommentListResult { Success = false, ErrorMessage = "Task not found." };

            // Access check
            if (!await CanAccessTask(companyId, userId, userRole, task))
                return new TaskCommentListResult { Success = false, ErrorMessage = "Access denied." };

            var isAdmin = userRole.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

            var comments = await _context.TaskComments
                .Where(c => c.TaskID == taskId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new TaskCommentItem
                {
                    TaskCommentID = c.TaskCommentID,
                    TaskID = c.TaskID,
                    UserID = c.UserID,
                    UserName = c.User.Fname + " " + c.User.Lname,
                    Initials = (c.User.Fname.Substring(0, 1) + c.User.Lname.Substring(0, 1)).ToUpper(),
                    AvatarColor = c.User.AvatarColor,
                    Content = c.Content,
                    IsEdited = c.IsEdited,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CanEdit = isAdmin || c.UserID == userId,
                    CanDelete = isAdmin || c.UserID == userId
                })
                .ToListAsync();

            return new TaskCommentListResult
            {
                Success = true,
                Comments = comments,
                TotalCount = comments.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for task {TaskId}", taskId);
            return new TaskCommentListResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<CreateTaskCommentResult> AddTaskCommentAsync(int companyId, int userId, string userRole, int taskId, string content, List<int>? mentionedUserIds = null)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null)
                return new CreateTaskCommentResult { Success = false, ErrorMessage = "Task not found." };

            if (!await CanAccessTask(companyId, userId, userRole, task))
                return new CreateTaskCommentResult { Success = false, ErrorMessage = "Access denied." };

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new CreateTaskCommentResult { Success = false, ErrorMessage = "User not found." };

            var comment = new TaskComment
            {
                TaskID = taskId,
                UserID = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskComments.Add(comment);

            // Log to audit
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Added comment on task \"{task.Title}\"",
                EntityName = "TaskComment",
                EntityID = taskId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Send notifications to @mentioned users
            if (mentionedUserIds != null && mentionedUserIds.Any())
            {
                var validMentions = mentionedUserIds.Where(id => id != userId).Distinct().ToList();
                if (validMentions.Any())
                {
                    var commenterName = user.FullName;
                    await _notificationService.SendBulkNotificationAsync(
                        validMentions,
                        "mention",
                        $"{commenterName} mentioned you in a comment on task \"{task.Title}\""
                    );
                }
            }

            return new CreateTaskCommentResult
            {
                Success = true,
                Comment = new TaskCommentItem
                {
                    TaskCommentID = comment.TaskCommentID,
                    TaskID = comment.TaskID,
                    UserID = comment.UserID,
                    UserName = user.FullName,
                    Initials = (user.Fname.Substring(0, 1) + user.Lname.Substring(0, 1)).ToUpper(),
                    AvatarColor = user.AvatarColor,
                    Content = comment.Content,
                    IsEdited = false,
                    CreatedAt = comment.CreatedAt,
                    CanEdit = true,
                    CanDelete = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to task {TaskId}", taskId);
            return new CreateTaskCommentResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<ServiceResult> UpdateTaskCommentAsync(int companyId, int userId, string userRole, int commentId, string content)
    {
        try
        {
            var comment = await _context.TaskComments
                .Include(c => c.Task).ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(c => c.TaskCommentID == commentId);

            if (comment == null)
                return new ServiceResult { Success = false, ErrorMessage = "Comment not found." };

            if (comment.Task.Project.CompanyID != companyId)
                return new ServiceResult { Success = false, ErrorMessage = "Access denied." };

            var isAdmin = userRole.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && comment.UserID != userId)
                return new ServiceResult { Success = false, ErrorMessage = "You can only edit your own comments." };

            comment.Content = content.Trim();
            comment.IsEdited = true;
            comment.UpdatedAt = DateTime.UtcNow;

            // Log to audit
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Edited comment on task \"{comment.Task.Title}\"",
                EntityName = "TaskComment",
                EntityID = comment.TaskCommentID,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<ServiceResult> DeleteTaskCommentAsync(int companyId, int userId, string userRole, int commentId)
    {
        try
        {
            var comment = await _context.TaskComments
                .Include(c => c.Task).ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(c => c.TaskCommentID == commentId);

            if (comment == null)
                return new ServiceResult { Success = false, ErrorMessage = "Comment not found." };

            if (comment.Task.Project.CompanyID != companyId)
                return new ServiceResult { Success = false, ErrorMessage = "Access denied." };

            var isAdmin = userRole.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && comment.UserID != userId)
                return new ServiceResult { Success = false, ErrorMessage = "You can only delete your own comments." };

            // Log to audit
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Deleted comment on task \"{comment.Task.Title}\"",
                EntityName = "TaskComment",
                EntityID = comment.TaskCommentID,
                CreatedAt = DateTime.UtcNow
            });

            _context.TaskComments.Remove(comment);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<List<TaskMentionableUser>> GetMentionableUsersAsync(int companyId, int userId, string userRole, int taskId)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskID == taskId && t.Project.CompanyID == companyId);

            if (task == null) return new List<TaskMentionableUser>();

            IQueryable<User> usersQuery;

            var isAdmin = userRole.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (isAdmin)
            {
                // Admin can mention any user in the company
                usersQuery = _context.Users
                    .Where(u => u.CompanyID == companyId && u.Status == "Active");
            }
            else
            {
                // PM and TM can only mention project members
                usersQuery = _context.ProjectMembers
                    .Where(pm => pm.ProjectID == task.ProjectID)
                    .Select(pm => pm.User)
                    .Where(u => u.Status == "Active");
            }

            return await usersQuery
                .Where(u => u.UserID != userId)
                .OrderBy(u => u.Fname).ThenBy(u => u.Lname)
                .Select(u => new TaskMentionableUser
                {
                    UserID = u.UserID,
                    FullName = u.Fname + " " + u.Lname,
                    Initials = (u.Fname.Substring(0, 1) + u.Lname.Substring(0, 1)).ToUpper(),
                    AvatarColor = u.AvatarColor
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mentionable users for task {TaskId}", taskId);
            return new List<TaskMentionableUser>();
        }
    }

    private async System.Threading.Tasks.Task<bool> CanAccessTask(int companyId, int userId, string userRole, Task task)
    {
        if (userRole.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase) ||
            userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return task.Project.CreatedBy == userId ||
                   await _context.ProjectMembers.AnyAsync(pm => pm.ProjectID == task.ProjectID && pm.UserID == userId);
        }

        // TeamMember - must be assigned or a project member
        return await _context.TaskAssignments.AnyAsync(ta => ta.TaskID == task.TaskID && ta.UserID == userId) ||
               await _context.ProjectMembers.AnyAsync(pm => pm.ProjectID == task.ProjectID && pm.UserID == userId);
    }
}
