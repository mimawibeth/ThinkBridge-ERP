namespace ThinkBridge_ERP.Services.Interfaces;

public interface IUserManagementService
{
    Task<UserListResult> GetUsersAsync(int companyId, UserFilterRequest filter);
    Task<UserDetailResult> GetUserByIdAsync(int companyId, int userId);
    Task<CreateUserResult> CreateUserAsync(int companyId, CreateUserRequest request);
    Task<UpdateUserResult> UpdateUserAsync(int companyId, int userId, UpdateUserRequest request);
    Task<ServiceResult> UpdateUserStatusAsync(int companyId, int userId, string status);
    Task<ServiceResult> ResetUserPasswordAsync(int companyId, int userId);
    Task<CompanyUserStatsResult> GetCompanyUserStatsAsync(int companyId);
}

// Request DTOs
public class UserFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "TeamMember"; // ProjectManager or TeamMember
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
}

// Response DTOs
public class UserListResult : ServiceResult
{
    public List<UserListItem> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class UserListItem
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string AvatarColor { get; set; } = "#0B4F6C";
    public string Status { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int AssignedProjectsCount { get; set; }
    public int AssignedTasksCount { get; set; }
}

public class UserDetailResult : ServiceResult
{
    public UserListItem? User { get; set; }
}

public class CreateUserResult : ServiceResult
{
    public int? UserId { get; set; }
    public string? TemporaryPassword { get; set; }
}

public class UpdateUserResult : ServiceResult
{
    public UserListItem? User { get; set; }
}

public class CompanyUserStatsResult : ServiceResult
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int ProjectManagers { get; set; }
    public int TeamMembers { get; set; }
    public int InactiveUsers { get; set; }
    public List<UserListItem> RecentUsers { get; set; } = new();
}

public class ResetPasswordResult : ServiceResult
{
    public string? TemporaryPassword { get; set; }
}
