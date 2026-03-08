using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace ThinkBridge_ERP.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found for email {Email}", email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password."
                };
            }

            // Check if user is active
            if (user.Status != "Active")
            {
                // Check if user's company subscription expired past grace period
                if (user.CompanyID.HasValue)
                {
                    var subscription = await _context.Subscriptions
                        .Where(s => s.CompanyID == user.CompanyID.Value)
                        .OrderByDescending(s => s.StartDate)
                        .FirstOrDefaultAsync();

                    if (subscription != null && subscription.Status == "Expired")
                    {
                        _logger.LogWarning("Login blocked: User {Email} - subscription expired past grace period", email);
                        return new AuthResult
                        {
                            Success = false,
                            ErrorMessage = "Your subscription has expired and the grace period has ended. Please contact your administrator to renew."
                        };
                    }
                }

                _logger.LogWarning("Login attempt failed: User {Email} is not active (Status: {Status})", email, user.Status);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Your account is not active. Please contact your administrator."
                };
            }

            // Check if account is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds);
                _logger.LogWarning("Login attempt failed: User {Email} is locked out for {Seconds}s", email, remaining);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Account is temporarily locked.",
                    LockoutSeconds = remaining
                };
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 3)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(1);
                    _logger.LogWarning("User {Email} locked out after {Attempts} failed attempts", email, user.FailedLoginAttempts);
                }
                await _context.SaveChangesAsync();

                _logger.LogWarning("Login attempt failed: Invalid password for user {Email} (attempt {Attempts})", email, user.FailedLoginAttempts);
                var lockoutSecs = user.FailedLoginAttempts >= 3 && user.LockoutEnd.HasValue
                    ? (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds)
                    : 0;
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = user.FailedLoginAttempts >= 3
                        ? "Account is temporarily locked due to multiple failed attempts."
                        : "Invalid email or password.",
                    LockoutSeconds = lockoutSecs
                };
            }

            // Reset failed attempts on successful login
            if (user.FailedLoginAttempts > 0)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();
            }

            // Get user roles
            var roles = await GetUserRolesAsync(user.UserID);

            // Determine primary role for dashboard redirect
            var primaryRole = DeterminePrimaryRole(roles, user.IsSuperAdmin);
            var redirectUrl = GetDashboardByRole(primaryRole);

            _logger.LogInformation("User {Email} logged in successfully with role {Role}", email, primaryRole);

            return new AuthResult
            {
                Success = true,
                User = user,
                Roles = roles,
                RedirectUrl = redirectUrl,
                MustChangePassword = user.MustChangePassword
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred during login. Please try again."
            };
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.UserID == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserID == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.RoleName)
            .ToListAsync();
    }

    public string GetDashboardByRole(string primaryRole)
    {
        return primaryRole.ToLower() switch
        {
            "superadmin" => "/Web/SuperAdminDashboard",
            "companyadmin" => "/Web/Dashboard",
            "projectmanager" => "/Web/ProjectManagerDashboard",
            "teammember" => "/Web/TeamMemberDashboard",
            _ => "/Web/Dashboard"
        };
    }

    private string DeterminePrimaryRole(IList<string> roles, bool isSuperAdmin)
    {
        // SuperAdmin takes priority
        if (isSuperAdmin || roles.Contains("SuperAdmin"))
            return "SuperAdmin";

        // Priority order: CompanyAdmin > ProjectManager > TeamMember
        if (roles.Contains("CompanyAdmin"))
            return "CompanyAdmin";

        if (roles.Contains("ProjectManager"))
            return "ProjectManager";

        if (roles.Contains("TeamMember"))
            return "TeamMember";

        // Default fallback
        return "TeamMember";
    }
}
