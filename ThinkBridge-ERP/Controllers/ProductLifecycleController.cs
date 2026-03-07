using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThinkBridge_ERP.Services.Interfaces;

namespace ThinkBridge_ERP.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "TeamMemberOnly")]
public class ProductLifecycleController : ControllerBase
{
    private readonly IProductLifecycleService _productService;
    private readonly ILogger<ProductLifecycleController> _logger;

    public ProductLifecycleController(IProductLifecycleService productService, ILogger<ProductLifecycleController> logger)
    {
        _productService = productService;
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

    /// <summary>
    /// Get lifecycle stages
    /// </summary>
    [HttpGet("stages")]
    public async Task<IActionResult> GetStages()
    {
        var stages = await _productService.GetLifecycleStagesAsync();
        return Ok(new { success = true, data = stages });
    }

    /// <summary>
    /// Get lifecycle pipeline overview
    /// </summary>
    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline([FromQuery] int? projectId = null)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _productService.GetPipelineAsync(companyId, userId, role, projectId);
        return result.Success
            ? Ok(new { success = true, data = result.Stages })
            : BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Get product list with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] string? stage = null,
        [FromQuery] int? projectId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var filter = new ProductFilterRequest
        {
            SearchTerm = search,
            Stage = stage,
            ProjectId = projectId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _productService.GetProductsAsync(companyId, userId, role, filter);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            data = result.Products,
            pagination = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages
            }
        });
    }

    /// <summary>
    /// Get products by project
    /// </summary>
    [HttpGet("by-project/{projectId}")]
    public async Task<IActionResult> GetProductsByProject(int projectId)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var products = await _productService.GetProductsByProjectAsync(companyId, projectId);
        return Ok(new { success = true, data = products });
    }

    /// <summary>
    /// Get single product details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var companyId = GetCurrentCompanyId();
        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        var result = await _productService.GetProductByIdAsync(companyId, id);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Product });
    }

    /// <summary>
    /// Create a new product (ProjectManager only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // Only ProjectManagers can create products (not CompanyAdmin)
        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
            return BadRequest(new { success = false, message = "Product name is required." });

        var result = await _productService.CreateProductAsync(companyId, userId, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = new { productId = result.ProductId } });
    }

    /// <summary>
    /// Update a product (ProjectManager only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // Only ProjectManagers can update products (not CompanyAdmin)
        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var result = await _productService.UpdateProductAsync(companyId, userId, id, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Advance product to a lifecycle stage (ProjectManager and TeamMember for assigned projects)
    /// </summary>
    [HttpPost("{id}/advance")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> AdvanceStage(int id, [FromBody] AdvanceStageBody body)
    {
        if (body == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (body.StageId <= 0)
            return BadRequest(new { success = false, message = "A valid stage is required." });

        // Only ProjectManagers can advance stages
        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var result = await _productService.AdvanceStageAsync(companyId, userId, id, body.StageId, body.Remarks);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Archive a product (ProjectManager only)
    /// </summary>
    [HttpPost("{id}/archive")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> ArchiveProduct(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        // Only ProjectManagers can archive products (not CompanyAdmin)
        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var result = await _productService.ArchiveProductAsync(companyId, userId, id);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Restore an archived product (ProjectManager only)
    /// </summary>
    [HttpPost("{id}/restore")]
    [Authorize(Policy = "ProjectManagerOnly")]
    public async Task<IActionResult> RestoreProduct(int id)
    {
        var companyId = GetCurrentCompanyId();
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (companyId == 0) return BadRequest(new { success = false, message = "Invalid company context." });

        if (!role.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var result = await _productService.RestoreProductAsync(companyId, userId, id);
        if (!result.Success) return BadRequest(new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true });
    }
}

public class AdvanceStageBody
{
    public int StageId { get; set; }
    public string? Remarks { get; set; }
}
