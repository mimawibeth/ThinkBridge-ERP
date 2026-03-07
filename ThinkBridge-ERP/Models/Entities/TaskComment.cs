using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("TaskComment")]
public class TaskComment
{
    [Key]
    public int TaskCommentID { get; set; }

    [Required]
    public int TaskID { get; set; }

    [Required]
    public int UserID { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsEdited { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("TaskID")]
    public virtual Task Task { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}
