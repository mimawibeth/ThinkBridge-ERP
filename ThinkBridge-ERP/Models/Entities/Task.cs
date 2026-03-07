using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Task")]
public class Task
{
    [Key]
    public int TaskID { get; set; }

    [Required]
    public int ProjectID { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "To Do";

    [StringLength(10)]
    public string? Priority { get; set; } = "Medium";

    [Column(TypeName = "date")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DueDate { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal? EstimatedHours { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal? ActualHours { get; set; }

    [Required]
    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProjectID")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public virtual ICollection<TaskUpdate> TaskUpdates { get; set; } = new List<TaskUpdate>();
    public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
