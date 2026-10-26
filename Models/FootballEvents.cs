using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace _2Korriku.Models;

public class FootballEvent
{
    public string Kind { get; set; } = "Goal";
    public int? PlayerId { get; set; }
    public int? AssistId { get; set; }
    [Range(0,300)] public int Minute { get; set; }
    [Range(0,20)] public int? SuspensionGames { get; set; }
    [StringLength(200)] public string? Decision { get; set; }
}
public class DisciplineRule
{
    public int Id { get; set; }
    public int TrainingTeamId { get; set; }
    public TrainingTeam TrainingTeam { get; set; } = null!;
    [MaxLength(20)] public string Kind { get; set; } = "Official";
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public int YellowThreshold { get; set; }
    public int YellowBan { get; set; } = 1;
    public int RedBan { get; set; } = 1;
    public int SecondYellowBan { get; set; } = 1;
    public bool CountDismissalYellows { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
    public string UpdatedById { get; set; } = "";
    public ApplicationUser UpdatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
}
public class DisciplineInput
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    public int TeamId { get; set; }
    public string Kind { get; set; } = "Official";
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    [Range(1,20)] public int YellowThreshold { get; set; }
    [Range(1,20)] public int YellowBan { get; set; } = 1;
    [Range(0,20)] public int RedBan { get; set; } = 1;
    [Range(0,20)] public int SecondYellowBan { get; set; } = 1;
    public bool CountDismissalYellows { get; set; }
    [ValidateNever] public string TeamName { get; set; } = "";
}
public record DisciplineAlert(int StudentId,string Name,int Remaining,bool NeedsDecision,int Yellows,int? Threshold)
{
    public string Message => NeedsDecision?"Karton i kuq · konfirmo masën disiplinore":Remaining>0?$"Mungon: {Remaining} ndeshje":$"{Yellows} të verdhë" + (Threshold.HasValue?$" / {Threshold} · afër pezullimit":" · rregulli ende pa konfirmuar");
}
public static class FootballLabels
{
    public static readonly Dictionary<string,string> Events = new(){["Goal"]="Gol",["PenaltyGoal"]="Gol nga penalltia",["OwnGoal"]="Autogol i yni",["OpponentGoal"]="Gol i kundërshtarit",["OpponentOwnGoal"]="Autogol i kundërshtarit",["Yellow"]="I verdhë",["SecondYellow"]="I dyti i verdhë → i kuq",["Red"]="I kuq direkt"};
    public static bool Dismissal(FootballEvent e)=>e.Kind is "Red" or "SecondYellow";
    public static bool Card(FootballEvent e)=>e.Kind is "Yellow" or "SecondYellow" or "Red";
}
