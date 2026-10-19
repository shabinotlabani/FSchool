using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace _2Korriku.Models;

public class BankPaymentAllocation
{
    public int Id { get; set; }
    public int BankPaymentId { get; set; }
    public PendingBankPayment BankPayment { get; set; } = null!;
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    [MaxLength(200)] public string Label { get; set; } = "";
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
}
public record BankDebtItem(string Kind,int Id,int StudentId,decimal Due);
public class BankClientBalance
{
    public int? FamilyId { get; set; }
    public List<int> StudentIds { get; set; } = [];
    public List<BankDebtItem> Items { get; set; } = [];
    public decimal Total => Items.Sum(x=>x.Due);
    public decimal Monthly => Items.Where(x=>x.Kind is "Monthly" or "Seasonal").Sum(x=>x.Due);
    public decimal Equipment => Items.Where(x=>x.Kind=="Sale").Sum(x=>x.Due);
    public decimal Personal => Items.Where(x=>x.Kind=="Personal").Sum(x=>x.Due);
    public string State { get; set; } = "";
}
public class BankReceiptView
{
    public PendingBankPayment Request { get; set; } = null!;
    public List<BankPaymentAllocation> Allocations { get; set; } = [];
}
