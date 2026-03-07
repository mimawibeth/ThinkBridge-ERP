using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("ProjectMember")]
public class ProjectMember
{
    [Key]
    public int ProjectMemberID { get; set; }

    [Required]
    public int ProjectID { get; set; }

    [Required]
    public int UserID { get; set; }

    [StringLength(30)]
    public string? MemberRole { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProjectID")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}
