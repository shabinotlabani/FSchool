namespace _2Korriku.Models;

public record CoachTeamCard(TrainingTeam Team, DateTime? StartsAt, DateTime? EndsAt, bool InProgress, string When);
