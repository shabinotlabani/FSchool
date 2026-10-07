using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class PersonalTariff
{
    public int Id { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    [StringLength(100)] public string Name { get; set; } = "";
    [StringLength(20)] public string Mode { get; set; } = "Session";
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
    [StringLength(400)] public string? Notes { get; set; }
    [NotMapped] public string ModeLabel => Mode == "Monthly" ? "Paketë mujore" : "Për seancë";
}

public class PersonalTraining
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [StringLength(64)] public string PayloadHash { get; set; } = "";
    public Guid Revision { get; set; } = Guid.NewGuid();
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int PersonalTariffId { get; set; }
    public PersonalTariff? PersonalTariff { get; set; }
    [StringLength(100)] public string Name { get; set; } = "";
    [StringLength(20)] public string Mode { get; set; } = "Session";
    [Column(TypeName="numeric(18,2)")] public decimal Rate { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public bool IsCancelled { get; set; }
    [StringLength(150)] public string? Coach { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PersonalTrainingSlot> Sessions { get; set; } = [];
    public List<PersonalCharge> Charges { get; set; } = [];
    [NotMapped] public string ModeLabel => Mode == "Monthly" ? "Paketë mujore" : "Për seancë";
    [NotMapped] public string Schedule => string.Join("; ",Sessions.OrderBy(s=>s.Day).ThenBy(s=>s.StartsAt).Select(s=>$"{(Mode=="Monthly"?TeamService.DayName(s.Day):StartsOn.ToString("dd.MM.yyyy"))} {s.StartsAt:HH:mm}–{s.EndsAt:HH:mm}"));
}

public class PersonalTrainingSlot
{
    public int Id { get; set; }
    public int PersonalTrainingId { get; set; }
    public PersonalTraining? PersonalTraining { get; set; }
    public int Day { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
}

public class PersonalCharge
{
    public int Id { get; set; }
    public int PersonalTrainingId { get; set; }
    public PersonalTraining? PersonalTraining { get; set; }
    public DateOnly Period { get; set; }
    [StringLength(200)] public string Description { get; set; } = "";
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Paid { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Waived { get; set; }
    [StringLength(400)] public string? Calculation { get; set; }
    [StringLength(200)] public string? WaiverReason { get; set; }
    [StringLength(400)] public string? WaiverNotes { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Payment> Payments { get; set; } = [];
    [NotMapped] public decimal Due => IsCancelled ? 0 : Math.Max(0,Amount-Paid-Waived);
    [NotMapped] public string Number => $"TP-{Period:yyyyMM}-{Id:000000}";
    [NotMapped] public string Status => IsCancelled ? "Anuluar" : Waived > 0 ? "E liruar" : Due == 0 ? "E paguar" : Paid > 0 ? "Pjesërisht e paguar" : "E papaguar";
}

public class PersonalTrainingAudit
{
    public int Id { get; set; }
    public int? PersonalTrainingId { get; set; }
    public PersonalTraining? PersonalTraining { get; set; }
    public int? PersonalTariffId { get; set; }
    public PersonalTariff? PersonalTariff { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public string Snapshot { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PersonalTariffModel : IValidatableObject
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public List<PersonalTrainingAudit> History { get; set; } = [];
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(100)] public string Name { get; set; } = "";
    [Required,RegularExpression("Session|Monthly")] public string Mode { get; set; } = "Session";
    [Range(typeof(decimal),"0.01","999999.99")] public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
    [StringLength(400)] public string? Notes { get; set; }
    [StringLength(200)] public string? Reason { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(decimal.Round(Amount,2)!=Amount)yield return new("Çmimi lejon deri në dy shifra dhjetore.");
        if(Id!=0&&string.IsNullOrWhiteSpace(Reason))yield return new("Shkruani arsyen e ndryshimit.");
    }
}

public class PersonalTrainingCreateModel : IValidatableObject
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(1,int.MaxValue,ErrorMessage="Zgjidhni lojtarin.")] public int StudentId { get; set; }
    [Range(1,int.MaxValue,ErrorMessage="Zgjidhni tarifën.")] public int PersonalTariffId { get; set; }
    public decimal ExpectedRate { get; set; }
    [Required,DataType(DataType.Date)] public DateOnly? StartsOn { get; set; } = BillingClock.Today;
    [StringLength(150)] public string? Coach { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
    public List<TrainingSessionInput> Sessions { get; set; } = [new(){Day=1}];
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public List<Student> Students { get; set; } = [];
    public List<PersonalTariff> Tariffs { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(RequestId==Guid.Empty)yield return new("Rihapni formularin.");
        if(StartsOn.HasValue&&(StartsOn<TariffService.CurrentMonth||StartsOn>BillingClock.Today.AddYears(2)))yield return new("Fillimi lejohet nga muaji aktual deri në dy vjet më vonë.");
        if(Sessions==null||Sessions.Count is <1 or >14){yield return new("Vendosni 1–14 orare.");yield break;}
        if(Sessions.Any(s=>s.StartsAt.HasValue&&s.EndsAt.HasValue&&(s.StartsAt>=s.EndsAt||s.StartsAt.Value.Ticks%TimeSpan.TicksPerMinute!=0||s.EndsAt.Value.Ticks%TimeSpan.TicksPerMinute!=0)))yield return new("Përfundimi duhet të jetë pas fillimit, brenda ditës dhe me minuta të plota.");
    }
}

public class PersonalPaymentModel : IValidatableObject
{
    public int ChargeId { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(typeof(decimal),"0.01","999999999")] public decimal Amount { get; set; }
    [Required,RegularExpression("Cash|Bank|Other")] public string PaymentMethod { get; set; } = "Cash";
    [StringLength(250)] public string? Notes { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public PersonalCharge? Charge { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(RequestId==Guid.Empty||decimal.Round(Amount,2)!=Amount)yield return new("Kontrolloni shumën dhe rihapni formularin nëse është e nevojshme.");
    }
}

public class PersonalActionModel
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    public string? EndMonth { get; set; }
    [Required,StringLength(200)] public string Reason { get; set; } = "";
    [StringLength(400)] public string? Notes { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public PersonalTraining? Training { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public PersonalCharge? Charge { get; set; }
}

public class PersonalTrainingIndexModel
{
    public string Month { get; set; } = TariffService.CurrentMonth.ToString("yyyy-MM");
    public string? Search { get; set; }
    public bool UnpaidOnly { get; set; }
    public List<PersonalTraining> Trainings { get; set; } = [];
    public List<PersonalCharge> Charges { get; set; } = [];
}

public class PersonalTrainingDetailsModel
{
    public PersonalTraining Training { get; set; } = new();
    public List<PersonalTrainingAudit> History { get; set; } = [];
}
