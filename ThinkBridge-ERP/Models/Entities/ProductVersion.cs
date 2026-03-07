using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("ProductVersion")]
public class ProductVersion
{
    [Key]
    public int VersionID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    [StringLength(30)]
    public string VersionLabel { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime? ReleaseDate { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("ProductID")]
    public virtual Product Product { get; set; } = null!;
}
