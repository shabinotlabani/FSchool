using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;
namespace _2Korriku.Models;

public class League
{
    public int Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Kind { get; set; } = "Official";
    [MaxLength(60)] public string Country { get; set; } = "Kosovë";
    [MaxLength(100)] public string Organizer { get; set; } = "FFK";
    [MaxLength(30)] public string Sport { get; set; } = "Football";
    [MaxLength(10)] public string Gender { get; set; } = "Male";
    [MaxLength(20)] public string Season { get; set; } = "";
    [MaxLength(30)] public string AgeCategory { get; set; } = "Senior";
    [MaxLength(20)] public string CompetitionScope { get; set; } = "Domestic";
    [MaxLength(20)] public string CompetitionType { get; set; } = "League";
    [MaxLength(100)] public string? Division { get; set; }
    [MaxLength(100)] public string? Group { get; set; }
    [MaxLength(30)] public string Stage { get; set; } = "LeaguePhase";
    public bool IsOfficial { get; set; }
    public bool IsVerified { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool DatesSuggestedByAi { get; set; }
    public DateTime? DatesCheckedAt { get; set; }
    [MaxLength(500)] public string? ScheduleSourceUrl { get; set; }
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
    [MaxLength(20)] public string Status { get; set; } = "Registered";
    public DateOnly RegisteredOn { get; set; } = BillingClock.Today;
    [MaxLength(30)] public string? Stage { get; set; }
    [MaxLength(100)] public string? Group { get; set; }
    public DateOnly? SquadDeadline { get; set; }
}
public class CompetitionPlayerRegistration
{
    public int Id { get; set; }
    public int LeagueTeamId { get; set; }
    public LeagueTeam LeagueTeam { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(20)] public string ListType { get; set; } = "Main";
    [MaxLength(20)] public string Status { get; set; } = "Eligible";
    public DateOnly RegisteredOn { get; set; } = BillingClock.Today;
    public DateOnly? EligibleFrom { get; set; }
    public DateOnly? EligibleUntil { get; set; }
    [MaxLength(300)] public string? Notes { get; set; }
}
public class LeagueReportModel
{
    public League League { get; set; } = null!;
    public List<MatchGame> Matches { get; set; } = [];
    public List<LeagueTeamReportRow> Teams { get; set; } = [];
    public List<LeaguePlayerReportRow> Players { get; set; } = [];
}
public record LeagueTeamReportRow(int TeamId,string Team,int Scheduled,int Played,int Wins,int Draws,int Losses,int GoalsFor,int GoalsAgainst,int Minutes);
public record LeaguePlayerReportRow(int StudentId,string Name,string Team,int Appearances,int Starts,int Minutes,int Goals,int Assists,int YellowCards,int RedCards);
public class LeagueInput
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(150)] public string Name { get; set; } = "";
    [RegularExpression("Official|Cup|Friendly")] public string Kind { get; set; } = "Official";
    [Required,StringLength(60)] public string Country { get; set; } = "Kosovë";
    [Required,StringLength(100)] public string Organizer { get; set; } = "FFK";
    [Required,StringLength(30)] public string Sport { get; set; } = "Football";
    [Required,RegularExpression("Male|Female|Mixed")] public string Gender { get; set; } = "Male";
    [Required,StringLength(20)] public string Season { get; set; } = $"{(BillingClock.Today.Month>=7?BillingClock.Today.Year:BillingClock.Today.Year-1)}/{((BillingClock.Today.Month>=7?BillingClock.Today.Year:BillingClock.Today.Year-1)+1).ToString()[2..]}";
    [Required,StringLength(30)] public string AgeCategory { get; set; } = "Senior";
    [Required,RegularExpression("Domestic|Regional|International")] public string CompetitionScope { get; set; } = "Domestic";
    [Required,RegularExpression("League|Cup|SuperCup|Tournament|Friendly")] public string CompetitionType { get; set; } = "League";
    [StringLength(100)] public string? Division { get; set; }
    [StringLength(100)] public string? Group { get; set; }
    [Required,RegularExpression("Qualifying|LeaguePhase|GroupStage|Knockout|Final")] public string Stage { get; set; } = "LeaguePhase";
    public bool IsOfficial { get; set; }
    public bool IsVerified { get; set; }
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
    public Dictionary<int,string> TeamStatuses { get; set; } = [];
    public Dictionary<int,DateOnly?> TeamDeadlines { get; set; } = [];
    [ValidateNever] public List<TrainingTeam> Teams { get; set; } = [];
}
public class CompetitionListModel
{
    public string? Sport { get; set; }
    public string? Gender { get; set; }
    public string? Season { get; set; }
    public string? Organizer { get; set; }
    public string? AgeCategory { get; set; }
    public string? Scope { get; set; }
    public List<League> Rows { get; set; } = [];
}
public record CompetitionCatalogItem(string Key,string Group,string Name,string Organizer,string Country,string Gender,string AgeCategory,string Scope,string Type,string Stage);
public class CompetitionCatalogModel
{
    [Required,StringLength(20)] public string Season { get; set; } = $"{(BillingClock.Today.Month>=7?BillingClock.Today.Year:BillingClock.Today.Year-1)}/{((BillingClock.Today.Month>=7?BillingClock.Today.Year:BillingClock.Today.Year-1)+1).ToString()[2..]}";
    public DateOnly StartsOn { get; set; } = new(BillingClock.Today.Month>=7?BillingClock.Today.Year:BillingClock.Today.Year-1,7,1);
    public DateOnly EndsOn { get; set; } = new(BillingClock.Today.Month>=7?BillingClock.Today.Year+1:BillingClock.Today.Year,6,30);
    public List<string> SelectedKeys { get; set; } = [];
    [ValidateNever] public List<CompetitionCatalogItem> Items { get; set; } = [];
    [ValidateNever] public HashSet<string> ExistingKeys { get; set; } = [];
}
public class CompetitionSquadModel
{
    public int LeagueId { get; set; }
    public int TeamId { get; set; }
    public string CompetitionName { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string ListType { get; set; } = "Main";
    public DateOnly? Deadline { get; set; }
    public List<int> StudentIds { get; set; } = [];
    public List<Student> Players { get; set; } = [];
}
