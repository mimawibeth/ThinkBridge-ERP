using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Company")]
public class Company
{
    [Key]
    public int CompanyID { get; set; }

    [Required]
    [StringLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Industry { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}
