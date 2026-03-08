namespace ThinkBridge_ERP.Services.Interfaces;

public interface ICollaborationService
{
    // Posts (Project Discussions)
    Task<PostListResult> GetPostsAsync(int companyId, int userId, string userRole, PostFilterRequest filter);
    Task<CreatePostResult> CreatePostAsync(int companyId, int userId, CreatePostRequest request);
    Task<ServiceResult> DeletePostAsync(int companyId, int userId, int postId);

    // Comments
    Task<CommentListResult> GetCommentsAsync(int companyId, int postId, string userRole);
    Task<CreateCommentResult> AddCommentAsync(int companyId, int userId, int postId, string content);
    Task<ServiceResult> DeleteCommentAsync(int companyId, int userId, int commentId);

    // Activity Feed (aggregates tasks, products, posts, comments)
    Task<ActivityFeedResult> GetActivityFeedAsync(int companyId, int userId, string userRole, ActivityFeedRequest filter);
}

// ─── Request DTOs ──────────────────────────────────────

public class PostFilterRequest
{
    public int? ProjectId { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreatePostRequest
{
    public int? ProjectID { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class ActivityFeedRequest
{
    public string? ActivityType { get; set; } // task, product, post, comment
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}

// ─── Response DTOs ──────────────────────────────────────

public class PostListResult : ServiceResult
{
    public List<PostItem> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class PostItem
{
    public int PostID { get; set; }
    public int? ProjectID { get; set; }
    public string? ProjectName { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string AuthorAvatarColor { get; set; } = "#0B4F6C";
    public string Content { get; set; } = string.Empty;
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePostResult : ServiceResult
{
    public int? PostId { get; set; }
}

public class CommentListResult : ServiceResult
{
    public List<CommentItem> Comments { get; set; } = new();
}

public class CommentItem
{
    public int CommentID { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string AuthorAvatarColor { get; set; } = "#0B4F6C";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentResult : ServiceResult
{
    public int? CommentId { get; set; }
}

public class ActivityFeedResult : ServiceResult
{
    public List<ActivityItem> Activities { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ActivityItem
{
    public string ActivityType { get; set; } = string.Empty; // task, product, post, comment
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;
    public string UserAvatarColor { get; set; } = "#0B4F6C";
    public string Description { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? SubType { get; set; } // completed, created, comment, lifecycle, etc.
    public DateTime CreatedAt { get; set; }
}
