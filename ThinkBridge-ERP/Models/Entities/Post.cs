using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Post")]
public class Post
{
    [Key]
    public int PostID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    public int? ProjectID { get; set; }

    [Required]
    public int CreatedBy { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ProjectID")]
    public virtual Project? Project { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<PostDocument> PostDocuments { get; set; } = new List<PostDocument>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
