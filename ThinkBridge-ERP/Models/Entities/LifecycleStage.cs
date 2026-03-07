using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("LifecycleStage")]
public class LifecycleStage
{
    [Key]
    public int StageID { get; set; }

    [Required]
    [StringLength(50)]
    public string StageName { get; set; } = string.Empty;

    public int? StageOrder { get; set; }

    // Navigation properties
    public virtual ICollection<ProductHistory> ProductHistories { get; set; } = new List<ProductHistory>();
}
