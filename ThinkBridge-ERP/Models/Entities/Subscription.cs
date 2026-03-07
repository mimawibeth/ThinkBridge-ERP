using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Subscription")]
public class Subscription
{
    [Key]
    public int SubscriptionID { get; set; }

    [Required]
    public int CompanyID { get; set; }

    [Required]
    public int PlanID { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Trial";

    [Required]
    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EndDate { get; set; }

    // Navigation properties
    [ForeignKey("CompanyID")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("PlanID")]
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
