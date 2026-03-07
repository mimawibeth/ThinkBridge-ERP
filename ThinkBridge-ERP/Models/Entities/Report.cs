using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Report")]
public class Report
{
    [Key]
    public int ReportID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    [Required]
    public int CreatedBy { get; set; }

    [Required]
    [StringLength(150)]
    public string ReportName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = string.Empty;

    public string? Parameters { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;
}
