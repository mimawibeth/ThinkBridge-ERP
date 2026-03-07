using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Tag")]
public class Tag
{
    [Key]
    public int TagID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    [Required]
    [StringLength(60)]
    public string TagName { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
}
