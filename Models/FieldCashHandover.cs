using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class FieldCashHandover
{
    public int Id { get; set; }
    public string OperatorId { get; set; } = "";
    public ApplicationUser? Operator { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
    public int PaymentCount { get; set; }
    [StringLength(20)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedById { get; set; }
    public ApplicationUser? ConfirmedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedById { get; set; }
    public ApplicationUser? RejectedBy { get; set; }
    [StringLength(300)] public string? RejectionReason { get; set; }
    public ICollection<FieldCashHandoverItem> Items { get; set; } = new List<FieldCashHandoverItem>();
}

public class FieldCashHandoverItem
{
    public int Id { get; set; }
    public int HandoverId { get; set; }
    public FieldCashHandover? Handover { get; set; }
    public int FieldPaymentId { get; set; }
    public FieldPayment? FieldPayment { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
}

public class FieldCashDashboard
{
    public bool IsAdmin { get; set; }
    public decimal Available { get; set; }
    public List<FieldPayment> AvailablePayments { get; set; } = [];
    public List<FieldCashHandover> Handovers { get; set; } = [];
    public List<FieldCashOperatorBalance> OperatorBalances { get; set; } = [];
}

public class FieldCashOperatorBalance
{
    public string OperatorId { get; set; } = "";
    public string OperatorName { get; set; } = "";
    public decimal Available { get; set; }
    public decimal Pending { get; set; }
}
