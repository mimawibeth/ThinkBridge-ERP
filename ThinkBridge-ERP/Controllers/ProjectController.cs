using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = "TeamMemberOnly")] // All 3 roles can access (TeamMember, PM, CompanyAdmin)
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectController> _logger;
    private readonly ApplicationDbContext _context;

    public ProjectController(IProjectService projectService, ILogger<ProjectController> logger, ApplicationDbContext context)
    {
        _projectService = projectService;
        _logger = logger;
        _context = context;
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
    /// Get project statistics (all roles)
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _projectService.GetProjectStatsAsync(companyId, userId, role);
        return result.Success ? Ok(new { success = true, data = result }) : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get project list with filtering/pagination (all roles - scope varies)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var filter = new ProjectFilterRequest
        {
            SearchTerm = search,
            Status = status,
            Category = category,
            Page = page,
            PageSize = pageSize
        };

        var result = await _projectService.GetProjectsAsync(companyId, userId, role, filter);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Projects,
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
    /// Get single project details (all roles - access check inside)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _projectService.GetProjectByIdAsync(companyId, userId, role, id);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Project });
    }

    /// <summary>
    /// Get team members for assignment dropdown (ProjectManager only)
    /// </summary>
    [HttpGet("team-members")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> GetTeamMembers()
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var members = await _projectService.GetTeamMembersForCompanyAsync(companyId);
        return Ok(new { success = true, data = members });
    }

    /// <summary>
    /// Create a new project (ProjectManager only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // Only ProjectManagers can create (not CompanyAdmin even though they pass the policy)
        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.ProjectName))
        {
            return BadRequest(new { success = false, message = "Project name is required." });
        }

        var result = await _projectService.CreateProjectAsync(companyId, userId, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = new { projectId = result.ProjectId, projectCode = result.ProjectCode } });
    }

    /// <summary>
    /// Update a project (ProjectManager full edit, CompanyAdmin can archive/restore only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // CompanyAdmin can only archive/restore projects (status changes to Archived or Planning)
        if (role.Equals("CompanyAdmin", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Status) ||
                (!request.Status.Equals("Archived", StringComparison.OrdinalIgnoreCase) &&
                 !request.Status.Equals("Planning", StringComparison.OrdinalIgnoreCase)))
            {
                return Forbid();
            }
            // Allow CompanyAdmin to archive/restore - pass to service with admin flag
            var result = await _projectService.ArchiveOrRestoreProjectAsync(companyId, userId, id, request.Status);
            if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });
            return Ok(new { success = true });
        }

        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var updateResult = await _projectService.UpdateProjectAsync(companyId, userId, id, request);
        if (!updateResult.Success) return BadRequest(new { success = false, message = updateResult.ErrorMessage });

        return Ok(new { success = true });
    }

    // ---- Project Categories ----

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var categories = await _context.ProjectCategories
            .Where(c => c.CompanyID == companyId)
            .OrderBy(c => c.CategoryName)
            .Select(c => new { c.CategoryID, c.CategoryName })
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    [HttpPost("categories")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateProjectCategoryRequest request)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (string.IsNullOrWhiteSpace(request.CategoryName))
            return BadRequest(new { success = false, message = "Category name is required." });

        var name = request.CategoryName.Trim();
        if (name.Length > 50)
            return BadRequest(new { success = false, message = "Category name must be 50 characters or less." });

        var exists = await _context.ProjectCategories
            .AnyAsync(c => c.CompanyID == companyId && c.CategoryName.ToLower() == name.ToLower());
        if (exists)
            return BadRequest(new { success = false, message = "Category already exists." });

        var category = new ProjectCategory
        {
            CompanyID = companyId,
            CategoryName = name,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = new { category.CategoryID, category.CategoryName } });
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var category = await _context.ProjectCategories
            .FirstOrDefaultAsync(c => c.CategoryID == id && c.CompanyID == companyId);
        if (category == null)
            return NotFound(new { success = false, message = "Category not found." });

        _context.ProjectCategories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

}
