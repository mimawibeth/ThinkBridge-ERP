using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("CalendarEvent")]
public class CalendarEvent
{
    [Key]
    public int EventID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    public int? ProjectID { get; set; }

    [Required]
    public int CreatedBy { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool AllDay { get; set; } = false;

    [StringLength(50)]
    public string? Location { get; set; }

    /// <summary>
    /// Event priority: Low, Medium, High
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Event color for calendar display (hex code)
    /// </summary>
    [StringLength(20)]
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ProjectID")]
    public virtual Project? Project { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;
}
