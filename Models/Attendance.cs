using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class TeamAttendance
{
    public int Id { get; set; }
    public int TrainingTeamId { get; set; }
    public TrainingTeam? TrainingTeam { get; set; }
    public DateOnly Date { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string SavedById { get; set; } = "";
    public ApplicationUser? SavedBy { get; set; }
    [StringLength(20)] public string CoachRole { get; set; } = "head";
    public List<AttendancePlayer> Players { get; set; } = [];
}
public class AttendancePlayer
{
    public int Id { get; set; }
    public int TeamAttendanceId { get; set; }
    public TeamAttendance? TeamAttendance { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    [StringLength(201)] public string StudentName { get; set; } = "";
    public bool Present { get; set; }
}
public class AttendanceChange
{
    public int Id { get; set; }
    public int TeamAttendanceId { get; set; }
    public TeamAttendance? TeamAttendance { get; set; }
    public string ActorId { get; set; } = "";
    public ApplicationUser? Actor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Snapshot { get; set; } = "";
}
public class AttendancePlayerInput
{
    [Range(1,int.MaxValue)] public int StudentId { get; set; }
    [Required(ErrorMessage="Shënoni Prezent ose Mungon për çdo lojtar.")] public bool? Present { get; set; }
    [ValidateNever] public string Name { get; set; } = "";
    [ValidateNever] public string? Notes { get; set; }
    [ValidateNever] public decimal Debt { get; set; }
}
public class AttendanceModel : IValidatableObject
{
    [Range(1,int.MaxValue)] public int TeamId { get; set; }
    [Required, RegularExpression("head|assistant")] public string CoachRole { get; set; } = "head";
    [Required] public DateOnly? Date { get; set; } = BillingClock.Today;
    public Guid Revision { get; set; }
    [Required] public string RosterToken { get; set; } = "";
    public List<AttendancePlayerInput> Players { get; set; } = [];
    [ValidateNever] public string TeamName { get; set; } = "";
    [ValidateNever] public string? FieldName { get; set; }
    [ValidateNever] public DateTime? SavedAt { get; set; }
    [ValidateNever] public DateTime? EditableUntil { get; set; }
    [ValidateNever] public bool IsLocked { get; set; }
    [ValidateNever] public string? SavedBy { get; set; }
    [ValidateNever] public List<TeamAttendance> History { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(Date.HasValue && (Date<new DateOnly(1900,1,1)||Date>BillingClock.Today)) yield return new("Zgjidhni datë deri në ditën e sotme.");
        if(Players == null || Players.Count is <1 or >1000) yield return new("Lista duhet të ketë nga 1 deri në 1000 lojtarë.");
        else if(Players.Any(x=>x==null) || Players.Select(x=>x.StudentId).Distinct().Count()!=Players.Count) yield return new("Lista përmban lojtarë të pavlefshëm ose të përsëritur. Rihapeni.");
    }
}
