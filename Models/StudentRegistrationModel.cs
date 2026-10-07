using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class StudentRegistrationModel : IValidatableObject
{
    [Range(typeof(decimal),"0","999999999.99")] public decimal? FirstMonthAmount { get; set; }
    [StringLength(200)] public string? FirstMonthReason { get; set; }
    [Required(ErrorMessage = "Shkruani emrin e lojtarit."), StringLength(100)]
    [Display(Name = "Emri")]
    public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Shkruani mbiemrin e lojtarit."), StringLength(100)]
    [Display(Name = "Mbiemri")]
    public string LastName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Zgjidhni datëlindjen."), DataType(DataType.Date)]
    [Display(Name = "Datëlindja")]
    public DateOnly? DateOfBirth { get; set; }
    [StringLength(150), Display(Name = "Emri i prindit / kujdestarit")]
    public string? ParentName { get; set; }
    [EmailAddress(ErrorMessage = "Shkruani një email të vlefshëm."), Display(Name = "Emaili i prindit")]
    public string? ParentEmail { get; set; }
    [Phone(ErrorMessage = "Shkruani një numër telefoni të vlefshëm."), Display(Name = "Telefoni i prindit")]
    public string? ParentPhone { get; set; }
    [Range(1,int.MaxValue,ErrorMessage="Zgjidhni kategorinë e tarifës.")]
    public int FeePlanId { get; set; } = 1;
    public int? FamilyId { get; set; }
    public Dictionary<int,int> FamilyCounts { get; set; } = [];
    public Dictionary<int,int> FamilyPlanIds { get; set; } = [];
    public decimal StandardPrice { get; set; }
    public List<PlayerFamily> Families { get; set; } = [];
    [StringLength(200)] public string? FeeReason { get; set; }
    [StringLength(400)] public string? FeeNotes { get; set; }
    public List<FeePlanOption> FeePlans { get; set; } = [];
    public int? TrainingTeamId { get; set; }
    public List<TeamOption> SuggestedTeams { get; set; } = [];
    [StringLength(2000, ErrorMessage = "Shënimet mund të kenë deri në 2000 karaktere.")]
    [Display(Name = "Shënime")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(FirstMonthAmount.HasValue && (decimal.Round(FirstMonthAmount.Value,2)!=FirstMonthAmount.Value || string.IsNullOrWhiteSpace(FirstMonthReason)))yield return new("Për shumën e caktuar të muajit të parë shkruani arsyen dhe përdorni deri në dy shifra dhjetore.");
        if (DateOfBirth.HasValue && (DateOfBirth.Value == DateOnly.MinValue || DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today)))
            yield return new ValidationResult("Datëlindja duhet të jetë një datë e vlefshme, jo në të ardhmen.", [nameof(DateOfBirth)]);
        if(FeePlanId==2 && (!FamilyId.HasValue || FamilyId<=0)) yield return new ValidationResult("Zgjidhni familjen për paketën familjare.");
        if(FeePlanId==3 && (string.IsNullOrWhiteSpace(FeeReason) || string.IsNullOrWhiteSpace(FeeNotes)))
            yield return new ValidationResult("Për lirim nga pagesa kërkohen arsyeja dhe shënimi.");
    }
}
