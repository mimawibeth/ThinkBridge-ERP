using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("DocumentAccess")]
public class DocumentAccess
{
    [Key]
    public int DocAccessID { get; set; }

    [Required]
    public int DocumentID { get; set; }

    [Required]
    public int RoleID { get; set; }

    [Required]
    [StringLength(20)]
    public string AccessLevel { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("DocumentID")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("RoleID")]
    public virtual Role Role { get; set; } = null!;
}
