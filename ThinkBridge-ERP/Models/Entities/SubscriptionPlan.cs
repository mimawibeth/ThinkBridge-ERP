using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThinkBridge_ERP.Models.Entities;

[Table("SubscriptionPlan")]
public class SubscriptionPlan
{
    [Key]
    public int PlanID { get; set; }

    [Required]
    [StringLength(80)]
    public string PlanName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(20)]
    public string BillingCycle { get; set; } = "Monthly";

    public int? MaxUsers { get; set; }

    public int? MaxProjects { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
