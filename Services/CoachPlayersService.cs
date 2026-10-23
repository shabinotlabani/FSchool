using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class CoachPlayersService(ApplicationDbContext db)
{
    public async Task<CoachPlayers> Load(string coach, string? search)
    {
        var today = BillingClock.Today;
        var from = today.AddDays(-29);
        var assigned = new CoachChatService(db).Assigned(coach).Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) {
            var term = search.Trim().ToLower();
            assigned = assigned.Where(s => (s.FirstName + " " + s.LastName).ToLower().Contains(term));
        }
        var players = await assigned.AsNoTracking().OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
            .Select(s => new { s.Id, s.FirstName, s.LastName, s.DateOfBirth, Team = s.TrainingTeam!.Name, s.TrainingTeamId }).ToListAsync();
        var ids = players.Select(s => s.Id).ToArray();
        // Only the currently assigned team; count one registered day, preferring the head coach's register.
        var entries = await db.AttendancePlayers.AsNoTracking()
            .Where(p => ids.Contains(p.StudentId) && p.TeamAttendance!.Date >= from && p.TeamAttendance.Date <= today &&
                p.TeamAttendance.TrainingTeam!.IsActive &&
                (p.TeamAttendance.TrainingTeam.CoachId == coach || p.TeamAttendance.TrainingTeam.AssistantCoachId == coach))
            .Select(p => new { p.StudentId, p.Present, p.TeamAttendance!.Date, p.TeamAttendance.TrainingTeamId, p.TeamAttendance.CoachRole, p.TeamAttendance.UpdatedAt }).ToListAsync();
        var rows = players.Select(s => {
            var days = entries.Where(p => p.StudentId == s.Id && p.TrainingTeamId == s.TrainingTeamId)
                .GroupBy(p => p.Date).Select(g => g.OrderBy(p => p.CoachRole == "head" ? 0 : 1).ThenByDescending(p => p.UpdatedAt).First()).ToList();
            var age = today.Year - s.DateOfBirth.Year;
            if (s.DateOfBirth.AddYears(age) > today) age--;
            return new CoachPlayerRow(s.Id, (s.FirstName + " " + s.LastName).Trim(), s.Team, age, days.Count(p => p.Present), days.Count);
        }).ToList();
        return new() { Search = search, From = from, To = today, Players = rows };
    }
}
