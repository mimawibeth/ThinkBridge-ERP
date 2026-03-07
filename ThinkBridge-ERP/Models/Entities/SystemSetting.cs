using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("SystemSetting")]
public class SystemSetting
{
    [Key]
    public int SettingID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    [Required]
    [StringLength(80)]
    public string SettingKey { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string SettingValue { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;
}
