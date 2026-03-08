using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProjectListResult> GetProjectsAsync(int companyId, int userId, string userRole, ProjectFilterRequest filter)
    {
        try
        {
            IQueryable<Project> query = _context.Projects
                .Include(p => p.Creator)
                .Include(p => p.ProjectMembers).ThenInclude(pm => pm.User)
                .Include(p => p.Tasks)
                .Where(p => p.CompanyID == companyId);

            // TeamMember: only see projects they are assigned to
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            // ProjectManager: see projects they created or are a member of
            else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.CreatedBy == userId || p.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            // CompanyAdmin: sees all projects for their company (no extra filter)

            // Search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.ProjectName.ToLower().Contains(search) ||
                    (p.ProjectCode != null && p.ProjectCode.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status.ToLower() != "all")
            {
                query = query.Where(p => p.Status.ToLower() == filter.Status.ToLower());
            }
            else
            {
                // "All" tab: exclude archived projects
                query = query.Where(p => p.Status != "Archived");
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(p => p.Category != null && p.Category.ToLower() == filter.Category.ToLower());
            }

            var totalCount = await query.CountAsync();

            var projects = await query
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProjectListItem
                {
                    ProjectID = p.ProjectID,
                    ProjectCode = p.ProjectCode ?? $"PRJ-{p.ProjectID:D3}",
                    ProjectName = p.ProjectName,
                    Description = p.Description,
                    Status = p.Status,
                    Progress = p.Progress ?? 0,
                    Category = p.Category,
                    StartDate = p.StartDate,
                    DueDate = p.DueDate,
                    CreatedByName = p.Creator.Fname + " " + p.Creator.Lname,
                    CreatedAt = p.CreatedAt,
                    TaskCount = p.Tasks.Count,
                    CompletedTaskCount = p.Tasks.Count(t => t.Status == "Completed"),
                    Members = p.ProjectMembers.Select(pm => new ProjectMemberInfo
                    {
                        UserID = pm.UserID,
                        FullName = pm.User.Fname + " " + pm.User.Lname,
                        Initials = (pm.User.Fname.Substring(0, 1) + pm.User.Lname.Substring(0, 1)).ToUpper(),
                        AvatarUrl = pm.User.AvatarUrl,
                        MemberRole = pm.MemberRole,
                        AvatarColor = pm.User.AvatarColor
                    }).ToList()
                })
                .ToListAsync();

            return new ProjectListResult
            {
                Success = true,
                Projects = projects,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects for company {CompanyId}", companyId);
            return new ProjectListResult
            {
                Success = false,
                ErrorMessage = "An error occurred while loading projects."
            };
        }
    }

    public async Task<ProjectDetailResult> GetProjectByIdAsync(int companyId, int userId, string userRole, int projectId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Creator)
                .Include(p => p.ProjectMembers).ThenInclude(pm => pm.User)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && p.CompanyID == companyId);

            if (project == null)
            {
                return new ProjectDetailResult { Success = false, ErrorMessage = "Project not found." };
            }

            // TeamMember: can only view if assigned
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase) &&
                !project.ProjectMembers.Any(pm => pm.UserID == userId))
            {
                return new ProjectDetailResult { Success = false, ErrorMessage = "Access denied." };
            }

            // ProjectManager: can only view if creator or member
            if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase) &&
                project.CreatedBy != userId &&
                !project.ProjectMembers.Any(pm => pm.UserID == userId))
            {
                return new ProjectDetailResult { Success = false, ErrorMessage = "Access denied." };
            }

            return new ProjectDetailResult
            {
                Success = true,
                Project = MapToListItem(project)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", projectId);
            return new ProjectDetailResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<CreateProjectResult> CreateProjectAsync(int companyId, int userId, CreateProjectRequest request)
    {
        try
        {
            // Generate project code
            var projectCount = await _context.Projects.CountAsync(p => p.CompanyID == companyId);
            var projectCode = $"PRJ-{(projectCount + 1):D3}";

            var project = new Project
            {
                CompanyID = companyId,
                ProjectCode = projectCode,
                ProjectName = request.ProjectName,
                Description = request.Description,
                Category = request.Category,
                Status = request.Status ?? "Planning",
                Progress = 0,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add team members
            if (request.TeamMemberIds != null && request.TeamMemberIds.Any())
            {
                foreach (var memberId in request.TeamMemberIds)
                {
                    // Verify user belongs to same company
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == memberId && u.CompanyID == companyId);
                    if (user != null)
                    {
                        _context.ProjectMembers.Add(new ProjectMember
                        {
                            ProjectID = project.ProjectID,
                            UserID = memberId,
                            MemberRole = "Member",
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Also add the creator as a member (Lead)
            var creatorAlreadyMember = request.TeamMemberIds?.Contains(userId) ?? false;
            if (!creatorAlreadyMember)
            {
                _context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectID = project.ProjectID,
                    UserID = userId,
                    MemberRole = "Lead",
                    JoinedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Created project '{request.ProjectName}'",
                EntityName = "Project",
                EntityID = project.ProjectID,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectCode} created by user {UserId}", projectCode, userId);

            return new CreateProjectResult
            {
                Success = true,
                ProjectId = project.ProjectID,
                ProjectCode = projectCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project for company {CompanyId}", companyId);
            return new CreateProjectResult
            {
                Success = false,
                ErrorMessage = "An error occurred while creating the project."
            };
        }
    }

    public async Task<ServiceResult> UpdateProjectAsync(int companyId, int userId, int projectId, UpdateProjectRequest request)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && p.CompanyID == companyId);

            if (project == null)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Project not found." };
            }

            // Only the creator (PM) can edit
            if (project.CreatedBy != userId)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Only the project creator can edit this project." };
            }

            if (!string.IsNullOrWhiteSpace(request.ProjectName)) project.ProjectName = request.ProjectName;
            if (request.Description != null) project.Description = request.Description;
            if (!string.IsNullOrWhiteSpace(request.Category)) project.Category = request.Category;
            if (!string.IsNullOrWhiteSpace(request.Status)) project.Status = request.Status;
            if (request.Progress.HasValue) project.Progress = request.Progress.Value;
            if (request.StartDate.HasValue) project.StartDate = request.StartDate.Value;
            if (request.DueDate.HasValue) project.DueDate = request.DueDate.Value;
            project.UpdatedAt = DateTime.UtcNow;

            // Update team members if provided
            if (request.TeamMemberIds != null)
            {
                // Remove existing members except the creator
                var existingMembers = project.ProjectMembers.Where(pm => pm.UserID != userId).ToList();
                _context.ProjectMembers.RemoveRange(existingMembers);

                foreach (var memberId in request.TeamMemberIds.Where(id => id != userId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == memberId && u.CompanyID == companyId);
                    if (user != null)
                    {
                        _context.ProjectMembers.Add(new ProjectMember
                        {
                            ProjectID = project.ProjectID,
                            UserID = memberId,
                            MemberRole = "Member",
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Updated project '{project.ProjectName}'",
                EntityName = "Project",
                EntityID = projectId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} updated by user {UserId}", projectId, userId);

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", projectId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while updating the project." };
        }
    }

    public async Task<ServiceResult> ArchiveOrRestoreProjectAsync(int companyId, int userId, int projectId, string status)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && p.CompanyID == companyId);

            if (project == null)
                return new ServiceResult { Success = false, ErrorMessage = "Project not found." };

            project.Status = status;
            project.UpdatedAt = DateTime.UtcNow;

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"{(status == "Archived" ? "Archived" : "Restored")} project '{project.ProjectName}'",
                EntityName = "Project",
                EntityID = projectId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} status changed to {Status} by CompanyAdmin", projectId, status);

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving/restoring project {ProjectId}", projectId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<ServiceResult> DeleteProjectAsync(int companyId, int userId, int projectId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && p.CompanyID == companyId);

            if (project == null)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Project not found." };
            }

            if (project.CreatedBy != userId)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Only the project creator can delete this project." };
            }

            var projectName = project.ProjectName;

            // Remove members first
            _context.ProjectMembers.RemoveRange(project.ProjectMembers);
            _context.Projects.Remove(project);

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Deleted project '{projectName}'",
                EntityName = "Project",
                EntityID = projectId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectId} deleted by user {UserId}", projectId, userId);
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while deleting the project." };
        }
    }

    public async Task<ProjectStatsResult> GetProjectStatsAsync(int companyId, int userId, string userRole)
    {
        try
        {
            IQueryable<Project> query = _context.Projects.Where(p => p.CompanyID == companyId);

            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.CreatedBy == userId || p.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            // Exclude archived projects from stats
            query = query.Where(p => p.Status != "Archived");
            var projects = await query.ToListAsync();

            return new ProjectStatsResult
            {
                Success = true,
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Status != "Completed" && p.Status != "Archived"),
                CompletedProjects = projects.Count(p => p.Status == "Completed"),
                PlanningProjects = projects.Count(p => p.Status == "Planning"),
                DelayedProjects = projects.Count(p => p.Status == "Delayed" || (p.DueDate.HasValue && p.DueDate.Value < DateTime.UtcNow && p.Status != "Completed"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project stats for company {CompanyId}", companyId);
            return new ProjectStatsResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<List<TeamMemberOption>> GetTeamMembersForCompanyAsync(int companyId)
    {
        try
        {
            var members = await _context.Users
                .Where(u => u.CompanyID == companyId && u.Status == "Active" && !u.IsSuperAdmin)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "TeamMember"))
                .Select(u => new TeamMemberOption
                {
                    UserID = u.UserID,
                    FullName = u.Fname + " " + u.Lname,
                    Initials = (u.Fname.Substring(0, 1) + u.Lname.Substring(0, 1)).ToUpper(),
                    Role = u.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "TeamMember",
                    AvatarColor = u.AvatarColor
                })
                .ToListAsync();

            return members;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team members for company {CompanyId}", companyId);
            return new List<TeamMemberOption>();
        }
    }

    private ProjectListItem MapToListItem(Project p)
    {
        return new ProjectListItem
        {
            ProjectID = p.ProjectID,
            ProjectCode = p.ProjectCode ?? $"PRJ-{p.ProjectID:D3}",
            ProjectName = p.ProjectName,
            Description = p.Description,
            Status = p.Status,
            Progress = p.Progress ?? 0,
            Category = p.Category,
            StartDate = p.StartDate,
            DueDate = p.DueDate,
            CreatedByName = p.Creator.Fname + " " + p.Creator.Lname,
            CreatedAt = p.CreatedAt,
            TaskCount = p.Tasks.Count,
            CompletedTaskCount = p.Tasks.Count(t => t.Status == "Completed"),
            Members = p.ProjectMembers.Select(pm => new ProjectMemberInfo
            {
                UserID = pm.UserID,
                FullName = pm.User.Fname + " " + pm.User.Lname,
                Initials = (pm.User.Fname.Substring(0, 1) + pm.User.Lname.Substring(0, 1)).ToUpper(),
                AvatarUrl = pm.User.AvatarUrl,
                MemberRole = pm.MemberRole,
                AvatarColor = pm.User.AvatarColor
            }).ToList()
        };
    }
}
