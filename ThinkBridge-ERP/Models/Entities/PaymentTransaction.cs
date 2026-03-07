using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("PaymentTransaction")]
public class PaymentTransaction
{
    [Key]
    public int PaymentID { get; set; }

    [Required]
    public int SubscriptionID { get; set; }

    public int? InvoiceID { get; set; }

    [Required]
    [StringLength(40)]
    public string Provider { get; set; } = "PayMongo";

    [StringLength(255)]
    public string? ExternalTransactionID { get; set; }

    [StringLength(255)]
    public string? CheckoutSessionID { get; set; }

    [StringLength(40)]
    public string? PaymentMethod { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "PHP";

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SubscriptionID")]
    public virtual Subscription Subscription { get; set; } = null!;

    [ForeignKey("InvoiceID")]
    public virtual Invoice? Invoice { get; set; }
}
