using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("ProjectCategory")]
public class ProjectCategory
{
    [Key]
    public int CategoryID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    [Required]
    [StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;
}
