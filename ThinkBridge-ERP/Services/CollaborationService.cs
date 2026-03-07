using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace ThinkBridge_ERP.Services;

public class CollaborationService : ICollaborationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CollaborationService> _logger;

    public CollaborationService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<CollaborationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────
    // Posts (Project Discussions)
    // ──────────────────────────────────────────────
    public async Task<PostListResult> GetPostsAsync(int companyId, int userId, string userRole, PostFilterRequest filter)
    {
        try
        {
            IQueryable<Post> query = _context.Posts
                .Include(p => p.Creator)
                .Include(p => p.Project)
                .Include(p => p.Comments)
                .Where(p => p.CompanyID == companyId);

            // Role-based scoping
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                // Team members see posts for projects they belong to, plus general posts
                query = query.Where(p => p.ProjectID == null ||
                    p.Project!.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectID == null ||
                    p.Project!.CreatedBy == userId ||
                    p.Project!.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            // Project filter
            if (filter.ProjectId.HasValue && filter.ProjectId.Value > 0)
            {
                query = query.Where(p => p.ProjectID == filter.ProjectId.Value);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(p => p.Content.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new PostItem
                {
                    PostID = p.PostID,
                    ProjectID = p.ProjectID,
                    ProjectName = p.Project != null ? p.Project.ProjectName : null,
                    AuthorName = p.Creator.Fname + " " + p.Creator.Lname,
                    AuthorInitials = (p.Creator.Fname.Substring(0, 1) + p.Creator.Lname.Substring(0, 1)).ToUpper(),
                    AuthorAvatarColor = p.Creator.AvatarColor,
                    Content = p.Content,
                    CommentCount = p.Comments.Count,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return new PostListResult
            {
                Success = true,
                Posts = posts,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts");
            return new PostListResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<CreatePostResult> CreatePostAsync(int companyId, int userId, CreatePostRequest request)
    {
        try
        {
            var post = new Post
            {
                CompanyID = companyId,
                ProjectID = request.ProjectID,
                CreatedBy = userId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Notify project members about the new post
            if (request.ProjectID.HasValue)
            {
                var user = await _context.Users.FindAsync(userId);
                var project = await _context.Projects.FindAsync(request.ProjectID.Value);
                var memberIds = await _context.ProjectMembers
                    .Where(pm => pm.ProjectID == request.ProjectID.Value && pm.UserID != userId)
                    .Select(pm => pm.UserID)
                    .ToListAsync();

                if (memberIds.Any())
                {
                    var userName = user?.FullName ?? "Someone";
                    var projectName = project?.ProjectName ?? "a project";
                    await _notificationService.SendBulkNotificationAsync(
                        memberIds,
                        "post",
                        $"{userName} posted a discussion in {projectName}"
                    );
                }
            }

            return new CreatePostResult { Success = true, PostId = post.PostID };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return new CreatePostResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<ServiceResult> DeletePostAsync(int companyId, int userId, int postId)
    {
        try
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.PostID == postId && p.CompanyID == companyId);

            if (post == null)
                return new ServiceResult { Success = false, ErrorMessage = "Post not found." };

            // Only the author or admins can delete
            if (post.CreatedBy != userId)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsSuperAdmin)
                    return new ServiceResult { Success = false, ErrorMessage = "You can only delete your own posts." };
            }

            _context.Comments.RemoveRange(post.Comments);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", postId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Comments
    // ──────────────────────────────────────────────
    public async Task<CommentListResult> GetCommentsAsync(int postId)
    {
        try
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostID == postId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentItem
                {
                    CommentID = c.CommentID,
                    AuthorName = c.User.Fname + " " + c.User.Lname,
                    AuthorInitials = (c.User.Fname.Substring(0, 1) + c.User.Lname.Substring(0, 1)).ToUpper(),
                    AuthorAvatarColor = c.User.AvatarColor,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return new CommentListResult { Success = true, Comments = comments };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return new CommentListResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<CreateCommentResult> AddCommentAsync(int companyId, int userId, int postId, string content)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.CompanyID != companyId)
                return new CreateCommentResult { Success = false, ErrorMessage = "Post not found." };

            var comment = new Comment
            {
                PostID = postId,
                UserID = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Notify the post author
            if (post.CreatedBy != userId)
            {
                var commenter = await _context.Users.FindAsync(userId);
                var commenterName = commenter?.FullName ?? "Someone";
                await _notificationService.SendNotificationAsync(
                    post.CreatedBy,
                    "comment",
                    $"{commenterName} commented on your discussion"
                );
            }

            return new CreateCommentResult { Success = true, CommentId = comment.CommentID };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to post {PostId}", postId);
            return new CreateCommentResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<ServiceResult> DeleteCommentAsync(int userId, int commentId)
    {
        try
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return new ServiceResult { Success = false, ErrorMessage = "Comment not found." };

            if (comment.UserID != userId)
                return new ServiceResult { Success = false, ErrorMessage = "You can only delete your own comments." };

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Activity Feed (aggregates multiple sources)
    // ──────────────────────────────────────────────
    public async Task<ActivityFeedResult> GetActivityFeedAsync(int companyId, int userId, string userRole, ActivityFeedRequest filter)
    {
        try
        {
            var activities = new List<ActivityItem>();

            // Get project IDs the user can see
            var visibleProjectIds = await GetVisibleProjectIdsAsync(companyId, userId, userRole);

            bool includeAll = string.IsNullOrWhiteSpace(filter.ActivityType) ||
                              filter.ActivityType.Equals("all", StringComparison.OrdinalIgnoreCase);

            // 1. Task Updates (status changes, creations)
            if (includeAll || filter.ActivityType == "task")
            {
                var taskUpdates = await _context.TaskUpdates
                    .Include(tu => tu.User)
                    .Include(tu => tu.Task).ThenInclude(t => t.Project)
                    .Where(tu => visibleProjectIds.Contains(tu.Task.ProjectID))
                    .OrderByDescending(tu => tu.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                activities.AddRange(taskUpdates.Select(tu => new ActivityItem
                {
                    ActivityType = "task",
                    UserName = tu.User.FullName,
                    UserInitials = GetInitials(tu.User.Fname, tu.User.Lname),
                    UserAvatarColor = tu.User.AvatarColor,
                    Description = tu.UpdateText,
                    EntityName = tu.Task.Title,
                    SubType = !string.IsNullOrEmpty(tu.NewStatus) && tu.NewStatus == "Completed" ? "completed" : "updated",
                    CreatedAt = tu.CreatedAt
                }));
            }

            // 2. Product Lifecycle events
            if (includeAll || filter.ActivityType == "product")
            {
                var productHistory = await _context.ProductHistories
                    .Include(ph => ph.User)
                    .Include(ph => ph.Product)
                    .Include(ph => ph.Stage)
                    .Where(ph => ph.Product.CompanyID == companyId &&
                        (ph.Product.ProjectID == null || visibleProjectIds.Contains(ph.Product.ProjectID.Value)))
                    .OrderByDescending(ph => ph.ChangedAt)
                    .Take(50)
                    .ToListAsync();

                activities.AddRange(productHistory.Select(ph => new ActivityItem
                {
                    ActivityType = "product",
                    UserName = ph.User.FullName,
                    UserInitials = GetInitials(ph.User.Fname, ph.User.Lname),
                    UserAvatarColor = ph.User.AvatarColor,
                    Description = $"moved product to {ph.Stage.StageName} phase",
                    EntityName = ph.Product.ProductName,
                    SubType = "lifecycle",
                    CreatedAt = ph.ChangedAt
                }));
            }

            // 3. Posts
            if (includeAll || filter.ActivityType == "post")
            {
                var posts = await _context.Posts
                    .Include(p => p.Creator)
                    .Include(p => p.Project)
                    .Where(p => p.CompanyID == companyId &&
                        (p.ProjectID == null || visibleProjectIds.Contains(p.ProjectID.Value)))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                activities.AddRange(posts.Select(p => new ActivityItem
                {
                    ActivityType = "post",
                    UserName = p.Creator.FullName,
                    UserInitials = GetInitials(p.Creator.Fname, p.Creator.Lname),
                    UserAvatarColor = p.Creator.AvatarColor,
                    Description = p.Content.Length > 120 ? p.Content.Substring(0, 120) + "..." : p.Content,
                    EntityName = p.Project?.ProjectName ?? "General",
                    SubType = "discussion",
                    CreatedAt = p.CreatedAt
                }));
            }

            // 4. Comments
            if (includeAll || filter.ActivityType == "comment")
            {
                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Post).ThenInclude(p => p.Project)
                    .Where(c => c.Post.CompanyID == companyId &&
                        (c.Post.ProjectID == null || visibleProjectIds.Contains(c.Post.ProjectID.Value)))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                activities.AddRange(comments.Select(c => new ActivityItem
                {
                    ActivityType = "comment",
                    UserName = c.User.FullName,
                    UserInitials = GetInitials(c.User.Fname, c.User.Lname),
                    UserAvatarColor = c.User.AvatarColor,
                    Description = c.Content.Length > 120 ? c.Content.Substring(0, 120) + "..." : c.Content,
                    EntityName = c.Post.Project?.ProjectName ?? "General Discussion",
                    SubType = "comment",
                    CreatedAt = c.CreatedAt
                }));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                activities = activities
                    .Where(a => a.Description.ToLower().Contains(search) ||
                        (a.EntityName ?? "").ToLower().Contains(search) ||
                        a.UserName.ToLower().Contains(search))
                    .ToList();
            }

            // Sort by date, paginate
            var sorted = activities.OrderByDescending(a => a.CreatedAt).ToList();
            var totalCount = sorted.Count;
            var paged = sorted
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new ActivityFeedResult
            {
                Success = true,
                Activities = paged,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity feed");
            return new ActivityFeedResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────
    private async Task<List<int>> GetVisibleProjectIdsAsync(int companyId, int userId, string userRole)
    {
        IQueryable<Project> query = _context.Projects.Where(p => p.CompanyID == companyId);

        if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.ProjectMembers.Any(pm => pm.UserID == userId));
        }
        else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.CreatedBy == userId ||
                p.ProjectMembers.Any(pm => pm.UserID == userId));
        }

        return await query.Select(p => p.ProjectID).ToListAsync();
    }

    private static string GetInitials(string firstName, string lastName)
    {
        var first = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "";
        var last = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() : "";
        return first + last;
    }
}
