using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(ApplicationDbContext context, ILogger<UserManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CompanyUserStatsResult> GetCompanyUserStatsAsync(int companyId)
    {
        try
        {
            var users = _context.Users
                .Where(u => u.CompanyID == companyId && !u.IsSuperAdmin)
                .Where(u => !_context.UserRoles.Any(ur => ur.UserID == u.UserID && ur.Role.RoleName == "CompanyAdmin"));

            var totalUsers = await users.CountAsync();
            var activeUsers = await users.Where(u => u.Status == "Active").CountAsync();
            var inactiveUsers = await users.Where(u => u.Status == "Inactive").CountAsync();

            var projectManagerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "ProjectManager");
            var teamMemberRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "TeamMember");

            var projectManagers = projectManagerRole != null
                ? await _context.UserRoles
                    .Where(ur => ur.RoleID == projectManagerRole.RoleID && ur.User.CompanyID == companyId)
                    .CountAsync()
                : 0;

            var teamMembers = teamMemberRole != null
                ? await _context.UserRoles
                    .Where(ur => ur.RoleID == teamMemberRole.RoleID && ur.User.CompanyID == companyId)
                    .CountAsync()
                : 0;

            var recentUsers = await users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new UserListItem
                {
                    UserID = u.UserID,
                    FullName = u.Fname + " " + u.Lname,
                    FirstName = u.Fname,
                    LastName = u.Lname,
                    Email = u.Email,
                    Phone = u.Phone,
                    AvatarUrl = u.AvatarUrl,
                    AvatarColor = u.AvatarColor,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Role = _context.UserRoles
                        .Where(ur => ur.UserID == u.UserID)
                        .Select(ur => ur.Role.RoleName)
                        .FirstOrDefault() ?? "User"
                })
                .ToListAsync();

            return new CompanyUserStatsResult
            {
                Success = true,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                ProjectManagers = projectManagers,
                TeamMembers = teamMembers,
                RecentUsers = recentUsers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company user stats for company {CompanyId}", companyId);
            return new CompanyUserStatsResult
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving user statistics."
            };
        }
    }

    public async Task<UserListResult> GetUsersAsync(int companyId, UserFilterRequest filter)
    {
        try
        {
            var query = _context.Users
                .Where(u => u.CompanyID == companyId && !u.IsSuperAdmin)
                .Where(u => !_context.UserRoles.Any(ur => ur.UserID == u.UserID && ur.Role.RoleName == "CompanyAdmin"))
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Fname.ToLower().Contains(term) ||
                    u.Lname.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(u => u.Status.ToLower() == filter.Status.ToLower());
            }

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                query = query.Where(u => _context.UserRoles
                    .Any(ur => ur.UserID == u.UserID && ur.Role.RoleName.ToLower() == filter.Role.ToLower()));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new UserListItem
                {
                    UserID = u.UserID,
                    FullName = u.Fname + " " + u.Lname,
                    FirstName = u.Fname,
                    LastName = u.Lname,
                    Email = u.Email,
                    Phone = u.Phone,
                    AvatarUrl = u.AvatarUrl,
                    AvatarColor = u.AvatarColor,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Role = _context.UserRoles
                        .Where(ur => ur.UserID == u.UserID)
                        .Select(ur => ur.Role.RoleName)
                        .FirstOrDefault() ?? "User",
                    AssignedProjectsCount = _context.ProjectMembers
                        .Count(pm => pm.UserID == u.UserID),
                    AssignedTasksCount = _context.TaskAssignments
                        .Count(ta => ta.UserID == u.UserID)
                })
                .ToListAsync();

            return new UserListResult
            {
                Success = true,
                Users = users,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for company {CompanyId}", companyId);
            return new UserListResult
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving users."
            };
        }
    }

    public async Task<UserDetailResult> GetUserByIdAsync(int companyId, int userId)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.UserID == userId && u.CompanyID == companyId && !u.IsSuperAdmin)
                .Select(u => new UserListItem
                {
                    UserID = u.UserID,
                    FullName = u.Fname + " " + u.Lname,
                    FirstName = u.Fname,
                    LastName = u.Lname,
                    Email = u.Email,
                    Phone = u.Phone,
                    AvatarUrl = u.AvatarUrl,
                    AvatarColor = u.AvatarColor,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Role = _context.UserRoles
                        .Where(ur => ur.UserID == u.UserID)
                        .Select(ur => ur.Role.RoleName)
                        .FirstOrDefault() ?? "User",
                    AssignedProjectsCount = _context.ProjectMembers.Count(pm => pm.UserID == u.UserID),
                    AssignedTasksCount = _context.TaskAssignments.Count(ta => ta.UserID == u.UserID)
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new UserDetailResult
                {
                    Success = false,
                    ErrorMessage = "User not found."
                };
            }

            return new UserDetailResult
            {
                Success = true,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} for company {CompanyId}", userId, companyId);
            return new UserDetailResult
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving user details."
            };
        }
    }

    public async Task<CreateUserResult> CreateUserAsync(int companyId, CreateUserRequest request)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate role - only ProjectManager or TeamMember allowed
                if (request.Role != "ProjectManager" && request.Role != "TeamMember")
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid role. Only ProjectManager or TeamMember roles can be assigned."
                    };
                }

                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUser != null)
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "A user with this email address already exists."
                    };
                }

                // Get company name for password generation
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Company not found."
                    };
                }

                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword(company.CompanyName);
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                // Create user
                var user = new User
                {
                    CompanyID = companyId,
                    Fname = request.FirstName,
                    Lname = request.LastName,
                    Email = request.Email,
                    Password = hashedPassword,
                    Phone = request.Phone,
                    AvatarColor = "#0B4F6C",
                    IsSuperAdmin = false,
                    Status = "Active",
                    MustChangePassword = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign role
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        UserID = user.UserID,
                        RoleID = role.RoleID,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Created user {Email} with role {Role} for company {CompanyId}",
                    user.Email, request.Role, companyId);

                return new CreateUserResult
                {
                    Success = true,
                    UserId = user.UserID,
                    TemporaryPassword = tempPassword
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating user for company {CompanyId}", companyId);
                return new CreateUserResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the user."
                };
            }
        });
    }

    public async Task<UpdateUserResult> UpdateUserAsync(int companyId, int userId, UpdateUserRequest request)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.CompanyID == companyId && !u.IsSuperAdmin);

                if (user == null)
                {
                    return new UpdateUserResult
                    {
                        Success = false,
                        ErrorMessage = "User not found."
                    };
                }

                // Update user fields
                if (!string.IsNullOrWhiteSpace(request.FirstName))
                    user.Fname = request.FirstName;

                if (!string.IsNullOrWhiteSpace(request.LastName))
                    user.Lname = request.LastName;

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.ToLower() != user.Email.ToLower())
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.UserID != userId);
                    if (emailExists)
                    {
                        return new UpdateUserResult
                        {
                            Success = false,
                            ErrorMessage = "A user with this email address already exists."
                        };
                    }
                    user.Email = request.Email;
                }

                if (request.Phone != null)
                    user.Phone = request.Phone;

                if (!string.IsNullOrWhiteSpace(request.Status))
                    user.Status = request.Status;

                // Update role if changed
                if (!string.IsNullOrWhiteSpace(request.Role) &&
                    (request.Role == "ProjectManager" || request.Role == "TeamMember"))
                {
                    // Remove existing roles (except CompanyAdmin)
                    var existingRoles = await _context.UserRoles
                        .Where(ur => ur.UserID == userId)
                        .Include(ur => ur.Role)
                        .ToListAsync();

                    foreach (var ur in existingRoles.Where(r => r.Role.RoleName != "CompanyAdmin"))
                    {
                        _context.UserRoles.Remove(ur);
                    }

                    // Add new role
                    var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
                    if (newRole != null)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserID = userId,
                            RoleID = newRole.RoleID,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated user {UserId} for company {CompanyId}", userId, companyId);

                // Get updated user
                var updatedUser = await GetUserByIdAsync(companyId, userId);
                return new UpdateUserResult
                {
                    Success = true,
                    User = updatedUser.User
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating user {UserId} for company {CompanyId}", userId, companyId);
                return new UpdateUserResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while updating the user."
                };
            }
        });
    }

    public async Task<ServiceResult> UpdateUserStatusAsync(int companyId, int userId, string status)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.CompanyID == companyId && !u.IsSuperAdmin);

            if (user == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "User not found."
                };
            }

            user.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated status for user {UserId} to {Status}", userId, status);

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status {UserId}", userId);
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = "An error occurred while updating user status."
            };
        }
    }

    public async Task<ServiceResult> ResetUserPasswordAsync(int companyId, int userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.CompanyID == companyId && !u.IsSuperAdmin);

            if (user == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    ErrorMessage = "User not found."
                };
            }

            var tempPassword = GenerateTemporaryPassword(user.Company!.CompanyName);
            user.Password = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reset password for user {UserId}", userId);

            // Return the temporary password in a custom result
            return new ResetPasswordResult
            {
                Success = true,
                TemporaryPassword = tempPassword
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = "An error occurred while resetting the password."
            };
        }
    }

    private static string GenerateTemporaryPassword(string companyName)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        const string specials = "!@#$%";
        var random = new Random();

        // Clean company name - remove spaces and special characters, take first 8 chars
        var cleanName = new string(companyName
            .Where(c => char.IsLetterOrDigit(c))
            .Take(8)
            .ToArray());

        // Generate random suffix (4 chars + 1 special + 1 number)
        var suffix = new char[6];
        for (int i = 0; i < 4; i++)
        {
            suffix[i] = chars[random.Next(chars.Length)];
        }
        suffix[4] = specials[random.Next(specials.Length)];
        suffix[5] = (char)('0' + random.Next(10));

        // Format: CompanyName_RandomChars
        return $"{cleanName}_{new string(suffix)}";
    }
}
