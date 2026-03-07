using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("RolePermission")]
public class RolePermission
{
    [Key]
    public int RolePermissionID { get; set; }

    [Required]
    public int RoleID { get; set; }

    [Required]
    public int PermissionID { get; set; }

    // Navigation properties
    [ForeignKey("RoleID")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionID")]
    public virtual Permission Permission { get; set; } = null!;
}
