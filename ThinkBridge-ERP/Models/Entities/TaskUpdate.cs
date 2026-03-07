using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("TaskUpdate")]
public class TaskUpdate
{
    [Key]
    public int UpdateID { get; set; }

    [Required]
    public int TaskID { get; set; }

    [Required]
    public int UserID { get; set; }

    [Required]
    public string UpdateText { get; set; } = string.Empty;

    [StringLength(20)]
    public string? NewStatus { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TaskID")]
    public virtual Task Task { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}
