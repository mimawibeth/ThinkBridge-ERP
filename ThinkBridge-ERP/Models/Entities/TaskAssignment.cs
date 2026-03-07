using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("TaskAssignment")]
public class TaskAssignment
{
    [Key]
    public int TaskAssignmentID { get; set; }

    [Required]
    public int TaskID { get; set; }

    [Required]
    public int UserID { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TaskID")]
    public virtual Task Task { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}
