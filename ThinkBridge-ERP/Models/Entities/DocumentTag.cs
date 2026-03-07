using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("DocumentTag")]
public class DocumentTag
{
    [Key]
    public int DocTagID { get; set; }

    [Required]
    public int DocumentID { get; set; }

    [Required]
    public int TagID { get; set; }

    // Navigation properties
    [ForeignKey("DocumentID")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("TagID")]
    public virtual Tag Tag { get; set; } = null!;
}
