using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class PlayerEvaluation
{
    [MaxLength(30)] public string Sport { get; set; } = "Football";
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(450)] public string CoachId { get; set; } = "";
    public ApplicationUser Coach { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int Technique { get; set; }
    public int Tactics { get; set; }
    public int Fitness { get; set; }
    public int Discipline { get; set; }
    public int Teamwork { get; set; }
    [MaxLength(2000)] public string? Strengths { get; set; }
    [MaxLength(2000)] public string? Improvements { get; set; }
    [MaxLength(2000)] public string Goals { get; set; } = "";
    public DateOnly? ReviewDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
    public decimal Average => (Technique + Tactics + Fitness + Discipline + Teamwork) / 5m;
}

public class EvaluationInput
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Guid Revision { get; set; }
    public DateOnly Date { get; set; } = BillingClock.Today;
    [Range(1,5)] public int Technique { get; set; } = 3;
    [Range(1,5)] public int Tactics { get; set; } = 3;
    [Range(1,5)] public int Fitness { get; set; } = 3;
    [Range(1,5)] public int Discipline { get; set; } = 3;
    [Range(1,5)] public int Teamwork { get; set; } = 3;
    [MaxLength(2000)] public string? Strengths { get; set; }
    [MaxLength(2000)] public string? Improvements { get; set; }
    [Required, MaxLength(2000)] public string Goals { get; set; } = "";
    public DateOnly? ReviewDate { get; set; }
}
public record DevelopmentRoster(List<Student> Players, List<PlayerEvaluation> Latest);
public record DevelopmentHistory(Student Player, List<PlayerEvaluation> Evaluations, string CoachId)
{
    public SportCatalog Catalog { get; init; } = new("Football",DevelopmentSections.All.ToList());
    public List<DevelopmentSection> Sections=>Catalog.Sections;
    public string[] Positions=>Catalog.Positions;
    public List<DevelopmentRecord> Records { get; init; } = [];
    public int? Year { get; init; }
    public string Section { get; init; } = "Assessment";
    public List<int> Years { get; init; } = [];
    private string? ProfileValue(string key)=>Records.Where(x=>x.Sport==Catalog.Sport&&x.Kind=="Profile"&&x.Values.ContainsKey(key)).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).FirstOrDefault()?.Values.GetValueOrDefault(key);
    public string? PlayerRole => DevelopmentSections.RoleLabel(ProfileValue("PlayerRole"));
    public string? PrimaryPosition => ProfileValue("PrimaryPosition");
    public string? SecondaryPosition => ProfileValue("SecondaryPosition");
    public bool IsGoalkeeper => Catalog.Find("Goalkeeping")!=null&&PlayerRole=="Portier";
}
