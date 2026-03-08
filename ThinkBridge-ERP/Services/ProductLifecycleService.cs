using Microsoft.EntityFrameworkCore;
using ThinkBridge_ERP.Data;
using ThinkBridge_ERP.Models.Entities;
using ThinkBridge_ERP.Services.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace ThinkBridge_ERP.Services;

public class ProductLifecycleService : IProductLifecycleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductLifecycleService> _logger;
    private readonly INotificationService _notificationService;

    public ProductLifecycleService(ApplicationDbContext context, ILogger<ProductLifecycleService> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<ProductListResult> GetProductsAsync(int companyId, int userId, string userRole, ProductFilterRequest filter)
    {
        try
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.Project)
                .Include(p => p.ProductHistories).ThenInclude(h => h.Stage)
                .Include(p => p.ProductHistories).ThenInclude(h => h.User)
                .Where(p => p.CompanyID == companyId);

            // Filter out archived products unless specifically filtering for them
            if (string.IsNullOrWhiteSpace(filter.Stage) || !filter.Stage.Equals("Archived", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status != "Archived");
            }

            // Role-based scoping  
            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                // Team members see products linked to projects they're assigned to
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
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    (p.ProductCode != null && p.ProductCode.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            // Stage/status filter
            if (!string.IsNullOrWhiteSpace(filter.Stage) && !filter.Stage.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status == filter.Stage);
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var stages = await _context.LifecycleStages.OrderBy(s => s.StageOrder).ToListAsync();

            var items = products.Select(p => MapToListItem(p, stages)).ToList();

            return new ProductListResult
            {
                Success = true,
                Products = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for company {CompanyId}", companyId);
            return new ProductListResult { Success = false, ErrorMessage = "An error occurred while loading products." };
        }
    }

    public async Task<ProductDetailResult> GetProductByIdAsync(int companyId, int productId)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.Project)
                .Include(p => p.ProductHistories).ThenInclude(h => h.Stage)
                .Include(p => p.ProductHistories).ThenInclude(h => h.User)
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
                return new ProductDetailResult { Success = false, ErrorMessage = "Product not found." };

            var stages = await _context.LifecycleStages.OrderBy(s => s.StageOrder).ToListAsync();

            return new ProductDetailResult
            {
                Success = true,
                Product = MapToListItem(product, stages)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            return new ProductDetailResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<CreateProductResult> CreateProductAsync(int companyId, int userId, CreateProductRequest request)
    {
        try
        {
            // Validate project if provided
            if (request.ProjectID.HasValue)
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectID == request.ProjectID.Value && p.CompanyID == companyId);
                if (project == null)
                    return new CreateProductResult { Success = false, ErrorMessage = "Project not found." };
            }

            // Generate product code if not provided
            var productCode = request.ProductCode;
            if (string.IsNullOrWhiteSpace(productCode))
            {
                var count = await _context.Products.CountAsync(p => p.CompanyID == companyId);
                productCode = $"PRD-{(count + 1):D3}";
            }

            var product = new Product
            {
                CompanyID = companyId,
                ProjectID = request.ProjectID,
                ProductCode = productCode,
                ProductName = request.ProductName,
                Description = request.Description,
                Status = "Concept",
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Create initial lifecycle history entry
            var conceptStage = await _context.LifecycleStages.FirstOrDefaultAsync(s => s.StageName == "Concept");
            if (conceptStage != null)
            {
                _context.ProductHistories.Add(new ProductHistory
                {
                    ProductID = product.ProductID,
                    StageID = conceptStage.StageID,
                    ChangedBy = userId,
                    ChangedAt = DateTime.UtcNow,
                    Remarks = "Product created"
                });
                await _context.SaveChangesAsync();
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Created product '{request.ProductName}'",
                EntityName = "Product",
                EntityID = product.ProductID,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product '{ProductName}' created by user {UserId}", request.ProductName, userId);
            return new CreateProductResult { Success = true, ProductId = product.ProductID };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return new CreateProductResult { Success = false, ErrorMessage = "An error occurred while creating the product." };
        }
    }

    public async Task<ServiceResult> UpdateProductAsync(int companyId, int userId, int productId, UpdateProductRequest request)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
                return new ServiceResult { Success = false, ErrorMessage = "Product not found." };

            if (!string.IsNullOrWhiteSpace(request.ProductName)) product.ProductName = request.ProductName;
            if (request.ProductCode != null) product.ProductCode = request.ProductCode;
            if (request.Description != null) product.Description = request.Description;
            if (request.ProjectID.HasValue) product.ProjectID = request.ProjectID.Value == 0 ? null : request.ProjectID.Value;

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Updated product '{product.ProductName}'",
                EntityName = "Product",
                EntityID = productId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", productId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while updating the product." };
        }
    }

    public async Task<ServiceResult> AdvanceStageAsync(int companyId, int userId, int productId, int stageId, string? remarks)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
                return new ServiceResult { Success = false, ErrorMessage = "Product not found." };

            var stage = await _context.LifecycleStages.FindAsync(stageId);
            if (stage == null)
                return new ServiceResult { Success = false, ErrorMessage = "Stage not found." };

            // Update product status to the stage name
            product.Status = stage.StageName;

            // Log the transition
            _context.ProductHistories.Add(new ProductHistory
            {
                ProductID = productId,
                StageID = stageId,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                Remarks = remarks ?? $"Advanced to {stage.StageName}"
            });

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Advanced product '{product.ProductName}' to '{stage.StageName}'",
                EntityName = "Product",
                EntityID = productId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Auto-update linked project progress based on product lifecycle
            if (product.ProjectID.HasValue)
            {
                await RecalculateProjectProgressFromProductsAsync(product.ProjectID.Value);
            }

            _logger.LogInformation("Product {ProductId} advanced to stage '{StageName}' by user {UserId}",
                productId, stage.StageName, userId);

            // Notify project members about lifecycle change
            if (product.ProjectID.HasValue)
            {
                var memberIds = await _context.ProjectMembers
                    .Where(pm => pm.ProjectID == product.ProjectID.Value && pm.UserID != userId)
                    .Select(pm => pm.UserID)
                    .ToListAsync();

                if (memberIds.Any())
                {
                    var user = await _context.Users.FindAsync(userId);
                    var userName = user?.FullName ?? "Someone";
                    await _notificationService.SendBulkNotificationAsync(
                        memberIds,
                        "lifecycle",
                        $"{userName} moved \"{product.ProductName}\" to {stage.StageName} phase"
                    );
                }
            }

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error advancing product {ProductId} to stage {StageId}", productId, stageId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    public async Task<ServiceResult> ArchiveProductAsync(int companyId, int userId, int productId)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
                return new ServiceResult { Success = false, ErrorMessage = "Product not found." };

            if (product.Status == "Archived")
                return new ServiceResult { Success = false, ErrorMessage = "Product is already archived." };

            product.Status = "Archived";

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Archived product '{product.ProductName}'",
                EntityName = "Product",
                EntityID = productId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving product {ProductId}", productId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while archiving the product." };
        }
    }

    public async Task<ServiceResult> RestoreProductAsync(int companyId, int userId, int productId)
    {
        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
                return new ServiceResult { Success = false, ErrorMessage = "Product not found." };

            if (product.Status != "Archived")
                return new ServiceResult { Success = false, ErrorMessage = "Product is not archived." };

            product.Status = "Concept";

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                CompanyID = companyId,
                UserID = userId,
                Action = $"Restored product '{product.ProductName}'",
                EntityName = "Product",
                EntityID = productId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return new ServiceResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring product {ProductId}", productId);
            return new ServiceResult { Success = false, ErrorMessage = "An error occurred while restoring the product." };
        }
    }

    public async Task<List<LifecycleStageInfo>> GetLifecycleStagesAsync()
    {
        return await _context.LifecycleStages
            .OrderBy(s => s.StageOrder)
            .Select(s => new LifecycleStageInfo
            {
                StageID = s.StageID,
                StageName = s.StageName,
                StageOrder = s.StageOrder ?? 0
            })
            .ToListAsync();
    }

    public async Task<List<ProductListItem>> GetProductsByProjectAsync(int companyId, int projectId)
    {
        try
        {
            var products = await _context.Products
                .Include(p => p.Project)
                .Include(p => p.ProductHistories).ThenInclude(h => h.Stage)
                .Include(p => p.ProductHistories).ThenInclude(h => h.User)
                .Where(p => p.CompanyID == companyId && p.ProjectID == projectId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var stages = await _context.LifecycleStages.OrderBy(s => s.StageOrder).ToListAsync();
            return products.Select(p => MapToListItem(p, stages)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for project {ProjectId}", projectId);
            return new List<ProductListItem>();
        }
    }

    public async Task<LifecyclePipelineResult> GetPipelineAsync(int companyId, int userId, string userRole, int? projectId = null)
    {
        try
        {
            var stages = await _context.LifecycleStages.OrderBy(s => s.StageOrder).ToListAsync();

            IQueryable<Product> query = _context.Products
                .Include(p => p.Project)
                .Where(p => p.CompanyID == companyId && p.Status != "Archived");

            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(p => p.ProjectID == projectId.Value);
            }

            if (userRole.Equals("TeamMember", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectID == null ||
                    p.Project!.ProjectMembers.Any(pm => pm.UserID == userId));
            }
            else if (userRole.Equals("ProjectManager", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ProjectID == null ||
                    p.Project!.CreatedBy == userId ||
                    p.Project!.ProjectMembers.Any(pm => pm.UserID == userId));
            }

            var products = await query.ToListAsync();

            var pipeline = stages.Select(s => new PipelineStageInfo
            {
                StageID = s.StageID,
                StageName = s.StageName,
                StageOrder = s.StageOrder ?? 0,
                ProductCount = products.Count(p => p.Status == s.StageName),
                Products = products.Where(p => p.Status == s.StageName).Select(p => new PipelineProductInfo
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductCode = p.ProductCode,
                    ProjectName = p.Project?.ProjectName
                }).ToList()
            }).ToList();

            return new LifecyclePipelineResult { Success = true, Stages = pipeline };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lifecycle pipeline");
            return new LifecyclePipelineResult { Success = false, ErrorMessage = "An error occurred." };
        }
    }

    /// <summary>
    /// Recalculates project progress by blending task completion (70%) and product lifecycle progress (30%).
    /// Product progress = average of (currentStageOrder / totalStages) across all linked products.
    /// </summary>
    private async Task RecalculateProjectProgressFromProductsAsync(int projectId)
    {
        try
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return;

            // Task-based progress (70% weight)
            var totalTasks = await _context.Tasks.CountAsync(t => t.ProjectID == projectId && t.Status != "Archived");
            var completedTasks = await _context.Tasks.CountAsync(t => t.ProjectID == projectId && t.Status == "Completed");
            decimal taskProgress = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

            // Product lifecycle progress (30% weight)
            var totalStages = await _context.LifecycleStages.CountAsync();
            var products = await _context.Products
                .Where(p => p.ProjectID == projectId)
                .ToListAsync();

            decimal productProgress = 0;
            if (products.Count > 0 && totalStages > 0)
            {
                var stages = await _context.LifecycleStages.ToListAsync();
                decimal totalProductProgress = 0;
                foreach (var prod in products)
                {
                    var currentStage = stages.FirstOrDefault(s => s.StageName == prod.Status);
                    var stageOrder = currentStage?.StageOrder ?? 1;
                    totalProductProgress += (decimal)stageOrder / totalStages * 100;
                }
                productProgress = totalProductProgress / products.Count;
            }

            // Blend: if project has products use 70/30, otherwise 100% tasks
            project.Progress = products.Count > 0
                ? Math.Round(taskProgress * 0.7m + productProgress * 0.3m, 2)
                : Math.Round(taskProgress, 2);

            // Auto-update project status
            if (project.Progress >= 100 && project.Status != "Completed" && project.Status != "Archived")
            {
                project.Progress = 100;
                project.Status = "Completed";
            }
            else if (project.Progress > 0 && project.Progress < 100 && project.Status == "Planning")
            {
                project.Status = "In Progress";
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} progress recalculated (with products): {Progress}%", projectId, project.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating progress for project {ProjectId}", projectId);
        }
    }

    private ProductListItem MapToListItem(Product p, List<LifecycleStage> stages)
    {
        var currentStage = stages.FirstOrDefault(s => s.StageName == p.Status);

        return new ProductListItem
        {
            ProductID = p.ProductID,
            CompanyID = p.CompanyID,
            ProjectID = p.ProjectID,
            ProjectName = p.Project?.ProjectName,
            ProjectCode = p.Project?.ProjectCode,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            Description = p.Description,
            Status = p.Status,
            CurrentStage = currentStage?.StageName ?? p.Status,
            CurrentStageOrder = currentStage?.StageOrder ?? 0,
            CreatedAt = p.CreatedAt,
            History = p.ProductHistories
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new ProductHistoryItem
                {
                    HistoryID = h.HistoryID,
                    StageName = h.Stage.StageName,
                    StageOrder = h.Stage.StageOrder ?? 0,
                    ChangedByName = h.User.Fname + " " + h.User.Lname,
                    ChangedAt = h.ChangedAt,
                    Remarks = h.Remarks
                }).ToList()
        };
    }
}
