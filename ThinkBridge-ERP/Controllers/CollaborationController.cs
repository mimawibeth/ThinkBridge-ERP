using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/collaboration")]
[Authorize(Policy = "TeamMemberOnly")]
public class CollaborationController : ControllerBase
{
    private readonly ICollaborationService _collaborationService;
    private readonly ILogger<CollaborationController> _logger;

    public CollaborationController(ICollaborationService collaborationService, ILogger<CollaborationController> logger)
    {
        _collaborationService = collaborationService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private int GetCurrentCompanyId()
    {
        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CompanyID")?.Value;
        return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
    }

    private string GetCurrentUserRole()
    {
        return User.Claims.FirstOrDefault(c => c.Type == "PrimaryRole")?.Value ?? "TeamMember";
    }

    // ──────────────────────────────────────────────
    // Posts
    // ──────────────────────────────────────────────

    /// <summary>
    /// Get posts with optional filtering
    /// </summary>
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int? projectId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (companyId == 0 || userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var filter = new PostFilterRequest
        {
            ProjectId = projectId,
            SearchTerm = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _collaborationService.GetPostsAsync(companyId, userId, role, filter);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Posts,
            pagination = new { page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount }
        });
    }

    /// <summary>
    /// Create a new discussion post
    /// </summary>
    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        if (companyId == 0 || userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { success = false, message = "Content is required." });

        var result = await _collaborationService.CreatePostAsync(companyId, userId, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = new { postId = result.PostId } });
    }

    /// <summary>
    /// Delete a post
    /// </summary>
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var result = await _collaborationService.DeletePostAsync(companyId, userId, postId);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // Comments
    // ──────────────────────────────────────────────

    /// <summary>
    /// Get comments for a post
    /// </summary>
    [HttpGet("posts/{postId}/comments")]
    public async Task<IActionResult> GetComments(int postId)
    {
        var result = await _collaborationService.GetCommentsAsync(postId);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Comments });
    }

    /// <summary>
    /// Add a comment to a post
    /// </summary>
    [HttpPost("posts/{postId}/comments")]
    public async Task<IActionResult> AddComment(int postId, [FromBody] AddCommentRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        if (companyId == 0 || userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { success = false, message = "Content is required." });

        var result = await _collaborationService.AddCommentAsync(companyId, userId, postId, request.Content);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = new { commentId = result.CommentId } });
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var result = await _collaborationService.DeleteCommentAsync(userId, commentId);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // Activity Feed
    // ──────────────────────────────────────────────

    /// <summary>
    /// Get aggregated activity feed
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivityFeed(
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (companyId == 0 || userId == 0) return BadRequest(new { success = false, message = "Invalid user context." });

        var filter = new ActivityFeedRequest
        {
            ActivityType = type,
            SearchTerm = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _collaborationService.GetActivityFeedAsync(companyId, userId, role, filter);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Activities,
            pagination = new { page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount }
        });
    }
}

/// <summary>
/// Request body for adding a comment
/// </summary>
public class AddCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
