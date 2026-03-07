using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Folder")]
public class Folder
{
    [Key]
    public int FolderID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    public int? ParentFolderID { get; set; }

    [Required]
    [StringLength(150)]
    public string FolderName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("ParentFolderID")]
    public virtual Folder? ParentFolder { get; set; }

    public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
