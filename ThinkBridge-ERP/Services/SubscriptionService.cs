using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(ApplicationDbContext context, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
    {
        return await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
    {
        return await _context.SubscriptionPlans.FindAsync(planId);
    }

    /// <summary>
    /// Registers a company, creates a CompanyAdmin user with a generated @thinkbridge.com email,
    /// creates a subscription record, and returns the data needed for checkout.
    /// </summary>
    public async Task<CompanyRegistrationResult> RegisterCompanyAsync(CompanyRegistrationRequest request)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate plan
                var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
                if (plan == null || !plan.IsActive)
                {
                    return new CompanyRegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid subscription plan selected."
                    };
                }

                // Use the admin-provided email as login email
                var adminEmail = request.AdminEmail.Trim().ToLower();

                // Check if email already exists
                var emailExists = await _context.Users.AnyAsync(u => u.Email == adminEmail);
                if (emailExists)
                {
                    return new CompanyRegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "An account with this email already exists."
                    };
                }

                // Check if company name already exists
                var companyExists = await _context.Companies.AnyAsync(c => c.CompanyName == request.CompanyName);
                if (companyExists)
                {
                    return new CompanyRegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "A company with this name already exists."
                    };
                }

                // 1. Create Company (Status = Pending until payment is confirmed)
                var company = new Company
                {
                    CompanyName = request.CompanyName,
                    Industry = request.Industry,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // 2. Create CompanyAdmin user with generated email
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword);
                var user = new User
                {
                    CompanyID = company.CompanyID,
                    Fname = request.AdminFirstName,
                    Lname = request.AdminLastName,
                    Email = adminEmail,
                    Password = hashedPassword,
                    Phone = request.AdminPhone,
                    AvatarColor = "#0B4F6C",
                    IsSuperAdmin = false,
                    Status = "Inactive", // Will be activated after payment
                    MustChangePassword = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 3. Assign CompanyAdmin role (RoleID = 2)
                var userRole = new UserRole
                {
                    UserID = user.UserID,
                    RoleID = 2, // CompanyAdmin
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);

                // 4. Create Subscription (Status = Pending)
                var isTrial = plan.Price == 0;
                var subscription = new Subscription
                {
                    CompanyID = company.CompanyID,
                    PlanID = request.PlanId,
                    Status = isTrial ? "Trial" : "Pending",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(isTrial ? 14 : 30) // Trial = 14 days, Paid = 30 days
                };
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // 5. If it's a free trial, activate immediately
                if (isTrial)
                {
                    company.Status = "Active";
                    user.Status = "Active";
                    subscription.Status = "Trial";
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Company '{CompanyName}' registered with email {Email}, Plan: {Plan}",
                    request.CompanyName, adminEmail, plan.PlanName);

                return new CompanyRegistrationResult
                {
                    Success = true,
                    CompanyId = company.CompanyID,
                    SubscriptionId = subscription.SubscriptionID,
                    GeneratedEmail = adminEmail,
                    TempPassword = "", // User sets their own password
                    Amount = plan.Price,
                    PlanName = plan.PlanName
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error registering company {CompanyName}", request.CompanyName);
                return new CompanyRegistrationResult
                {
                    Success = false,
                    ErrorMessage = $"Registration error: {ex.Message} {ex.InnerException?.Message}"
                };
            }
        });
    }

    /// <summary>
    /// Activates a subscription after successful payment via PayMongo.
    /// Updates Company.Status, User.Status, and Subscription.Status.
    /// </summary>
    public async Task<bool> ActivateSubscriptionAsync(string checkoutSessionId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find the payment transaction by checkout session ID
                var payment = await _context.PaymentTransactions
                    .Include(p => p.Subscription)
                        .ThenInclude(s => s.Company)
                    .FirstOrDefaultAsync(p => p.CheckoutSessionID == checkoutSessionId);

                if (payment == null)
                {
                    _logger.LogWarning("No payment found for checkout session {SessionId}", checkoutSessionId);
                    return false;
                }

                if (payment.Status == "Paid")
                {
                    _logger.LogInformation("Payment already processed for session {SessionId}", checkoutSessionId);
                    return true; // Idempotent
                }

                // Update payment
                payment.Status = "Paid";
                payment.PaidAt = DateTime.UtcNow;

                // Update subscription
                var subscription = payment.Subscription;
                subscription.Status = "Active";
                subscription.StartDate = DateTime.UtcNow;
                subscription.EndDate = DateTime.UtcNow.AddDays(30);

                // Update company
                var company = subscription.Company;
                company.Status = "Active";

                // Activate all users in this company
                var users = await _context.Users
                    .Where(u => u.CompanyID == company.CompanyID)
                    .ToListAsync();
                foreach (var user in users)
                {
                    user.Status = "Active";
                }

                // Update invoice if exists
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.SubscriptionID == subscription.SubscriptionID && i.Status == "Pending");
                if (invoice != null)
                {
                    invoice.Status = "Paid";
                    invoice.PaidDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Subscription {SubId} activated for company {CompanyId} via checkout {SessionId}",
                    subscription.SubscriptionID, company.CompanyID, checkoutSessionId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error activating subscription for session {SessionId}", checkoutSessionId);
                return false;
            }
        });
    }

    /// <summary>
    /// Checks for subscriptions past their EndDate and marks them Expired.
    /// Also suspends the corresponding companies.
    /// </summary>
    public async Task<int> ExpireOverdueSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;
        var overdueSubscriptions = await _context.Subscriptions
            .Include(s => s.Company)
            .Where(s => (s.Status == "Active" || s.Status == "Trial")
                        && s.EndDate.HasValue
                        && s.EndDate.Value < now)
            .ToListAsync();

        foreach (var sub in overdueSubscriptions)
        {
            sub.Status = "Expired";
            sub.Company.Status = "Suspended";

            // Deactivate all users in this company
            var users = await _context.Users
                .Where(u => u.CompanyID == sub.CompanyID)
                .ToListAsync();
            foreach (var user in users)
            {
                user.Status = "Inactive";
            }

            _logger.LogInformation(
                "Subscription {SubId} expired for company '{CompanyName}'",
                sub.SubscriptionID, sub.Company.CompanyName);
        }

        if (overdueSubscriptions.Any())
        {
            await _context.SaveChangesAsync();
        }

        return overdueSubscriptions.Count;
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(int companyId)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.CompanyID == companyId && (s.Status == "Active" || s.Status == "Trial"))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a readable temp password: Company@2026
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        var password = new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"Tb@{password}1";
    }
}
