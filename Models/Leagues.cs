using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;
namespace _2Korriku.Models;

public class League
{
    public int Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Kind { get; set; } = "Official";
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RulesConfirmed { get; set; }
    public int YellowThreshold { get; set; }
    public int YellowBan { get; set; }
    public int RedBan { get; set; }
    public int SecondYellowBan { get; set; }
    public bool CountDismissalYellows { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; }
    public string UpdatedById { get; set; } = "";
    public ApplicationUser UpdatedBy { get; set; } = null!;
    public List<LeagueTeam> Teams { get; set; } = [];
    public DisciplineRule? Rule => !RulesConfirmed?null:new(){StartsOn=StartsOn,EndsOn=EndsOn,YellowThreshold=YellowThreshold,YellowBan=YellowBan,RedBan=RedBan,SecondYellowBan=SecondYellowBan,CountDismissalYellows=CountDismissalYellows};
}
public class LeagueTeam
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int TrainingTeamId { get; set; }
    public TrainingTeam TrainingTeam { get; set; } = null!;
}
public class LeagueInput
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(150)] public string Name { get; set; } = "";
    [RegularExpression("Official|Cup")] public string Kind { get; set; } = "Official";
    public DateOnly StartsOn { get; set; } = new(BillingClock.Today.Month>=7?BillingClock.Today.Year:BillingClock.Today.Year-1,7,1);
    public DateOnly EndsOn { get; set; } = new(BillingClock.Today.Month>=7?BillingClock.Today.Year+1:BillingClock.Today.Year,6,30);
    public bool IsActive { get; set; } = true;
    public bool RulesConfirmed { get; set; }
    [Range(1,20)] public int? YellowThreshold { get; set; }
    [Range(1,20)] public int? YellowBan { get; set; }
    [Range(0,20)] public int? RedBan { get; set; }
    [Range(0,20)] public int? SecondYellowBan { get; set; }
    public bool CountDismissalYellows { get; set; }
    public List<int> TeamIds { get; set; } = [];
    [ValidateNever] public List<TrainingTeam> Teams { get; set; } = [];
}
