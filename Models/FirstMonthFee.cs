using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace _2Korriku.Models;

public class FirstMonthFeeChange
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int PeriodId { get; set; }
    public MonthlyFeePeriod? Period { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal PreviousAmount { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public string ActorId { get; set; } = "";
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class FirstMonthFeeModel:IValidatableObject
{
    public int PeriodId { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(typeof(decimal),"0","999999999.99")] public decimal Amount { get; set; }
    [Range(typeof(decimal),"0","999999999.99")] public decimal ExpectedAmount { get; set; }
    [Required,StringLength(200)] public string Reason { get; set; } = "";
    [ValidateNever] public MonthlyInvoiceView? Invoice { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(RequestId==Guid.Empty)yield return new("Rihapni formularin.");
        if(decimal.Round(Amount,2)!=Amount)yield return new("Shuma lejon deri në dy shifra dhjetore.");
    }
}
