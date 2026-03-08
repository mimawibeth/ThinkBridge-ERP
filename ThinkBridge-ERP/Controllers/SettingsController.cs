using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using Task = System.Threading.Tasks.Task;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId() =>
        int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

    // GET api/settings/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Get user roles
            var roles = await _context.UserRoles
                .Where(ur => ur.UserID == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();

            var primaryRole = user.IsSuperAdmin ? "SuperAdmin" :
                roles.Contains("CompanyAdmin") ? "CompanyAdmin" :
                roles.Contains("ProjectManager") ? "ProjectManager" : "TeamMember";

            return Ok(new
            {
                userId = user.UserID,
                fname = user.Fname,
                lname = user.Lname,
                email = user.Email,
                phone = user.Phone,
                avatarUrl = user.AvatarUrl,
                avatarColor = user.AvatarColor,
                status = user.Status,
                companyName = user.Company?.CompanyName,
                companyId = user.CompanyID,
                role = primaryRole,
                roles = roles,
                isSuperAdmin = user.IsSuperAdmin,
                lastLoginAt = user.LastLoginAt,
                createdAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile");
            return StatusCode(500, new { message = "An error occurred while fetching profile." });
        }
    }

    // PUT api/settings/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Fname) || string.IsNullOrWhiteSpace(request.Lname))
                return BadRequest(new { message = "First name and last name are required." });

            if (request.Fname.Length > 150)
                return BadRequest(new { message = "First name must be 150 characters or less." });

            if (request.Lname.Length > 150)
                return BadRequest(new { message = "Last name must be 150 characters or less." });

            if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Length > 30)
                return BadRequest(new { message = "Phone number must be 30 characters or less." });

            user.Fname = request.Fname.Trim();
            user.Lname = request.Lname.Trim();
            user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

            // Update avatar color if provided and valid hex
            if (!string.IsNullOrWhiteSpace(request.AvatarColor) &&
                System.Text.RegularExpressions.Regex.IsMatch(request.AvatarColor, @"^#[0-9A-Fa-f]{6}$"))
            {
                user.AvatarColor = request.AvatarColor;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Profile updated for user {UserId}", userId);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully.",
                fname = user.Fname,
                lname = user.Lname,
                phone = user.Phone,
                avatarColor = user.AvatarColor
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, new { message = "An error occurred while updating profile." });
        }
    }

    // POST api/settings/change-password
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new { message = "Current password is required." });

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
                return BadRequest(new { message = "Password must be at least 12 characters." });

            if (!request.NewPassword.Any(char.IsUpper))
                return BadRequest(new { message = "Password must include an uppercase letter." });

            if (!request.NewPassword.Any(char.IsLower))
                return BadRequest(new { message = "Password must include a lowercase letter." });

            if (!request.NewPassword.Any(char.IsDigit))
                return BadRequest(new { message = "Password must include a number." });

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[^a-zA-Z0-9]"))
                return BadRequest(new { message = "Password must include a special character." });

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { message = "New passwords do not match." });

            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                return BadRequest(new { message = "Current password is incorrect." });

            // Check new password is different
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.Password))
                return BadRequest(new { message = "New password must be different from current password." });

            // Hash and save
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed via settings for user {UserId}", userId);

            return Ok(new { success = true, message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { message = "An error occurred while changing password." });
        }
    }

    // GET api/settings/onboarding
    [HttpGet("onboarding")]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });
            return Ok(new { hasCompleted = user.HasCompletedOnboarding });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching onboarding status");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    // POST api/settings/onboarding/complete
    [HttpPost("onboarding/complete")]
    public async Task<IActionResult> CompleteOnboarding()
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });
            user.HasCompletedOnboarding = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }
}

// DTOs
public class UpdateProfileRequest
{
    public string Fname { get; set; } = string.Empty;
    public string Lname { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarColor { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
