using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;
using Task = ThinkBridge_ERP.Models.Entities.Task;

namespace ThinkBridge_ERP.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ──────────────────────────────────────────────
    // Dashboard KPI cards
    // ──────────────────────────────────────────────
    public async System.Threading.Tasks.Task<ReportDashboardResult> GetReportDashboardAsync(
        int companyId, int userId, string userRole, string period = "month")
    {
        try
        {
            // Determine date range
            var now = DateTime.UtcNow;
            DateTime from = period switch
            {
                "week" => now.AddDays(-7),
                "quarter" => now.AddMonths(-3),
                "year" => now.AddYears(-1),
                _ => now.AddMonths(-1) // month
            };

            // All tasks for company in range, scoped by role
            IQueryable<Task> taskQuery = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .Where(t => t.Project.CompanyID == companyId && t.Status != "Archived");

            taskQuery = ApplyRoleScope(taskQuery, userId, userRole);

            var allTasks = await taskQuery.ToListAsync();
            var periodTasks = allTasks.Where(t => t.CreatedAt >= from).ToList();

            // Tasks completed
            var completedTasks = allTasks.Count(t => t.Status == "Completed");
            var totalTasks = allTasks.Count;

            // On-time delivery: completed tasks that were completed by their DueDate (or have no DueDate)
            var completedWithDue = allTasks.Where(t => t.Status == "Completed" && t.DueDate.HasValue).ToList();
            var onTimeCount = completedWithDue.Count(t => t.DueDate!.Value >= t.CreatedAt.Date);
            decimal onTimePercent = completedWithDue.Count > 0
                ? Math.Round((decimal)onTimeCount / completedWithDue.Count * 100, 0)
                : 100;

            // Team utilization: users with at least one active task / total team members
            var teamUserIds = await GetTeamUserIdsAsync(companyId, userId, userRole);
            var usersWithActiveTasks = allTasks
                .Where(t => t.Status == "In Progress" || t.Status == "In Review")
                .SelectMany(t => t.TaskAssignments.Select(ta => ta.UserID))
                .Distinct()
                .Count();
            decimal utilization = teamUserIds.Count > 0
                ? Math.Round((decimal)usersWithActiveTasks / teamUserIds.Count * 100, 0)
                : 0;

            // Blockers: tasks marked as High priority that are overdue
            var blockerTasks = allTasks.Where(t => t.Priority == "High" && t.DueDate.HasValue && t.DueDate.Value < now).ToList();
            var blockersResolved = blockerTasks.Count(t => t.Status == "Completed");

            return new ReportDashboardResult
            {
                Success = true,
                TasksCompleted = completedTasks,
                TotalTasks = totalTasks,
                OnTimeDeliveryPercent = onTimePercent,
                TeamUtilizationPercent = utilization,
                BlockersResolved = blockersResolved,
                TotalBlockers = blockerTasks.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report dashboard");
            return new ReportDashboardResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Project progress bars
    // ──────────────────────────────────────────────
    public async System.Threading.Tasks.Task<ProjectProgressResult> GetProjectProgressAsync(
        int companyId, int userId, string userRole)
    {
        try
        {
            IQueryable<Project> query = _context.Projects
                .Where(p => p.CompanyID == companyId && p.Status != "Archived");

            if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.CreatedBy == userId ||
                    p.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            else if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            var projects = await query
                .OrderByDescending(p => p.Progress)
                .Take(10)
                .Select(p => new ProjectProgressItem
                {
                    ProjectID = p.ProjectID,
                    ProjectName = p.ProjectName,
                    Progress = p.Progress ?? 0,
                    Status = p.Status
                })
                .ToListAsync();

            return new ProjectProgressResult { Success = true, Projects = projects };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project progress");
            return new ProjectProgressResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Task distribution (status breakdown)
    // ──────────────────────────────────────────────
    public async System.Threading.Tasks.Task<TaskDistributionResult> GetTaskDistributionAsync(
        int companyId, int userId, string userRole)
    {
        try
        {
            IQueryable<Task> query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                .Where(t => t.Project.CompanyID == companyId && t.Status != "Archived");

            query = ApplyRoleScope(query, userId, userRole);

            var tasks = await query.ToListAsync();
            var total = tasks.Count;

            return new TaskDistributionResult
            {
                Success = true,
                Completed = tasks.Count(t => t.Status == "Completed"),
                InProgress = tasks.Count(t => t.Status == "In Progress"),
                InReview = tasks.Count(t => t.Status == "In Review"),
                NotStarted = tasks.Count(t => t.Status == "To Do"),
                Total = total
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task distribution");
            return new TaskDistributionResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Team performance table
    // ──────────────────────────────────────────────
    public async System.Threading.Tasks.Task<TeamPerformanceResult> GetTeamPerformanceAsync(
        int companyId, int userId, string userRole)
    {
        try
        {
            // Get all non-archived tasks for the company
            IQueryable<Task> taskQuery = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Where(t => t.Project.CompanyID == companyId && t.Status != "Archived");

            if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                taskQuery = taskQuery.Where(t => t.Project.CreatedBy == userId ||
                    t.Project.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            else if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                taskQuery = taskQuery.Where(t => t.TaskAssignments.Any(ta => ta.UserID == userId));
            }

            var tasks = await taskQuery.ToListAsync();

            // Group by assigned user
            var assignmentGroups = tasks
                .SelectMany(t => t.TaskAssignments.Select(ta => new { ta.UserID, ta.User, Task = t }))
                .GroupBy(x => x.UserID)
                .Select(g =>
                {
                    var user = g.First().User;
                    var assigned = g.Select(x => x.Task).Distinct().ToList();
                    var completed = assigned.Count(t => t.Status == "Completed");
                    var completedWithDue = assigned.Where(t => t.Status == "Completed" && t.DueDate.HasValue).ToList();
                    var onTime = completedWithDue.Count > 0
                        ? completedWithDue.Count(t => t.DueDate!.Value >= t.CreatedAt.Date)
                        : 0;

                    return new TeamMemberPerformance
                    {
                        UserID = user.UserID,
                        FullName = user.FullName,
                        Initials = GetInitials(user.Fname, user.Lname),
                        AvatarColor = user.AvatarColor,
                        TasksAssigned = assigned.Count,
                        TasksCompleted = completed,
                        CompletionRate = assigned.Count > 0
                            ? Math.Round((decimal)completed / assigned.Count * 100, 0)
                            : 0,
                        OnTimePercent = completedWithDue.Count > 0
                            ? Math.Round((decimal)onTime / completedWithDue.Count * 100, 0)
                            : 100
                    };
                })
                .OrderByDescending(m => m.CompletionRate)
                .ToList();

            return new TeamPerformanceResult { Success = true, Members = assignmentGroups };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team performance");
            return new TeamPerformanceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Saved reports CRUD
    // ──────────────────────────────────────────────
    public async System.Threading.Tasks.Task<SavedReportListResult> GetSavedReportsAsync(int companyId, int userId)
    {
        try
        {
            var reports = await _context.Reports
                .Include(r => r.Creator)
                .Where(r => r.CompanyID == companyId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SavedReportItem
                {
                    ReportID = r.ReportID,
                    ReportName = r.ReportName,
                    ReportType = r.ReportType,
                    Parameters = r.Parameters,
                    CreatedByName = r.Creator.Fname + " " + r.Creator.Lname,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return new SavedReportListResult { Success = true, Reports = reports };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved reports");
            return new SavedReportListResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<CreateReportResult> SaveReportAsync(
        int companyId, int userId, SaveReportRequest request)
    {
        try
        {
            var report = new Report
            {
                CompanyID = companyId,
                CreatedBy = userId,
                ReportName = request.ReportName,
                ReportType = request.ReportType,
                Parameters = request.Parameters,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return new CreateReportResult { Success = true, ReportId = report.ReportID };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving report");
            return new CreateReportResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async System.Threading.Tasks.Task<ServiceResult> DeleteReportAsync(int companyId, int userId, int reportId)
    {
        try
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.ReportID == reportId && r.CompanyID == companyId);

            if (report == null)
                return new ServiceResult { Success = false, ErrorMessage = "Report not found." };

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report");
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────
    private IQueryable<Task> ApplyRoleScope(IQueryable<Task> query, int userId, string userRole)
    {
        if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.TaskAssignments.Any(ta => ta.UserID == userId));
        }
        else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.Project.CreatedBy == userId ||
                t.Project.ProjectMembers.Any(pm => pm.UserID == userId));
        }
        return query;
    }

    private async System.Threading.Tasks.Task<List<int>> GetTeamUserIdsAsync(int companyId, int userId, string userRole)
    {
        var usersQuery = _context.Users
            .Where(u => u.CompanyID == companyId && u.Status == "Active");

        if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            // PM sees members of their projects
            var projectIds = await _context.Projects
                .Where(p => p.CompanyID == companyId &&
                    (p.CreatedBy == userId || p.ProjectMembers.Any(pm => pm.UserID == userId)))
                .Select(p => p.ProjectID)
                .ToListAsync();

            return await _context.ProjectMembers
                .Where(pm => projectIds.Contains(pm.ProjectID))
                .Select(pm => pm.UserID)
                .Distinct()
                .ToListAsync();
        }
        else if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
        {
            return new List<int> { userId };
        }

        // CompanyAdmin / SuperAdmin see all company users
        return await usersQuery.Select(u => u.UserID).ToListAsync();
    }

    private static string GetInitials(string firstName, string lastName)
    {
        var first = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "";
        var last = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() : "";
        return first + last;
    }

    // ──────────────────────────────────────────────
    // Company Admin Report
    // ──────────────────────────────────────────────
    public async System.Threading.Tasks.Task<CompanyReportResult> GetCompanyReportAsync(int companyId)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Users in company
            var users = await _context.Users
                .Where(u => u.CompanyID == companyId)
                .Select(u => new
                {
                    u.UserID,
                    u.Fname,
                    u.Lname,
                    u.Email,
                    u.Status,
                    u.LastLoginAt,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserID == u.UserID)
                        .Join(_context.Roles, ur => ur.RoleID, r => r.RoleID, (ur, r) => r.RoleName)
                        .ToList()
                })
                .ToListAsync();

            var totalUsers = users.Count;
            var activeUsers = users.Count(u => u.Status == "Active");
            var inactiveUsers = totalUsers - activeUsers;

            // Role breakdown
            var roleBreakdown = users
                .SelectMany(u => u.Roles.Select(r => r))
                .GroupBy(r => r)
                .Select(g => new UserRoleBreakdown { RoleName = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList();

            // Subscription info
            var subscription = await _context.Subscriptions
                .Where(s => s.CompanyID == companyId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            string planName = "None";
            decimal planPrice = 0;
            string billingCycle = "";
            int? maxUsers = null;
            int? maxProjects = null;
            string subStatus = "None";
            DateTime? subStart = null;
            DateTime? subEnd = null;

            if (subscription != null)
            {
                subStatus = subscription.Status;
                subStart = subscription.StartDate;
                subEnd = subscription.EndDate;

                var plan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.PlanID == subscription.PlanID);

                if (plan != null)
                {
                    planName = plan.PlanName;
                    planPrice = plan.Price;
                    billingCycle = plan.BillingCycle;
                    maxUsers = plan.MaxUsers;
                    maxProjects = plan.MaxProjects;
                }
            }

            // Recent logins (last 30 days)
            var thirtyDaysAgo = now.AddDays(-30);
            var recentLogins = users.Count(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= thirtyDaysAgo);

            // Recent users (sorted by last login)
            var recentUsers = users
                .OrderByDescending(u => u.LastLoginAt)
                .Take(20)
                .Select(u => new UserActivityItem
                {
                    UserID = u.UserID,
                    FullName = $"{u.Fname} {u.Lname}",
                    Email = u.Email,
                    Status = u.Status,
                    RoleName = u.Roles.FirstOrDefault() ?? "TeamMember",
                    LastLoginAt = u.LastLoginAt
                })
                .ToList();

            return new CompanyReportResult
            {
                Success = true,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                UsersByRole = roleBreakdown,
                PlanName = planName,
                SubscriptionStatus = subStatus,
                SubscriptionStart = subStart,
                SubscriptionEnd = subEnd,
                PlanPrice = planPrice,
                BillingCycle = billingCycle,
                MaxUsers = maxUsers,
                MaxProjects = maxProjects,
                RecentLoginsLast30Days = recentLogins,
                RecentUsers = recentUsers,
                GeneratedAt = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company report");
            return new CompanyReportResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }
}
