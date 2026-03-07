using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("ProductHistory")]
public class ProductHistory
{
    [Key]
    public int HistoryID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    public int StageID { get; set; }

    [Required]
    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Remarks { get; set; }

    // Navigation properties
    [ForeignKey("ProductID")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("StageID")]
    public virtual LifecycleStage Stage { get; set; } = null!;

    [ForeignKey("ChangedBy")]
    public virtual User User { get; set; } = null!;
}
