namespace ThinkBridge_ERP.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectListResult> GetProjectsAsync(int companyId, int userId, string userRole, ProjectFilterRequest filter);
    Task<ProjectDetailResult> GetProjectByIdAsync(int companyId, int userId, string userRole, int projectId);
    Task<CreateProjectResult> CreateProjectAsync(int companyId, int userId, CreateProjectRequest request);
    Task<ServiceResult> UpdateProjectAsync(int companyId, int userId, int projectId, UpdateProjectRequest request);
    Task<ServiceResult> ArchiveOrRestoreProjectAsync(int companyId, int projectId, string status);
    Task<ServiceResult> DeleteProjectAsync(int companyId, int userId, int projectId);
    Task<ProjectStatsResult> GetProjectStatsAsync(int companyId, int userId, string userRole);
    Task<List<TeamMemberOption>> GetTeamMembersForCompanyAsync(int companyId);
}

// Request DTOs
public class ProjectFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class CreateProjectRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "Planning";
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public List<int>? TeamMemberIds { get; set; }
}

public class UpdateProjectRequest
{
    public string? ProjectName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public decimal? Progress { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public List<int>? TeamMemberIds { get; set; }
}

// Response DTOs
public class ProjectListResult : ServiceResult
{
    public List<ProjectListItem> Projects { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ProjectListItem
{
    public int ProjectID { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Progress { get; set; }
    public string? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public List<ProjectMemberInfo> Members { get; set; } = new();
}

public class ProjectMemberInfo
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? MemberRole { get; set; }
    public string AvatarColor { get; set; } = "#0B4F6C";
}

public class ProjectDetailResult : ServiceResult
{
    public ProjectListItem? Project { get; set; }
}

public class CreateProjectResult : ServiceResult
{
    public int? ProjectId { get; set; }
    public string? ProjectCode { get; set; }
}

public class ProjectStatsResult : ServiceResult
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int PlanningProjects { get; set; }
    public int DelayedProjects { get; set; }
}

public class TeamMemberOption
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#0B4F6C";
}
