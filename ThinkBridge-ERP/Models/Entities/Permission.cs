using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Permission")]
public class Permission
{
    [Key]
    public int PermissionID { get; set; }

    [Required]
    public int ModuleID { get; set; }

    [Required]
    [StringLength(80)]
    public string PermissionKey { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    [ForeignKey("ModuleID")]
    public virtual Module Module { get; set; } = null!;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
