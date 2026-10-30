using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class StudentRegistrationModel : IValidatableObject
{
    public bool IsContractPlayer { get; set; }
    [Required,RegularExpression("Male|Female")] public string Gender { get; set; } = "Male";
    [Required, RegularExpression("Monthly|Seasonal")] public string RegistrationPayment { get; set; } = "Monthly";
    public Guid SeasonalRequestId { get; set; } = Guid.NewGuid();
    public int SeasonalMonths { get; set; } = 6;
    public decimal SeasonalExpectedRate { get; set; }
    [Required, RegularExpression("Cash|Bank")] public string SeasonalPaymentMethod { get; set; } = "Cash";
    [Required, RegularExpression("Cash|Bank"), StringLength(10)] public string PreferredPaymentMethod { get; set; } = "Cash";
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
    [Phone(ErrorMessage = "Shkruani një numër telefoni të vlefshëm."), StringLength(50), Display(Name = "Telefoni i lojtarit (opsional)")]
    public string? Phone { get; set; }
    public int FeePlanId { get; set; }
    public int? FamilyId { get; set; }
    public Dictionary<int,int> FamilyCounts { get; set; } = [];
    public Dictionary<int,int> FamilyPlanIds { get; set; } = [];
    public decimal StandardPrice { get; set; }
    public List<PlayerFamily> Families { get; set; } = [];
    [StringLength(200)] public string? FeeReason { get; set; }
    [StringLength(400)] public string? FeeNotes { get; set; }
    public List<FeePlanOption> FeePlans { get; set; } = [];
    [Required(ErrorMessage = "Zgjidhni ekipin e lojtarit.")]
    public int? TrainingTeamId { get; set; }
    public List<TeamOption> SuggestedTeams { get; set; } = [];
    [StringLength(2000, ErrorMessage = "Shënimet mund të kenë deri në 2000 karaktere.")]
    [Display(Name = "Shënime")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(string.IsNullOrWhiteSpace(Phone)&&string.IsNullOrWhiteSpace(ParentPhone))yield return new("Shkruani telefonin e lojtarit ose telefonin e prindit / kujdestarit.",[nameof(Phone),nameof(ParentPhone)]);
        if(string.IsNullOrWhiteSpace(Phone)&&string.IsNullOrWhiteSpace(ParentName))yield return new("Shkruani emrin e prindit / kujdestarit kur lojtari nuk ka telefon.",[nameof(ParentName)]);
        if(!IsContractPlayer&&FeePlanId<1)yield return new("Zgjidhni kategorinë e tarifës.",[nameof(FeePlanId)]);
        if(!IsContractPlayer&&RegistrationPayment=="Seasonal"){
            if(SeasonalMonths is not (6 or 12))yield return new("Zgjidhni sezonin 6 ose 12 muaj.");
            if(SeasonalRequestId==Guid.Empty)yield return new("Rihapni formularin e regjistrimit.");
            if(FirstMonthAmount.HasValue)yield return new("Pagesa sezonale nuk përdor shumën e muajit të parë.");
        }
        if(!IsContractPlayer&&FirstMonthAmount.HasValue && (decimal.Round(FirstMonthAmount.Value,2)!=FirstMonthAmount.Value || string.IsNullOrWhiteSpace(FirstMonthReason)))yield return new("Për shumën e caktuar të muajit të parë shkruani arsyen dhe përdorni deri në dy shifra dhjetore.");
        if (DateOfBirth.HasValue && (DateOfBirth.Value == DateOnly.MinValue || DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today)))
            yield return new ValidationResult("Datëlindja duhet të jetë një datë e vlefshme, jo në të ardhmen.", [nameof(DateOfBirth)]);
    }
}
