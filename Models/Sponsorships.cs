using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class SponsorshipPlan
{
    public int Id { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(200)] public string Reference { get; set; } = "";
    public List<SponsorshipPlanLine> Lines { get; set; } = [];
}
public class SponsorshipPlanLine
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public SponsorshipPlan Plan { get; set; } = null!;
    [MaxLength(150)] public string Category { get; set; } = "";
    [MaxLength(1000)] public string Activities { get; set; } = "";
    public bool IsSalary { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Budget { get; set; }
}
public class SponsorshipContract
{
    [MaxLength(100)] public string? CertificateNumber { get; set; }
    public DateOnly? CertificateDate { get; set; }
    public int Id { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public int PlanId { get; set; }
    public SponsorshipPlan Plan { get; set; } = null!;
    [MaxLength(150)] public string Sponsor { get; set; } = "";
    [MaxLength(60)] public string SponsorNumber { get; set; } = "";
    [MaxLength(100)] public string Reference { get; set; } = "";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    public DateTime? FundingClosedAt { get; set; }
    [MaxLength(300)] public string? FundingClosureReason { get; set; }
    [MaxLength(450)] public string? FundingClosedBy { get; set; }
    public List<SponsorshipAllocation> Allocations { get; set; } = [];
}
public class SponsorshipAllocation
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public SponsorshipContract Contract { get; set; } = null!;
    public int PlanLineId { get; set; }
    public SponsorshipPlanLine PlanLine { get; set; } = null!;
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
}
public class SponsorshipBeneficiary
{
    [MaxLength(30)] public string? StaffCategory { get; set; }
    public DateOnly? StaffBirthDate { get; set; }
    public int? StaffTeamId { get; set; }
    public TrainingTeam? StaffTeam { get; set; }
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    public int Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    // A stable personnel code or business identifier, not a freely repeated display name.
    [MaxLength(60)] public string Identifier { get; set; } = "";
    public bool IsPerson { get; set; }
}
public class SponsorshipReceipt
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [MaxLength(64)] public string PayloadHash { get; set; } = "";
    public int ContractId { get; set; }
    public SponsorshipContract Contract { get; set; } = null!;
    public DateOnly Date { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [MaxLength(100)] public string BankReference { get; set; } = "";
    public DateTime? CancelledAt { get; set; }
    [MaxLength(300)] public string? CancellationReason { get; set; }
}
public class SponsorshipSpend
{
    public DateOnly? InvoiceDate { get; set; }
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [MaxLength(64)] public string PayloadHash { get; set; } = "";
    public int AllocationId { get; set; }
    public SponsorshipAllocation Allocation { get; set; } = null!;
    public int BeneficiaryId { get; set; }
    public SponsorshipBeneficiary Beneficiary { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateOnly? SalaryMonth { get; set; }
    [MaxLength(10)] public string? SalaryRole { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [MaxLength(500)] public string Description { get; set; } = "";
    [MaxLength(100)] public string DocumentReference { get; set; } = "";
    [MaxLength(100)] public string BankReference { get; set; } = "";
    public DateTime? CancelledAt { get; set; }
    [MaxLength(300)] public string? CancellationReason { get; set; }
}
public class SponsorshipAudit
{
    public int Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string ActorId { get; set; } = "";
    [MaxLength(40)] public string Entity { get; set; } = "";
    public int EntityId { get; set; }
    [MaxLength(300)] public string Reason { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
}

public class SponsorshipPlanInput
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [StringLength(300)] public string? Reason { get; set; }
    [Range(2020,2100)] public int Year { get; set; } = BillingClock.Today.Year;
    [Required,StringLength(200)] public string Title { get; set; } = "Plani i aktiviteteve dhe plani financiar";
    [Required,StringLength(200)] public string Reference { get; set; } = "";
    [MinLength(1),MaxLength(100)] public List<SponsorshipLineInput> Lines { get; set; } = [new()];
    [ValidateNever] public List<SponsorshipAudit> History { get; set; } = [];
}
public class SponsorshipLineInput
{
    public int Id { get; set; }
    [Required,StringLength(150)] public string Category { get; set; } = "";
    [StringLength(1000)] public string? Activities { get; set; }
    public bool IsSalary { get; set; }
    [Range(typeof(decimal),"0.01","999999999.99")] public decimal Budget { get; set; }
}
public class SponsorshipContractInput
{
    [StringLength(100)] public string? CertificateNumber { get; set; }
    public DateOnly? CertificateDate { get; set; }
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [StringLength(300)] public string? Reason { get; set; }
    public int PlanId { get; set; }
    [Required,StringLength(150)] public string Sponsor { get; set; } = "";
    [Required,StringLength(60)] public string SponsorNumber { get; set; } = "";
    [Required,StringLength(100)] public string Reference { get; set; } = "";
    [Required(ErrorMessage="Zgjidhni datën e fillimit.")] public DateOnly? From { get; set; }
    [Required(ErrorMessage="Zgjidhni datën e përfundimit.")] public DateOnly? To { get; set; }
    [Range(typeof(decimal),"0.01","999999999.99")] public decimal Amount { get; set; }
    [MinLength(1),MaxLength(100)] public List<SponsorshipAllocationInput> Allocations { get; set; } = [];
    [ValidateNever] public SponsorshipPlan? Plan { get; set; }
}
public class SponsorshipAllocationInput
{
    public int PlanLineId { get; set; }
    [Range(typeof(decimal),"0","999999999.99")] public decimal Amount { get; set; }
}
public class SponsorshipBeneficiaryInput
{
    [Required,StringLength(150)] public string Name { get; set; } = "";
    [Required,StringLength(60)] public string Identifier { get; set; } = "";
    public bool IsPerson { get; set; } = true;
}
public class SponsorshipReceiptInput
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public int ContractId { get; set; }
    public DateOnly Date { get; set; } = BillingClock.Today;
    [Range(typeof(decimal),"0.01","999999999.99")] public decimal Amount { get; set; }
    [Required,StringLength(100)] public string BankReference { get; set; } = "";
    [ValidateNever] public SponsorshipContract? Contract { get; set; }
}
public class SponsorshipSpendInput
{
    public DateOnly? InvoiceDate { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public int ContractId { get; set; }
    [Range(1,int.MaxValue)] public int AllocationId { get; set; }
    [Range(1,int.MaxValue)] public int BeneficiaryId { get; set; }
    public DateOnly Date { get; set; } = BillingClock.Today;
    [RegularExpression(@"^\d{4}-\d{2}$")] public string? SalaryMonth { get; set; }
    [RegularExpression("^(Player|Staff)$")] public string? SalaryRole { get; set; }
    [Range(typeof(decimal),"0.01","999999999.99")] public decimal Amount { get; set; }
    [Required,StringLength(500)] public string Description { get; set; } = "";
    [Required,StringLength(100)] public string DocumentReference { get; set; } = "";
    [Required,StringLength(100)] public string BankReference { get; set; } = "";
    [ValidateNever] public SponsorshipContract? Contract { get; set; }
    [ValidateNever] public List<SponsorshipBeneficiary> Beneficiaries { get; set; } = [];
}
public class SponsorshipCancelInput
{
    public int Id { get; set; }
    [Required,RegularExpression("Receipt|Spend")] public string Kind { get; set; } = "Spend";
    [Required,StringLength(300)] public string Reason { get; set; } = "";
}
public class SponsorshipFundingCloseInput
{
    [Range(1,int.MaxValue)] public int ContractId { get; set; }
    [Required,RegularExpression("^(ContinuingExpenses|SponsorShortfall)$")] public string Outcome { get; set; } = "";
    [StringLength(180)] public string? Note { get; set; }
}
public class SponsorshipInvoiceInput : SponsorshipSpendInput
{
    [Range(1,int.MaxValue,ErrorMessage="Zgjidhni kategorinë.")] public int PlanLineId { get; set; }
    [ValidateNever] public List<SponsorshipPlanLine> Categories { get; set; } = [];
    [ValidateNever] public List<SponsorshipBudgetSuggestion> Suggestions { get; set; } = [];
}
public record SponsorshipBudgetSuggestion(int AllocationId,int ContractId,string Sponsor,string Reference,string Category,DateOnly Expires,decimal Available,decimal Proposed,bool CanCover);
public class SponsorshipInvoiceList
{
    public List<PlayerContract> PlayerContracts { get; set; } = [];
    public int Year { get; set; }
    public string? Search { get; set; }
    public List<int> Years { get; set; } = [];
    public List<SponsorshipSpend> Invoices { get; set; } = [];
}
public class SponsorshipDashboard
{
    public int Year { get; set; }
    public List<int> Years { get; set; } = [];
    public SponsorshipPlan? Plan { get; set; }
    public List<SponsorshipContract> Contracts { get; set; } = [];
    public List<SponsorshipReceipt> Receipts { get; set; } = [];
    public List<SponsorshipSpend> Spending { get; set; } = [];
}
public class SponsorshipContractDetails
{
    public SponsorshipContract Contract { get; set; } = null!;
    public List<SponsorshipReceipt> Receipts { get; set; } = [];
    public List<SponsorshipSpend> Spending { get; set; } = [];
    public List<SponsorshipAudit> History { get; set; } = [];
    public decimal Received => Receipts.Where(x=>x.CancelledAt==null).Sum(x=>x.Amount);
    public decimal Used => Spending.Where(x=>x.CancelledAt==null).Sum(x=>x.Amount);
}
