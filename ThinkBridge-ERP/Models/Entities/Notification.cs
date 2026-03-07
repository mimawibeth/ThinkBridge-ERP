using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Notification")]
public class Notification
{
    [Key]
    public int NotificationID { get; set; }

    [Required]
    public int UserID { get; set; }

    [StringLength(40)]
    public string? NotifType { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}
