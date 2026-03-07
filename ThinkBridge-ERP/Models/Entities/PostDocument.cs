using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("PostDocument")]
public class PostDocument
{
    [Key]
    public int PostDocID { get; set; }

    [Required]
    public int PostID { get; set; }

    [Required]
    public int DocumentID { get; set; }

    // Navigation properties
    [ForeignKey("PostID")]
    public virtual Post Post { get; set; } = null!;

    [ForeignKey("DocumentID")]
    public virtual Document Document { get; set; } = null!;
}
