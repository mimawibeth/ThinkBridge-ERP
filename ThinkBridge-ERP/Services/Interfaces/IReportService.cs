using ThinkBridge_ERP.Models.Entities;

namespace ThinkBridge_ERP.Services.Interfaces;

public interface IReportService
{
    Task<ReportDashboardResult> GetReportDashboardAsync(int companyId, int userId, string userRole, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<ProjectProgressResult> GetProjectProgressAsync(int companyId, int userId, string userRole, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<TaskDistributionResult> GetTaskDistributionAsync(int companyId, int userId, string userRole, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<TeamPerformanceResult> GetTeamPerformanceAsync(int companyId, int userId, string userRole, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<SavedReportListResult> GetSavedReportsAsync(int companyId, int userId);
    Task<CreateReportResult> SaveReportAsync(int companyId, int userId, SaveReportRequest request);
    Task<ServiceResult> DeleteReportAsync(int companyId, int userId, int reportId);
    Task<CompanyReportResult> GetCompanyReportAsync(int companyId);
}

// ─── Request DTOs ──────────────────────────────────────

public class SaveReportRequest
{
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string? Parameters { get; set; }
}

// ─── Response DTOs ──────────────────────────────────────

public class ReportDashboardResult : ServiceResult
{
    public int TasksCompleted { get; set; }
    public int TotalTasks { get; set; }
    public decimal OnTimeDeliveryPercent { get; set; }
    public decimal TeamUtilizationPercent { get; set; }
    public int BlockersResolved { get; set; }
    public int TotalBlockers { get; set; }
}

public class ProjectProgressResult : ServiceResult
{
    public List<ProjectProgressItem> Projects { get; set; } = new();
}

public class ProjectProgressItem
{
    public int ProjectID { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal Progress { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TaskDistributionResult : ServiceResult
{
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int InReview { get; set; }
    public int NotStarted { get; set; }
    public int Total { get; set; }
}

public class TeamPerformanceResult : ServiceResult
{
    public List<TeamMemberPerformance> Members { get; set; } = new();
}

public class TeamMemberPerformance
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#0B4F6C";
    public int TasksAssigned { get; set; }
    public int TasksCompleted { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal OnTimePercent { get; set; }
}

public class SavedReportListResult : ServiceResult
{
    public List<SavedReportItem> Reports { get; set; } = new();
}

public class SavedReportItem
{
    public int ReportID { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReportResult : ServiceResult
{
    public int? ReportId { get; set; }
}

// ─── Company Admin Report DTOs ─────────────────────────

public class CompanyReportResult : ServiceResult
{
    // User Overview
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public List<UserRoleBreakdown> UsersByRole { get; set; } = new();

    // Subscription Info
    public string PlanName { get; set; } = string.Empty;
    public string SubscriptionStatus { get; set; } = string.Empty;
    public DateTime? SubscriptionStart { get; set; }
    public DateTime? SubscriptionEnd { get; set; }
    public decimal PlanPrice { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public int? MaxUsers { get; set; }
    public int? MaxProjects { get; set; }

    // Activity
    public int RecentLoginsLast30Days { get; set; }
    public List<UserActivityItem> RecentUsers { get; set; } = new();

    // Metadata
    public DateTime GeneratedAt { get; set; }
}

public class UserRoleBreakdown
{
    public string RoleName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserActivityItem
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
}
