using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("Invoice")]
public class Invoice
{
    [Key]
    public int InvoiceID { get; set; }

    [Required]
    public int SubscriptionID { get; set; }

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? PaidDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SubscriptionID")]
    public virtual Subscription Subscription { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
