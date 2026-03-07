using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Module")]
public class Module
{
    [Key]
    public int ModuleID { get; set; }

    [Required]
    [StringLength(50)]
    public string ModuleName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
