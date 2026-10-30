using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class MatchGame
{
    public int? LeagueId { get; set; }
    public League? League { get; set; }
    public int Id { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
    public int TrainingTeamId { get; set; }
    public TrainingTeam TrainingTeam { get; set; } = null!;
    [MaxLength(30)] public string Sport { get; set; } = "Football";
    [MaxLength(150)] public string Opponent { get; set; } = "";
    [MaxLength(150)] public string? Competition { get; set; }
    [MaxLength(30)] public string? CompetitionStage { get; set; }
    [MaxLength(150)] public string? Venue { get; set; }
    [MaxLength(20)] public string Kind { get; set; } = "Official";
    [MaxLength(20)] public string Status { get; set; } = "Draft";
    public bool Home { get; set; } = true;
    public DateOnly Date { get; set; }
    public TimeOnly? Kickoff { get; set; }
    public int Duration { get; set; } = 90;
    public int PlayersOnCourt { get; set; } = 11;
    [MaxLength(20)] public string Formation { get; set; } = "4-4-2";
    public int? ScoreFor { get; set; }
    public int? ScoreAgainst { get; set; }
    public string UpdatedById { get; set; } = "";
    public ApplicationUser UpdatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
    public List<MatchPlayer> Players { get; set; } = [];
    public string EventData { get; set; } = "[]";
    [NotMapped] public List<FootballEvent> Events => JsonSerializer.Deserialize<List<FootballEvent>>(EventData) ?? [];
}
public class MatchPlayer
{
    public bool IsCallUp { get; set; }
    public int? HomeTeamId { get; set; }
    public TrainingTeam? HomeTeam { get; set; }
    [MaxLength(300)] public string? CallUpReason { get; set; }
    [Range(1,5)] public int? CoachRating { get; set; }
    [MaxLength(1000)] public string? CoachNote { get; set; }
    public int Id { get; set; }
    public int MatchGameId { get; set; }
    public MatchGame MatchGame { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(201)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Role { get; set; } = "Unused";
    public int Minutes { get; set; }
    public int? PitchSlot { get; set; }
    public string Intervals { get; set; } = "[]";
    [NotMapped] public List<MatchInterval> Stints => JsonSerializer.Deserialize<List<MatchInterval>>(Intervals) ?? [];
}
public class MatchChange
{
    public int Id { get; set; }
    public int MatchGameId { get; set; }
    public MatchGame MatchGame { get; set; } = null!;
    public string ActorId { get; set; } = "";
    public ApplicationUser Actor { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string Snapshot { get; set; } = "";
}
public class MatchInterval
{
    [Range(0,300)] public int? From { get; set; }
    [Range(0,300)] public int? To { get; set; }
}
public class MatchPlayerInput
{
    public bool IsCallUp { get; set; }
    [ValidateNever] public string? HomeTeamName { get; set; }
    [Range(1,5)] public int? CoachRating { get; set; }
    [StringLength(1000)] public string? CoachNote { get; set; }
    public int StudentId { get; set; }
    public string Role { get; set; } = "NotSelected";
    [Range(0,29)] public int? PitchSlot { get; set; }
    public List<MatchInterval> Intervals { get; set; } = [];
    [ValidateNever] public string Name { get; set; } = "";
    [ValidateNever] public string? Care { get; set; }
}
public class MatchInput
{
    public int? CallUpStudentId { get; set; }
    [StringLength(300)] public string? CallUpReason { get; set; }
    [ValidateNever] public List<MatchCallUpCandidate> CallUpCandidates { get; set; } = [];
    public int? LeagueId { get; set; }
    [ValidateNever] public List<League> Leagues { get; set; } = [];
    public int Id { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public Guid Revision { get; set; }
    [Range(1,int.MaxValue)] public int TeamId { get; set; }
    [Required, StringLength(150)] public string Opponent { get; set; } = "";
    [StringLength(150)] public string? Competition { get; set; }
    [StringLength(30)] public string? CompetitionStage { get; set; }
    [StringLength(150)] public string? Venue { get; set; }
    [RegularExpression("Official|Cup|Friendly")] public string Kind { get; set; } = "Official";
    [RegularExpression("Draft|Live|Completed|Cancelled")] public string Status { get; set; } = "Draft";
    public bool Home { get; set; } = true;
    public DateOnly Date { get; set; } = BillingClock.Today;
    public TimeOnly? Kickoff { get; set; }
    [Range(1,300)] public int Duration { get; set; } = 90;
    [Range(1,30)] public int PlayersOnCourt { get; set; } = 11;
    [Required, RegularExpression("4-4-2|4-3-3|3-5-2|4-2-3-1|Auto")] public string Formation { get; set; } = "4-4-2";
    [Range(0,999)] public int? ScoreFor { get; set; }
    [Range(0,999)] public int? ScoreAgainst { get; set; }
    public List<MatchPlayerInput> Players { get; set; } = [];
    public List<FootballEvent> Events { get; set; } = [];
    [ValidateNever] public List<DisciplineAlert> DisciplineAlerts { get; set; } = [];
    [ValidateNever] public bool HasDisciplineRule { get; set; }
    [ValidateNever] public string TeamName { get; set; } = "";
    [ValidateNever] public string Sport { get; set; } = "Football";
}
public record MatchCallUpCandidate(int StudentId,string Name,string TeamName);
public class MatchFixtureProposal
{
    public int Index { get; set; }
    [Required,StringLength(150)] public string Opponent { get; set; } = "";
    public DateOnly Date { get; set; }
    public TimeOnly? Kickoff { get; set; }
    public bool Home { get; set; }
    [StringLength(150)] public string? Venue { get; set; }
    [StringLength(30)] public string? Stage { get; set; }
    [Required,StringLength(500)] public string SourceUrl { get; set; } = "";
}
public class MatchFixtureProposalModel
{
    [Range(1,int.MaxValue)] public int TeamId { get; set; }
    [Range(1,int.MaxValue)] public int LeagueId { get; set; }
    public DateOnly From { get; set; } = BillingClock.Today;
    public List<int> Selected { get; set; } = [];
    public List<MatchFixtureProposal> Fixtures { get; set; } = [];
    [ValidateNever] public string TeamName { get; set; } = "";
    [ValidateNever] public string LeagueName { get; set; } = "";
    [ValidateNever] public List<TrainingTeam> Teams { get; set; } = [];
    [ValidateNever] public List<League> Leagues { get; set; } = [];
}
public class MatchList
{
    public CoachActivityReport? CoachActivity { get; set; }
    public string? CompetitionFilter { get; set; }
    public int? LeagueId { get; set; }
    public List<League> Leagues { get; set; } = [];
    public List<TrainingTeam> Teams { get; set; } = [];
    public int? TeamId { get; set; }
    public int Year { get; set; }
    public string? Kind { get; set; }
    public List<MatchGame> Matches { get; set; } = [];
    public List<PlayerMonthlyCount> ActivePlayersByMonth { get; set; } = [];
    public MonthlyPaymentRate? CurrentMonthPayments { get; set; }
    public List<MonthlyPaymentRate> MonthlyPayments { get; set; } = [];
    public List<(int TeamId,string TeamName,string Competition,DisciplineAlert Alert)> DisciplineAlerts { get; set; } = [];
}
public record PlayerMonthlyCount(int Month, string Label, int Count);
public record MonthlyPaymentRate(int Eligible, int Paid, int Partial, decimal Percent, string MonthLabel);
public record MatchPlayerTotal(int StudentId, string Name, int Games, int Starts, int Substitutions, int Unused, int OfficialMinutes, int FriendlyMinutes)
{
    public int CupMinutes { get; init; }
    public int Goals { get; init; }
    public int Assists { get; init; }
    public int YellowCards { get; init; }
    public int RedCards { get; init; }
}
public class MatchReport : MatchList
{
    public int? StudentId { get; set; }
    public List<MatchPlayerTotal> Totals { get; set; } = [];
}
public class MatchMatrix
{
    public MatchReport Report { get; set; } = new();
    public List<MatchPlayerTotal> Players { get; set; } = [];
}
public record MatchPitchCard(MatchGame Game, MatchPlayer Player);
public record AdminMatchPlayer(MatchPlayer Current,List<MatchPlayer> History);
public record PlayerMatchMinutes(int TotalMinutes,int Appearances)
{
    public decimal Average => Appearances==0?0:Math.Round((decimal)TotalMinutes/Appearances,1);
}
public static class MatchLabels
{
    public static string Kind(string value) => value switch { "Friendly"=>"Miqësore", "Cup"=>"Për kupë", _=>"Për pikë" };
    public static string Status(string value) => value=="Live"?"Në lojë":value=="Completed"?"Përfunduar":value=="Cancelled"?"Anuluar":"Në përgatitje";
    public static string Role(string value) => value switch { "Starter"=>"Titullar", "Substitute"=>"Nga stoli", "Unused"=>"Rezervë pa hyrë", _=>"Jashtë listës" };
}
