using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class ExpenseCategory
{
    public int Id { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    [StringLength(100)] public string Name { get; set; } = "";
    public int? ParentId { get; set; }
    public ExpenseCategory? Parent { get; set; }
    public bool IsActive { get; set; } = true;
}
public class ExpenseCategoryChange
{
    public int Id { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Expense
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [StringLength(64)] public string PayloadHash { get; set; } = "";
    public int CategoryId { get; set; }
    public ExpenseCategory? Category { get; set; }
    public int? SubcategoryId { get; set; }
    public ExpenseCategory? Subcategory { get; set; }
    [StringLength(100)] public string CategoryName { get; set; } = "";
    [StringLength(100)] public string? SubcategoryName { get; set; }
    public DateOnly Date { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [StringLength(200)] public string Description { get; set; } = "";
    [StringLength(150)] public string? Recipient { get; set; }
    [StringLength(100)] public string? DocumentNumber { get; set; }
    [StringLength(20)] public string Method { get; set; } = "Cash";
    [StringLength(500)] public string? Notes { get; set; }
    public string CreatedById { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancelledById { get; set; }
    public ApplicationUser? CancelledBy { get; set; }
    [StringLength(200)] public string? CancellationReason { get; set; }
    [NotMapped] public string Number => $"SHP-{Date.Year}-{Id:000000}";
    [NotMapped] public string CategoryLabel => CategoryName+(SubcategoryName==null?"":" → "+SubcategoryName);
}
public class ExpenseCategoryModel : IValidatableObject
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(100)] public string Name { get; set; } = "";
    public int? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    [StringLength(200)] public string? Reason { get; set; }
    [ValidateNever] public List<ExpenseCategory> Parents { get; set; } = [];
    [ValidateNever] public List<ExpenseCategoryChange> History { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context){
        if(Id!=0&&string.IsNullOrWhiteSpace(Reason))yield return new("Shkruani arsyen e ndryshimit.");
    }
}
public class ExpenseCreateModel : IValidatableObject
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(1,int.MaxValue,ErrorMessage="Zgjidhni kategorinë.")] public int CategoryId { get; set; }
    public int? SubcategoryId { get; set; }
    [Required] public DateOnly? Date { get; set; } = BillingClock.Today;
    [Range(typeof(decimal),"0.01","999999999.99")] public decimal Amount { get; set; }
    [Required,StringLength(200)] public string Description { get; set; } = "";
    [StringLength(150)] public string? Recipient { get; set; }
    [StringLength(100)] public string? DocumentNumber { get; set; }
    [Required,RegularExpression("Cash|Bank|Other")] public string Method { get; set; } = "Cash";
    [StringLength(500)] public string? Notes { get; set; }
    [ValidateNever] public List<ExpenseCategory> Categories { get; set; } = [];
    [ValidateNever] public decimal Available { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context){
        if(RequestId==Guid.Empty)yield return new("Rihapni formularin.");
        if(Date.HasValue&&(Date<new DateOnly(1900,1,1)||Date>BillingClock.Today))yield return new("Data e shpenzimit duhet të jetë nga viti 1900 deri sot.");
        if(decimal.Round(Amount,2)!=Amount)yield return new("Shuma lejon deri në dy shifra dhjetore.");
    }
}
public class ExpenseCancelModel
{
    public int Id { get; set; }
    [Required,StringLength(200)] public string Reason { get; set; } = "";
    [ValidateNever] public Expense? Expense { get; set; }
}
public class MoneyDateFilter : IValidatableObject
{
    [Required] public DateOnly? From { get; set; } = TariffService.CurrentMonth;
    [Required] public DateOnly? To { get; set; } = BillingClock.Today;
    public IEnumerable<ValidationResult> Validate(ValidationContext context){
        if(From.HasValue&&To.HasValue&&(From<new DateOnly(1900,1,1)||From>To||To>BillingClock.Today))yield return new("Zgjidhni periudhë të vlefshme deri në datën e sotme.");
    }
}
public class ExpenseIndexModel
{
    public MoneyDateFilter Filter { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeCancelled { get; set; }
    public List<ExpenseCategory> Categories { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
}
public record MoneyMovement(DateOnly Date,string Group,string Category,string? Subcategory,decimal Income,decimal Outgoing,string Reference,string Description,string Controller,string Action,int Id,string Method = "Other");
public record MoneyMethodBalance(string Method, string Name, decimal Opening, decimal Income, decimal Outgoing, decimal InitialFunds = 0)
{
    public decimal Net => InitialFunds + Income - Outgoing;
    public decimal Closing => Opening + Net;
}
public record MoneyReportRow(string Name,decimal Gross,decimal Reversed)
{
    public decimal Net => Gross-Reversed;
}
public class CashReportModel
{
    public List<MoneyMethodBalance> MethodBalances { get; set; } = [];
    public bool HasOtherMethod => MethodBalances.Any(x => x.Method == "Other");
    public bool HasUndatedCancellations { get; set; }
    public MoneyDateFilter Filter { get; set; } = new();
    public decimal Opening { get; set; }
    public List<MoneyReportRow> IncomeRows { get; set; } = [];
    public List<MoneyMovement> Movements { get; set; } = [];
    public decimal Income => IncomeRows.Sum(x=>x.Net);
    public decimal InitialFunds => Movements.Where(x => x.Group == TreasuryEntry.OpeningGroup).Sum(x => x.Income);
    public decimal Outgoing => Movements.Sum(x=>x.Outgoing);
    public decimal Net => InitialFunds+Income-Outgoing;
    public decimal Closing => Opening+Net;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
