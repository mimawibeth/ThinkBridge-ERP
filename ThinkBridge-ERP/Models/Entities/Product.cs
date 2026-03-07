using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Product")]
public class Product
{
    [Key]
    public int ProductID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    public int? ProjectID { get; set; }

    [StringLength(50)]
    public string? ProductCode { get; set; }

    [Required]
    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Concept";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ProjectID")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<ProductVersion> ProductVersions { get; set; } = new List<ProductVersion>();
    public virtual ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
    public virtual ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
}
