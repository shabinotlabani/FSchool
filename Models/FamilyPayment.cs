using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class FamilyPayment
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int FamilyId { get; set; }
    public PlayerFamily? Family { get; set; }
    [StringLength(150)] public string FamilyName { get; set; } = "";
    [StringLength(64)] public string PayloadHash { get; set; } = "";
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    [StringLength(50)] public string PaymentMethod { get; set; } = "Cash";
    [StringLength(250)] public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    [StringLength(200)] public string? CancellationReason { get; set; }
    public List<Payment> Payments { get; set; } = [];
}

public class FamilyPaymentModel : RegisterPaymentModel
{
    [Range(1, int.MaxValue)] public int FamilyId { get; set; }
    [Required, StringLength(64)] public string ExpectedState { get; set; } = "";
    public FamilyBillingModel? Account { get; set; }
}

public class FamilyBillingModel
{
    public PlayerFamily Family { get; set; } = new();
    public List<PlayerBillingModel> Members { get; set; } = [];
    public List<FamilyPayment> Payments { get; set; } = [];
    public decimal Due => Members.Sum(m => m.MonthlyDue);
    public string State { get; set; } = "";
}

public class FamilyInvoiceGroup
{
    public PlayerFamily Family { get; set; } = new();
    public List<MonthlyInvoiceView> Invoices { get; set; } = [];
    public decimal Due => Invoices.Sum(i => i.Due);
}
