using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Project")]
public class Project
{
    [Key]
    public int ProjectID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    [StringLength(50)]
    public string? ProjectCode { get; set; }

    [Required]
    [StringLength(150)]
    public string ProjectName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planning";

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Progress { get; set; } = 0;

    [StringLength(50)]
    public string? Category { get; set; }

    [Column(TypeName = "date")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DueDate { get; set; }

    [Required]
    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}
