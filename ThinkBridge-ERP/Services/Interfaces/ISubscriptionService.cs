using ThinkBridge_ERP.Models.Entities;

namespace ThinkBridge_ERP.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlan>> GetActivePlansAsync();
    Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
    Task<CompanyRegistrationResult> RegisterCompanyAsync(CompanyRegistrationRequest request);
    Task<bool> ActivateSubscriptionAsync(string checkoutSessionId);
    Task<int> ExpireOverdueSubscriptionsAsync();
    Task<Subscription?> GetActiveSubscriptionAsync(int companyId);
}

public class CompanyRegistrationRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminPhone { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public int PlanId { get; set; }
}

public class CompanyRegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int CompanyId { get; set; }
    public int SubscriptionId { get; set; }
    public string GeneratedEmail { get; set; } = string.Empty;
    public string TempPassword { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PlanName { get; set; } = string.Empty;
}
