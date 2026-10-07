using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class PlayerFamily
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(100)] public string? Phone { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}
public class FamilyChange
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public PlayerFamily? Family { get; set; }
    public DateOnly EffectiveMonth { get; set; }
    [StringLength(10000)] public string Snapshot { get; set; } = "";
    [StringLength(200)] public string Reason { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class FamilyEditModel : IValidatableObject
{
    [Range(1,int.MaxValue)] public int FeePlanId { get; set; } = 2;
    public List<FeePlanOption> Plans { get; set; } = [];
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(100)] public string? Phone { get; set; }
    [Required] public string EffectiveMonth { get; set; } = TariffService.NextMonth.ToString("yyyy-MM");
    [Required, StringLength(200)] public string Reason { get; set; } = "";
    public List<int> StudentIds { get; set; } = [];
    public List<Student> Students { get; set; } = [];
    public FeePlanOption? FamilyPrice { get; set; }
    public decimal StandardPrice { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!TariffService.TryMonth(EffectiveMonth, out var month) || month < TariffService.NextMonth)
            yield return new("Ndryshimet e familjes fillojnë nga muaji i ardhshëm ose më vonë.");
        if (StudentIds.Count > 20 || StudentIds.Any(id=>id<=0) || StudentIds.Distinct().Count()!=StudentIds.Count)
            yield return new("Zgjidhni deri në 20 fëmijë të ndryshëm.");
    }
}
public record FamilyMemberFee(Student Student, int Position, ResolvedFee Fee);
public class FamilyDetailsModel
{
    public PlayerFamily Family { get; set; } = new();
    public DateOnly Month { get; set; }
    public List<FamilyMemberFee> Members { get; set; } = [];
    public List<FamilyChange> History { get; set; } = [];
}
