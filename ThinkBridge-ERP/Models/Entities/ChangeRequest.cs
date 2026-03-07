using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("ChangeRequest")]
public class ChangeRequest
{
    [Key]
    public int CRID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    public int RequestedBy { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProductID")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("RequestedBy")]
    public virtual User Requester { get; set; } = null!;
}
