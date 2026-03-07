namespace ThinkBridge_ERP.Services.Interfaces;

public interface IProductLifecycleService
{
    Task<ProductListResult> GetProductsAsync(int companyId, int userId, string userRole, ProductFilterRequest filter);
    Task<ProductDetailResult> GetProductByIdAsync(int companyId, int productId);
    Task<CreateProductResult> CreateProductAsync(int companyId, int userId, CreateProductRequest request);
    Task<ServiceResult> UpdateProductAsync(int companyId, int userId, int productId, UpdateProductRequest request);
    Task<ServiceResult> AdvanceStageAsync(int companyId, int userId, int productId, int stageId, string? remarks);
    Task<ServiceResult> ArchiveProductAsync(int companyId, int userId, int productId);
    Task<ServiceResult> RestoreProductAsync(int companyId, int userId, int productId);
    Task<List<LifecycleStageInfo>> GetLifecycleStagesAsync();
    Task<List<ProductListItem>> GetProductsByProjectAsync(int companyId, int projectId);
    Task<LifecyclePipelineResult> GetPipelineAsync(int companyId, int userId, string userRole, int? projectId = null);
}

// Request DTOs
public class ProductFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? Stage { get; set; }
    public int? ProjectId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateProductRequest
{
    public int? ProjectID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? Description { get; set; }
}

public class UpdateProductRequest
{
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public string? Description { get; set; }
    public int? ProjectID { get; set; }
}

// Response DTOs
public class ProductListResult : ServiceResult
{
    public List<ProductListItem> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ProductListItem
{
    public int ProductID { get; set; }
    public int CompanyID { get; set; }
    public int? ProjectID { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectCode { get; set; }
    public string? ProductCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public int CurrentStageOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductHistoryItem> History { get; set; } = new();
}

public class ProductHistoryItem
{
    public int HistoryID { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Remarks { get; set; }
}

public class ProductDetailResult : ServiceResult
{
    public ProductListItem? Product { get; set; }
}

public class CreateProductResult : ServiceResult
{
    public int? ProductId { get; set; }
}

public class LifecycleStageInfo
{
    public int StageID { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
}

public class LifecyclePipelineResult : ServiceResult
{
    public List<PipelineStageInfo> Stages { get; set; } = new();
}

public class PipelineStageInfo
{
    public int StageID { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public int ProductCount { get; set; }
    public List<PipelineProductInfo> Products { get; set; } = new();
}

public class PipelineProductInfo
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProjectName { get; set; }
}
