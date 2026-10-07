using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class FamilyChildInput : IValidatableObject
{
    [Range(typeof(decimal),"0","999999999.99")] public decimal? FirstMonthAmount { get; set; }
    [StringLength(200)] public string? FirstMonthReason { get; set; }
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Required, DataType(DataType.Date)] public DateOnly? DateOfBirth { get; set; }
    public int? TrainingTeamId { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
    public List<TeamOption> Teams { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(FirstMonthAmount.HasValue && (decimal.Round(FirstMonthAmount.Value,2)!=FirstMonthAmount.Value || string.IsNullOrWhiteSpace(FirstMonthReason)))yield return new("Për shumën e caktuar të muajit të parë shkruani arsyen dhe përdorni deri në dy shifra dhjetore.");
        if(DateOfBirth.HasValue && (DateOfBirth.Value==DateOnly.MinValue || DateOfBirth>BillingClock.Today))
            yield return new("Datëlindja duhet të jetë e vlefshme dhe jo në të ardhmen.");
    }
}
public class FamilyRegistrationModel : IValidatableObject
{
    [Range(1,int.MaxValue)] public int FeePlanId { get; set; } = 2;
    public List<FeePlanOption> Plans { get; set; } = [];
    public Dictionary<int,int> FamilyPlanIds { get; set; } = [];
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public int? FamilyId { get; set; }
    [StringLength(150)] public string? FamilyName { get; set; }
    [StringLength(150)] public string? ParentName { get; set; }
    [Phone, StringLength(50)] public string? ParentPhone { get; set; }
    [EmailAddress, StringLength(254)] public string? ParentEmail { get; set; }
    public List<FamilyChildInput> Children { get; set; } = [new(),new()];
    public List<PlayerFamily> Families { get; set; } = [];
    public FeePlanOption? Price { get; set; }
    public Dictionary<int,int> FamilyCounts { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(RequestId==Guid.Empty)yield return new("Rihapni formularin e regjistrimit.");
        if(FamilyId.HasValue && FamilyId<=0)yield return new("Zgjidhni një familje të vlefshme.");
        if(!FamilyId.HasValue && string.IsNullOrWhiteSpace(FamilyName))yield return new("Shkruani emrin e familjes së re.");
        if(Children==null || Children.Count is <2 or >20)yield return new("Regjistroni nga 2 deri në 20 fëmijë së bashku.");
    }
}
public class FamilyRegistration
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [Required, StringLength(64)] public string PayloadHash { get; set; } = "";
    public int FamilyId { get; set; }
    public PlayerFamily? Family { get; set; }
    public string ActorId { get; set; } = "";
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class StudentEditModel : FamilyChildInput
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [StringLength(150)] public string? ParentName { get; set; }
    [Phone, StringLength(50)] public string? ParentPhone { get; set; }
    [EmailAddress, StringLength(254)] public string? ParentEmail { get; set; }
    public bool IsActive { get; set; }
    [Required, StringLength(200)] public string Reason { get; set; } = "";
    public string? CurrentTeamName { get; set; }
    public List<StudentChange> History { get; set; } = [];
}
public class StudentChange
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Summary { get; set; } = "";
    public string ActorId { get; set; } = "";
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
