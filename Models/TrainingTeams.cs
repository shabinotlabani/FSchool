using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class TrainingTeam
{
    public int? FootballFieldId { get; set; }
    public FootballField? FootballField { get; set; }
    public int Id { get; set; }
    [Required,StringLength(100)] public string Name { get; set; } = "";
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public ICollection<TrainingSession> Sessions { get; set; } = new List<TrainingSession>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
public class TrainingSession
{
    public int Id { get; set; }
    public int TrainingTeamId { get; set; }
    public TrainingTeam? TrainingTeam { get; set; }
    public int Day { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
}
public class TeamChange
{
    public int Id { get; set; }
    public int TrainingTeamId { get; set; }
    public TrainingTeam? TrainingTeam { get; set; }
    [StringLength(2000)] public string Snapshot { get; set; } = "";
    [StringLength(200)] public string Reason { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class StudentTeamChange
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int? FromTeamId { get; set; }
    public int? ToTeamId { get; set; }
    [StringLength(100)] public string? FromTeamName { get; set; }
    [StringLength(100)] public string? ToTeamName { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class TrainingSessionInput
{
    [Range(1,7)] public int Day { get; set; } = 1;
    [Required(ErrorMessage="Vendosni orën e fillimit.")] public TimeOnly? StartsAt { get; set; }
    [Required(ErrorMessage="Vendosni orën e përfundimit.")] public TimeOnly? EndsAt { get; set; }
}
public class TeamEditModel : IValidatableObject
{
    [Range(1, int.MaxValue)] public int? FootballFieldId { get; set; }
    public List<FootballField> Fields { get; set; } = [];
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(100)] public string Name { get; set; } = "";
    [Range(0,100)] public int MinAge { get; set; } = 6;
    [Range(0,100)] public int MaxAge { get; set; } = 8;
    [Range(1,1000)] public int Capacity { get; set; } = 20;
    public bool IsActive { get; set; } = true;
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    [StringLength(200)] public string? Reason { get; set; }
    public List<TrainingSessionInput> Sessions { get; set; } = [new(){Day=1},new(){Day=2},new(){Day=4}];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(MinAge>MaxAge)yield return new("Mosha minimale nuk mund të jetë mbi moshën maksimale.");
        if(Id!=0&&string.IsNullOrWhiteSpace(Reason))yield return new("Shkruani arsyen e ndryshimit.");
        if(Sessions==null||Sessions.Count is <1 or >14){yield return new("Vendosni nga 1 deri në 14 seanca javore.");yield break;}
        if(Sessions.Any(s=>s.StartsAt.HasValue&&s.EndsAt.HasValue&&(s.StartsAt>=s.EndsAt || s.StartsAt.Value.Ticks%TimeSpan.TicksPerMinute!=0 || s.EndsAt.Value.Ticks%TimeSpan.TicksPerMinute!=0)))yield return new("Ora e përfundimit duhet të jetë pas fillimit, brenda së njëjtës ditë dhe me minuta të plota.");
        foreach(var day in Sessions.GroupBy(s=>s.Day))
        {
            var rows=day.Where(s=>s.StartsAt.HasValue&&s.EndsAt.HasValue).OrderBy(s=>s.StartsAt).ToList();
            if(rows.Zip(rows.Skip(1)).Any(p=>p.First.EndsAt>p.Second.StartsAt))yield return new("Seancat e së njëjtës ditë nuk mund të mbivendosen.");
        }
    }
}
public class TeamOption
{
    public TrainingTeam Team { get; set; } = new();
    public int Enrolled { get; set; }
    public int FreePlaces => Math.Max(0,Team.Capacity-Enrolled);
}
public class TeamDetailsModel
{
    public TrainingTeam Team { get; set; } = new();
    public List<TeamChange> History { get; set; } = [];
    public List<StudentTeamChange> MembershipHistory { get; set; } = [];
}
public class ChangeStudentTeamModel
{
    public int StudentId { get; set; }
    public int? TeamId { get; set; }
    public int? ExpectedTeamId { get; set; }
    [Required,StringLength(200)] public string Reason { get; set; } = "";
    public Student? Student { get; set; }
    public List<TeamOption> Options { get; set; } = [];
}
