using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ApplicationDbContext context, ILogger<AuthController> logger)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already authenticated, redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "TeamMember";
            return Redirect(_authService.GetDashboardByRole(role));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View("~/Views/Web/Login.cshtml");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request)
    {
        try
        {
            _logger.LogInformation("Login attempt received");

            if (request == null)
            {
                _logger.LogWarning("Login request body is null");
                return Json(new { success = false, message = "Invalid request." });
            }

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Json(new { success = false, message = "Email and password are required." });
            }

            _logger.LogInformation("Authenticating user: {Email}", request.Email);
            var result = await _authService.AuthenticateAsync(request.Email, request.Password);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User!.UserID.ToString()),
                new Claim(ClaimTypes.Email, result.User.Email),
                new Claim(ClaimTypes.Name, $"{result.User.Fname} {result.User.Lname}"),
                new Claim("FirstName", result.User.Fname),
                new Claim("LastName", result.User.Lname),
                new Claim("IsSuperAdmin", result.User.IsSuperAdmin.ToString()),
                new Claim("AvatarUrl", result.User.AvatarUrl ?? ""),
                new Claim("AvatarColor", result.User.AvatarColor ?? "#0B4F6C")
            };

            // Add company claim if user belongs to a company
            if (result.User.CompanyID.HasValue)
            {
                claims.Add(new Claim("CompanyID", result.User.CompanyID.Value.ToString()));
                claims.Add(new Claim("CompanyName", result.User.Company?.CompanyName ?? ""));
            }

            // Add all roles as claims
            foreach (var role in result.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Determine primary role
            var primaryRole = result.Roles.Contains("SuperAdmin") ? "SuperAdmin" :
                              result.Roles.Contains("CompanyAdmin") ? "CompanyAdmin" :
                              result.Roles.Contains("ProjectManager") ? "ProjectManager" : "TeamMember";
            claims.Add(new Claim("PrimaryRole", primaryRole));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                // Session cookie (expires on browser close) unless RememberMe is checked
                IsPersistent = request.RememberMe,
                ExpiresUtc = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Update last login time
            await _authService.UpdateLastLoginAsync(result.User.UserID);

            _logger.LogInformation("User {Email} signed in successfully", request.Email);

            return Json(new
            {
                success = true,
                redirectUrl = result.MustChangePassword ? "/Auth/ChangePassword" : result.RedirectUrl,
                mustChangePassword = result.MustChangePassword,
                user = new
                {
                    name = $"{result.User.Fname} {result.User.Lname}",
                    email = result.User.Email,
                    role = primaryRole
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request?.Email);
            return Json(new { success = false, message = "An error occurred during login. Please try again." });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {Email} signed out", email);

        return Json(new { success = true, redirectUrl = "/Auth/Login" });
    }

    [HttpGet]
    [Authorize]
    [Route("/Auth/Logout")]
    public async Task<IActionResult> LogoutGet()
    {
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {Email} signed out", email);

        return Redirect("/Auth/Login");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            {
                return Json(new { success = false, message = "Password must be at least 8 characters." });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return Json(new { success = false, message = "Passwords do not match." });
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId == 0)
            {
                return Json(new { success = false, message = "Invalid user session. Please log in again." });
            }

            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            {
                return Json(new { success = false, message = "Current password is incorrect." });
            }

            // Hash and save new password
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;

            // Save to database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            var role = User.Claims.FirstOrDefault(c => c.Type == "PrimaryRole")?.Value ?? "TeamMember";
            return Json(new { success = true, redirectUrl = _authService.GetDashboardByRole(role) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return Json(new { success = false, message = "An error occurred while changing password." });
        }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
