using ThinkBridge_ERP.Models.Entities;

namespace ThinkBridge_ERP.Services.Interfaces;

public interface ICompanyService
{
    Task<CompanyListResult> GetCompaniesAsync(CompanyFilterRequest filter);
    Task<CompanyDetailResult> GetCompanyByIdAsync(int companyId);
    Task<CreateCompanyResult> CreateCompanyAsync(CreateCompanyRequest request);
    Task<UpdateCompanyResult> UpdateCompanyAsync(int companyId, UpdateCompanyRequest request);
    Task<ServiceResult> UpdateCompanyStatusAsync(int companyId, string status);
    Task<DashboardStatsResult> GetDashboardStatsAsync();
}

// Request DTOs
public class CompanyFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? PlanName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CreateCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public int PlanId { get; set; }
    public string Status { get; set; } = "Pending";
    public CreateAdminRequest Admin { get; set; } = new();
}

public class CreateAdminRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UpdateCompanyRequest
{
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
    public string? Status { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPhone { get; set; }
}

// Response DTOs
public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CompanyListResult : ServiceResult
{
    public List<CompanyListItem> Companies { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class CompanyListItem
{
    public int CompanyID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? PlanName { get; set; }
    public int UserCount { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminName { get; set; }
}

public class CompanyDetailResult : ServiceResult
{
    public Company? Company { get; set; }
    public Subscription? ActiveSubscription { get; set; }
    public int UserCount { get; set; }
    public int ProjectCount { get; set; }
    public User? AdminUser { get; set; }
}

public class CreateCompanyResult : ServiceResult
{
    public int? CompanyId { get; set; }
    public int? AdminUserId { get; set; }
    public string? TemporaryPassword { get; set; }
}

public class UpdateCompanyResult : ServiceResult
{
    public Company? Company { get; set; }
}

public class DashboardStatsResult : ServiceResult
{
    public int TotalCompanies { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int PendingPayments { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int NewCompaniesThisMonth { get; set; }
    public List<CompanyListItem> RecentCompanies { get; set; } = new();
    public List<RecentPayment> RecentPayments { get; set; } = new();
}

public class RecentPayment
{
    public int PaymentId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
