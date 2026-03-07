using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Policy = "TeamMemberOnly")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskController> _logger;

    public TaskController(ITaskService taskService, ILogger<TaskController> logger)
    {
        _taskService = taskService;
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
    /// Get task statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? projectId = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _taskService.GetTaskStatsAsync(companyId, userId, role, projectId);
        return result.Success
            ? Ok(new { success = true, data = result })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get task list with filtering/pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? projectId = null,
        [FromQuery] int? assignedTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var filter = new TaskFilterRequest
        {
            SearchTerm = search,
            Status = status,
            Priority = priority,
            ProjectId = projectId,
            AssignedToUserId = assignedTo,
            Page = page,
            PageSize = pageSize
        };

        var result = await _taskService.GetTasksAsync(companyId, userId, role, filter);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Tasks,
            pagination = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages
            }
        });
    }

    /// <summary>
    /// Get tasks for a specific project
    /// </summary>
    [HttpGet("by-project/{projectId}")]
    public async Task<IActionResult> GetTasksByProject(int projectId)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var tasks = await _taskService.GetTasksByProjectAsync(companyId, userId, role, projectId);
        return Ok(new { success = true, data = tasks });
    }

    /// <summary>
    /// Get single task details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _taskService.GetTaskByIdAsync(companyId, userId, role, id);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Task, updates = result.Updates });
    }

    /// <summary>
    /// Create a new task (ProjectManager only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // Only ProjectManagers can create tasks (not CompanyAdmin)
        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { success = false, message = "Task title is required." });

        if (request.ProjectID <= 0)
            return BadRequest(new { success = false, message = "Project is required." });

        var result = await _taskService.CreateTaskAsync(companyId, userId, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = new { taskId = result.TaskId } });
    }

    /// <summary>
    /// Update a task (PM full edit, TM status/hours only, CompanyAdmin cannot edit)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // CompanyAdmin can only view tasks, not edit
        if (role.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var result = await _taskService.UpdateTaskAsync(companyId, userId, role, id, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Quick status update (PM and TeamMember can use this for assigned tasks)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TaskStatusUpdateBody body)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // CompanyAdmin can only view tasks, not update status
        if (role.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(body.Status))
            return BadRequest(new { success = false, message = "Status is required." });

        var result = await _taskService.UpdateTaskStatusAsync(companyId, userId, role, id, body.Status);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    // ========== Task Comments ==========

    /// <summary>
    /// Get comments for a task
    /// </summary>
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetTaskComments(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _taskService.GetTaskCommentsAsync(companyId, userId, role, id);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Comments, totalCount = result.TotalCount });
    }

    /// <summary>
    /// Add a comment to a task
    /// </summary>
    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddTaskComment(int id, [FromBody] AddTaskCommentRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { success = false, message = "Comment content is required." });

        var result = await _taskService.AddTaskCommentAsync(companyId, userId, role, id, request.Content, request.MentionedUserIds);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Comment });
    }

    /// <summary>
    /// Update a comment
    /// </summary>
    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> UpdateTaskComment(int commentId, [FromBody] UpdateTaskCommentRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { success = false, message = "Comment content is required." });

        var result = await _taskService.UpdateTaskCommentAsync(companyId, userId, role, commentId, request.Content);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteTaskComment(int commentId)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _taskService.DeleteTaskCommentAsync(companyId, userId, role, commentId);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Get mentionable users for a task
    /// </summary>
    [HttpGet("{id}/mentionable-users")]
    public async Task<IActionResult> GetMentionableUsers(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var users = await _taskService.GetMentionableUsersAsync(companyId, userId, role, id);
        return Ok(new { success = true, data = users });
    }
}

public class TaskStatusUpdateBody
{
    public string Status { get; set; } = string.Empty;
}
