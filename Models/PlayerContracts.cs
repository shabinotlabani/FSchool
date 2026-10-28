using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;
namespace _2Korriku.Models;
public class PlayerContract
{
 public int Id { get; set; }
 public int? StudentId { get; set; }
 public Student Student { get; set; } = null!;
 public int? BeneficiaryId { get; set; }
 public SponsorshipBeneficiary? Beneficiary { get; set; }
 [MaxLength(80)] public string Number { get; set; } = "";
 public DateOnly StartsOn { get; set; }
 public DateOnly EndsOn { get; set; }
 public DateOnly? EndedOn { get; set; }
 [MaxLength(20)] public string Status { get; set; } = "Draft";
 [Column(TypeName="numeric(18,2)")] public decimal GrossSalary { get; set; }
 public string TermsData { get; set; } = "{}";
 public byte[]? SignedPdf { get; set; }
 [MaxLength(200)] public string? FileName { get; set; }
 public Guid RequestId { get; set; } = Guid.NewGuid();
 [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
 public DateTime UpdatedAt { get; set; }
 public string ActorId { get; set; } = "";
 public ApplicationUser Actor { get; set; } = null!;
 public List<PlayerContractAnnex> Annexes { get; set; } = [];
 [MaxLength(500)] public string? EndReason { get; set; }
 [NotMapped] public ContractTerms Terms => System.Text.Json.JsonSerializer.Deserialize<ContractTerms>(TermsData)??new();
 [NotMapped] public DateOnly EffectiveEnd => EndedOn??EndsOn;
 [NotMapped] public string PersonName => StudentId.HasValue ? Student.FullName : Beneficiary?.Name ?? "";
 [NotMapped] public string PersonKey => StudentId.HasValue ? "P"+StudentId : "S"+BeneficiaryId;
 [NotMapped] public string SalaryRole => StudentId.HasValue || Beneficiary?.StaffCategory=="ContractPlayer" ? "Player" : "Staff";
 [NotMapped] public string CategoryName => Beneficiary?.StaffCategory switch {"ContractPlayer"=>"Lojtar me kontratë","Management"=>"Stafi drejtues","Technical"=>"Stafi teknik","Medical"=>"Stafi mjekësor","Performance"=>"Performanca dhe nutricioni","Administration"=>"Administrata","Operations"=>"Logjistika dhe operacionet",_=>StudentId.HasValue?"Lojtar me kontratë":"Tjetër"};
 public decimal SalaryOn(DateOnly date)=>Annexes.Where(a=>a.Status=="Active"&&a.EffectiveOn<=date).OrderBy(a=>a.EffectiveOn).ThenBy(a=>a.Id).Select(a=>(decimal?)a.NewGrossSalary).LastOrDefault()??GrossSalary;
}
public class PlayerContractAnnex
{
 public int Id { get; set; }
 public int PlayerContractId { get; set; }
 public PlayerContract PlayerContract { get; set; } = null!;
 [Required,MaxLength(80)] public string Number { get; set; } = "";
 public DateOnly EffectiveOn { get; set; }
 [Column(TypeName="numeric(18,2)")] public decimal PreviousGrossSalary { get; set; }
 [Column(TypeName="numeric(18,2)")] public decimal NewGrossSalary { get; set; }
 [Required,MaxLength(500)] public string Reason { get; set; } = "";
 [Required,MaxLength(20)] public string Status { get; set; } = "Draft";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 [Required,MaxLength(450)] public string ActorId { get; set; } = "";
 public ApplicationUser Actor { get; set; } = null!;
 [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
}
public class PlayerContractAnnexInput
{
 public int Id { get; set; }
 public int PlayerContractId { get; set; }
 public Guid Revision { get; set; }
 [StringLength(80)] public string? Number { get; set; }
 [Required] public DateOnly? EffectiveOn { get; set; }
 [Range(typeof(decimal),"0.01","99999999.99")] public decimal NewGrossSalary { get; set; }
 [Required,StringLength(500)] public string Reason { get; set; } = "";
}
public class ContractTerms
{
 [Required,StringLength(200)] public string EmployerName { get; set; } = "";
 [Required,StringLength(60)] public string EmployerNumber { get; set; } = "";
 [Required,StringLength(250)] public string EmployerAddress { get; set; } = "";
 [Required,StringLength(150)] public string Representative { get; set; } = "";
 [StringLength(60)] public string? RepresentativePersonalNumber { get; set; }
 [Required,StringLength(60)] public string PersonalNumber { get; set; } = "";
 [Required,StringLength(250)] public string Address { get; set; } = "";
 [Required,StringLength(150)] public string Qualification { get; set; } = "";
 [Required,StringLength(120)] public string Nationality { get; set; } = "Kosovë";
 [Required,StringLength(150)] public string Position { get; set; } = "Lojtar";
 [Required,StringLength(3000)] public string Duties { get; set; } = "";
 [Required,StringLength(250)] public string Workplace { get; set; } = "";
 [Range(1,60)] public int WeeklyHours { get; set; } = 40;
 [Required,StringLength(500)] public string Schedule { get; set; } = "";
 [Required,StringLength(500)] public string Leave { get; set; } = "";
 [Required,StringLength(2000)] public string Termination { get; set; } = "";
 [StringLength(2000)] public string? OtherTerms { get; set; }
 [Required,StringLength(60)] public string BankAccount { get; set; } = "";
 [Required,RegularExpression("Primary|Secondary")] public string EmployerType { get; set; } = "Primary";
 [Range(typeof(decimal),"0","15")] public decimal EmployeePensionPercent { get; set; } = 5;
 [Range(typeof(decimal),"0","15")] public decimal EmployerPensionPercent { get; set; } = 5;
 [StringLength(300)] public string? PensionException { get; set; }
 [StringLength(150)] public string? GuardianName { get; set; }
 public bool GuardianConsent { get; set; }
 public DateOnly? AtkRegisteredOn { get; set; }
 [StringLength(100)] public string? AtkReference { get; set; }
}
public class PlayerContractInput
{
 [ValidateNever] public string? PersonKey { get; set; }
 public bool Simple { get; set; }
 public int Id { get; set; }
 public Guid Revision { get; set; }
 public Guid RequestId { get; set; } = Guid.NewGuid();
 public int? StudentId { get; set; }
 [RegularExpression("Player|Staff")] public string PersonType { get; set; } = "Player";
 public int? BeneficiaryId { get; set; }
 [StringLength(80)] public string Number { get; set; } = "";
 public DateOnly StartsOn { get; set; } = BillingClock.Today;
 public DateOnly EndsOn { get; set; } = BillingClock.Today.AddYears(1).AddDays(-1);
 [Range(typeof(decimal),"0.01","99999999.99")] public decimal GrossSalary { get; set; }
 [ValidateNever] public ContractTerms Terms { get; set; } = new();
 [ValidateNever] public IFormFile? Document { get; set; }
 [ValidateNever] public List<Student> Students { get; set; } = [];
 [ValidateNever] public List<SponsorshipBeneficiary> Beneficiaries { get; set; } = [];
 [ValidateNever] public bool HasDocument { get; set; }
}
public class PlayerContractAudit
{
 public int Id { get; set; }
 public int PlayerContractId { get; set; }
 public PlayerContract PlayerContract { get; set; } = null!;
 public DateTime At { get; set; } = DateTime.UtcNow;
 [MaxLength(450)] public string ActorId { get; set; } = "";
 [MaxLength(500)] public string Action { get; set; } = "";
 public string Snapshot { get; set; } = "";
}
public record ContractPayroll(decimal Gross,decimal EmployeePension,decimal Tax,decimal Net,decimal EmployerPension)
{
 public decimal Cost => Gross+EmployerPension;
}
public class ContractStaffInput
{
 public int? TeamId { get; set; }
 [Required,StringLength(150)] public string Name { get; set; } = "";
 [Required,StringLength(60)] public string Identifier { get; set; } = "";
 [Required] public DateOnly? BirthDate { get; set; }
 [Required,RegularExpression("ContractPlayer|Management|Technical|Medical|Performance|Administration|Operations|Other")] public string Category { get; set; } = "ContractPlayer";
}
