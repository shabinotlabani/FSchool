using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class FeePlan
{
    public bool IsFamily { get; set; }
    public bool IsActive { get; set; } = true;
    public int Id { get; set; }
    [StringLength(100)] public string Name { get; set; } = "";
    public bool IsWaiver { get; set; }
}
public class FeePlanPrice
{
    [Column(TypeName="numeric(18,2)")] public decimal? FamilyFirstAmount { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal? FamilyAdditionalAmount { get; set; }
    public int FamilyFirstCount { get; set; } = 2;
    public bool SingleUsesStandard { get; set; } = true;
    public int Id { get; set; }
    public int FeePlanId { get; set; }
    public FeePlan? FeePlan { get; set; }
    public DateOnly EffectiveMonth { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class StudentFeeAssignment
{
    public int? FamilyId { get; set; }
    public PlayerFamily? Family { get; set; }
    public int? FamilyOrder { get; set; }
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int FeePlanId { get; set; }
    public FeePlan? FeePlan { get; set; }
    public DateOnly EffectiveMonth { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    [StringLength(400)] public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class FeePriceModel : IValidatableObject
{
    public bool IsFamily { get; set; }
    [Range(typeof(decimal),"0.01","999999.99")] public decimal? FamilyAdditionalAmount { get; set; }
    [Range(1,20)] public int FamilyFirstCount { get; set; } = 2;
    public bool SingleUsesStandard { get; set; } = true;
    public int FeePlanId { get; set; }
    [Range(typeof(decimal),"0.01","999999.99")] public decimal Amount { get; set; }
    [Required] public string EffectiveMonth { get; set; } = TariffService.NextMonth.ToString("yyyy-MM");
    [Required, StringLength(200)] public string Reason { get; set; } = "";
    public string? PlanName { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(IsFamily && (!FamilyAdditionalAmount.HasValue || decimal.Round(FamilyAdditionalAmount.Value,2)!=FamilyAdditionalAmount.Value)) yield return new("Shkruani çmimin për fëmijët shtesë me dy shifra dhjetore.");
        if(decimal.Round(Amount,2)!=Amount) yield return new("Çmimi lejon dy shifra dhjetore.");
        if(!TariffService.TryMonth(EffectiveMonth,out var month) || month<TariffService.NextMonth) yield return new("Ndryshimi duhet të fillojë nga muaji i ardhshëm ose më vonë.");
    }
}
public class AssignFeeModel : IValidatableObject
{
    public int StudentId { get; set; }
    [Range(1,int.MaxValue)] public int FeePlanId { get; set; } = 1;
    [Required] public string EffectiveMonth { get; set; } = TariffService.NextMonth.ToString("yyyy-MM");
    [Required, StringLength(200)] public string Reason { get; set; } = "";
    [StringLength(400)] public string? Notes { get; set; }
    public Student? Student { get; set; }
    public List<FeePlanOption> Plans { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(!TariffService.TryMonth(EffectiveMonth,out var month) || month<TariffService.NextMonth) yield return new("Zgjidhni muajin e ardhshëm ose një muaj pas tij.");
        if(FeePlanId==3 && string.IsNullOrWhiteSpace(Notes)) yield return new("Për lirim nga pagesa kërkohet edhe shënimi.");
    }
}
public record FeePlanOption(int Id,string Name,decimal Amount,bool IsWaiver,decimal? FamilyAdditionalAmount=null,int FamilyFirstCount=2,bool SingleUsesStandard=true,bool IsFamily=false,bool IsActive=true)
{
    public string PriceLabel => IsFamily ? $"{Amount:0.##} € për {FamilyFirstCount} të parët; {FamilyAdditionalAmount:0.##} € për të tjerët" : $"{Amount:0.##} € / muaj";
}
public record ResolvedFee(decimal Amount,string Name,bool IsWaiver,string? Reason,string? Notes);
public class FeePlanIndexModel
{
    public List<FeePlanOption> Plans { get; set; } = [];
    public List<FeePlanPrice> History { get; set; } = [];
}
