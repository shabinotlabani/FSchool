using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class CreateTariffModel : IValidatableObject
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public bool IsFamily { get; set; }
    [Range(typeof(decimal), "0.01", "999999.99")] public decimal Amount { get; set; }
    [Range(1, 20)] public int FamilyFirstCount { get; set; } = 2;
    [Range(typeof(decimal), "0.01", "999999.99")] public decimal? FamilyAdditionalAmount { get; set; }
    public bool SingleUsesStandard { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (decimal.Round(Amount, 2) != Amount) yield return new("Çmimi lejon dy shifra dhjetore.");
        if (IsFamily && (!FamilyAdditionalAmount.HasValue || decimal.Round(FamilyAdditionalAmount.Value, 2) != FamilyAdditionalAmount.Value))
            yield return new("Shkruani çmimin për fëmijët shtesë me dy shifra dhjetore.");
    }
}

public class FamilyTariffAssignment
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public PlayerFamily? Family { get; set; }
    public int FeePlanId { get; set; }
    public FeePlan? FeePlan { get; set; }
    public DateOnly EffectiveMonth { get; set; } = TariffService.CurrentMonth;
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
