using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/companyadmin")]
[Authorize(Policy = "CompanyAdminOnly")]
public class CompanyAdminController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly ILogger<CompanyAdminController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public CompanyAdminController(
        IUserManagementService userService,
        ILogger<CompanyAdminController> logger,
        ApplicationDbContext context,
        ISubscriptionService subscriptionService)
    {
        _userService = userService;
        _logger = logger;
        _context = context;
        _subscriptionService = subscriptionService;
    }

    private int GetCurrentCompanyId()
    {
        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CompanyID")?.Value;
        return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
    }

    /// <summary>
    /// Get user statistics for the company admin dashboard
    /// </summary>
    [HttpGet("users/stats")]
    public async Task<IActionResult> GetUserStats()
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        var result = await _userService.GetCompanyUserStatsAsync(companyId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get list of users for the company with filtering and pagination
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? role = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        var filter = new UserFilterRequest
        {
            SearchTerm = search,
            Status = status,
            Role = role,
            Page = page,
            PageSize = pageSize
        };

        var result = await _userService.GetUsersAsync(companyId, filter);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new
        {
            success = true,
            data = result.Users,
            pagination = new
            {
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            }
        });
    }

    /// <summary>
    /// Get user details by ID
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        var result = await _userService.GetUserByIdAsync(companyId, id);
        if (!result.Success)
        {
            return NotFound(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, data = result.User });
    }

    /// <summary>
    /// Create a new user (Project Manager or Team Member)
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { success = false, message = "Email, first name, and last name are required." });
        }

        // Only allow ProjectManager or TeamMember roles
        if (request.Role != "ProjectManager" && request.Role != "TeamMember")
        {
            return BadRequest(new { success = false, message = "Only ProjectManager or TeamMember roles can be created." });
        }

        var result = await _userService.CreateUserAsync(companyId, request);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new
        {
            success = true,
            message = "User created successfully.",
            data = new
            {
                userId = result.UserId,
                temporaryPassword = result.TemporaryPassword
            }
        });
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        var result = await _userService.UpdateUserAsync(companyId, id, request);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, data = result.User });
    }

    /// <summary>
    /// Update user status (Activate/Deactivate)
    /// </summary>
    [HttpPatch("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(new { success = false, message = "Status is required." });
        }

        var result = await _userService.UpdateUserStatusAsync(companyId, id, request.Status);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, message = $"User status updated to {request.Status}." });
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(int id)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid company context." });
        }

        var result = await _userService.ResetUserPasswordAsync(companyId, id);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        // Cast to get temporary password
        var resetResult = result as ResetPasswordResult;
        return Ok(new
        {
            success = true,
            message = "Password reset successfully.",
            data = new
            {
                temporaryPassword = resetResult?.TemporaryPassword
            }
        });
    }

    // ─── Audit Logs ──────────────────────────────

    /// <summary>
    /// Get audit logs for the company with filtering and pagination
    /// </summary>
    [HttpGet("auditlogs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? search = null,
        [FromQuery] string? action = null,
        [FromQuery] string? dateRange = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
            return BadRequest(new { success = false, message = "Invalid company context." });

        try
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.CompanyID == companyId);

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(s) ||
                    a.EntityName.ToLower().Contains(s) ||
                    a.User.Fname.ToLower().Contains(s) ||
                    a.User.Lname.ToLower().Contains(s) ||
                    a.User.Email.ToLower().Contains(s));
            }

            // Action filter
            if (!string.IsNullOrWhiteSpace(action) && action != "All")
            {
                query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));
            }

            // Date range filter
            if (!string.IsNullOrWhiteSpace(dateRange))
            {
                var now = DateTime.UtcNow;
                query = dateRange switch
                {
                    "today" => query.Where(a => a.CreatedAt.Date == now.Date),
                    "7days" => query.Where(a => a.CreatedAt >= now.AddDays(-7)),
                    "30days" => query.Where(a => a.CreatedAt >= now.AddDays(-30)),
                    "90days" => query.Where(a => a.CreatedAt >= now.AddDays(-90)),
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    logId = a.LogID,
                    userName = a.User.Fname + " " + a.User.Lname,
                    userEmail = a.User.Email,
                    action = a.Action,
                    entityName = a.EntityName,
                    entityId = a.EntityID,
                    ipAddress = a.IPAddress,
                    createdAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = logs,
                pagination = new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "Failed to fetch audit logs." });
        }
    }

    // ─── Subscription ────────────────────────────

    /// <summary>
    /// Get current company subscription details
    /// </summary>
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
            return BadRequest(new { success = false, message = "Invalid company context." });

        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Company)
                .Where(s => s.CompanyID == companyId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return Ok(new { success = true, data = (object?)null });

            var userCount = await _context.Users.CountAsync(u => u.CompanyID == companyId && u.Status == "Active");
            var projectCount = await _context.Projects.CountAsync(p => p.CompanyID == companyId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    subscriptionId = subscription.SubscriptionID,
                    planName = subscription.Plan.PlanName,
                    price = subscription.Plan.Price,
                    billingCycle = subscription.Plan.BillingCycle,
                    status = subscription.Status,
                    startDate = subscription.StartDate,
                    endDate = subscription.EndDate,
                    companyName = subscription.Company.CompanyName,
                    maxUsers = subscription.Plan.MaxUsers,
                    maxProjects = subscription.Plan.MaxProjects,
                    currentUsers = userCount,
                    currentProjects = projectCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "Failed to fetch subscription details." });
        }
    }

    // ─── Payment History ─────────────────────────

    /// <summary>
    /// Get payment history for the current company
    /// </summary>
    [HttpGet("payment-history")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
            return BadRequest(new { success = false, message = "Invalid company context." });

        try
        {
            var subscriptionIds = await _context.Subscriptions
                .Where(s => s.CompanyID == companyId)
                .Select(s => s.SubscriptionID)
                .ToListAsync();

            if (!subscriptionIds.Any())
                return Ok(new { success = true, data = Array.Empty<object>() });

            var payments = await _context.PaymentTransactions
                .Include(p => p.Invoice)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Plan)
                .Where(p => subscriptionIds.Contains(p.SubscriptionID))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    paymentId = p.PaymentID,
                    invoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                    planName = p.Subscription.Plan.PlanName,
                    amount = p.Amount,
                    currency = p.Currency,
                    provider = p.Provider,
                    paymentMethod = p.PaymentMethod,
                    status = p.Status,
                    paidAt = p.PaidAt,
                    createdAt = p.CreatedAt,
                    externalTransactionId = p.ExternalTransactionID ?? p.CheckoutSessionID
                })
                .ToListAsync();

            return Ok(new { success = true, data = payments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment history for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "Failed to fetch payment history." });
        }
    }

    /// <summary>
    /// Get receipt details for a specific payment
    /// </summary>
    [HttpGet("payment-history/{paymentId:int}/receipt")]
    public async Task<IActionResult> GetPaymentReceipt(int paymentId)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0)
            return BadRequest(new { success = false, message = "Invalid company context." });

        try
        {
            var payment = await _context.PaymentTransactions
                .Include(p => p.Invoice)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Plan)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Company)
                .Where(p => p.PaymentID == paymentId && p.Subscription.CompanyID == companyId)
                .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound(new { success = false, message = "Payment not found." });

            return Ok(new
            {
                success = true,
                data = new
                {
                    paymentId = payment.PaymentID,
                    invoiceNumber = payment.Invoice?.InvoiceNumber,
                    companyName = payment.Subscription.Company.CompanyName,
                    planName = payment.Subscription.Plan.PlanName,
                    billingCycle = payment.Subscription.Plan.BillingCycle,
                    amount = payment.Amount,
                    currency = payment.Currency,
                    provider = payment.Provider,
                    paymentMethod = payment.PaymentMethod,
                    externalTransactionId = payment.ExternalTransactionID ?? payment.CheckoutSessionID,
                    status = payment.Status,
                    paidAt = payment.PaidAt,
                    createdAt = payment.CreatedAt,
                    subscriptionStart = payment.Subscription.StartDate,
                    subscriptionEnd = payment.Subscription.EndDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receipt for payment {PaymentId}", paymentId);
            return StatusCode(500, new { success = false, message = "Failed to fetch receipt." });
        }
    }
}
