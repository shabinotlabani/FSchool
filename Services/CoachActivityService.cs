using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace _2Korriku.Services;

public class CoachActivityService(ApplicationDbContext db)
{
    public async Task<CoachActivityReport> Load(DateOnly from, DateOnly to)
    {
        var start = BillingClock.UtcDate(from);
        var end = BillingClock.UtcDate(to.AddDays(1));
        var coaches = await db.Users.AsNoTracking().Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == Roles.Coach)))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName }).ToListAsync();
        var ids = coaches.Select(x => x.Id).ToList();
        // First save per coach/session: repeated corrections earn no additional points.
        var attendance = await db.AttendanceChanges.AsNoTracking().Where(x => ids.Contains(x.ActorId))
            .GroupBy(x => new { x.ActorId, x.TeamAttendanceId })
            .Select(g => new { Actor = g.Key.ActorId, At = g.Min(x => x.CreatedAt) })
            .Where(x => x.At >= start && x.At < end).ToListAsync();
        var development = await db.DevelopmentRecords.AsNoTracking().Where(x => ids.Contains(x.CoachId))
            .GroupBy(x => new { x.CoachId, x.StudentId, x.Kind, x.Date })
            .Select(g => new { Actor = g.Key.CoachId, At = g.Min(x => x.CreatedAt) })
            .Where(x => x.At >= start && x.At < end).ToListAsync();
        var referrals = await db.CareReferrals.AsNoTracking().Where(x => ids.Contains(x.CoachId) && x.CreatedAt >= start && x.CreatedAt < end)
            .Select(x => new { Actor = x.CoachId, x.StudentId, x.Kind, At = x.CreatedAt }).ToListAsync();
        var uniqueReferrals = referrals.GroupBy(x => new { x.Actor, x.StudentId, x.Kind, Day = BillingClock.LocalDate(x.At) }).Select(g => g.OrderBy(x => x.At).First()).ToList();
        // Read earlier history too: a correction in this period must not earn the first-save point again.
        var changes = await db.MatchChanges.AsNoTracking().Where(x => x.CreatedAt < end)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => new { x.MatchGameId, x.ActorId, x.CreatedAt, x.Snapshot }).ToListAsync();
        var lineups = new HashSet<int>();
        var completed = new HashSet<int>();
        var matchActivity = new List<(string Actor, DateTime At, bool Lineup)>();
        foreach (var change in changes)
        {
            MatchInput? saved;
            try { saved = JsonSerializer.Deserialize<MatchInput>(change.Snapshot); }
            catch (JsonException) { continue; }
            if (saved == null) continue;
            var hasLineup = saved.Players?.Any(p => p.Role == "Starter") == true;
            if (hasLineup && saved.Status is "Live" or "Completed" && lineups.Add(change.MatchGameId))
                matchActivity.Add((change.ActorId, change.CreatedAt, true));
            if (hasLineup && saved.Status == "Completed" && saved.ScoreFor.HasValue && saved.ScoreAgainst.HasValue && completed.Add(change.MatchGameId))
                matchActivity.Add((change.ActorId, change.CreatedAt, false));
        }
        var activity = matchActivity.Where(x => x.At >= start).ToList();
        var rows = coaches.Select(c => {
            var a = attendance.Where(x => x.Actor == c.Id).Select(x => x.At).ToList();
            var d = development.Where(x => x.Actor == c.Id).Select(x => x.At).ToList();
            var r = uniqueReferrals.Where(x => x.Actor == c.Id).Select(x => x.At).ToList();
            var l = activity.Where(x => x.Actor == c.Id && x.Lineup).Select(x => x.At).ToList();
            var m = activity.Where(x => x.Actor == c.Id && !x.Lineup).Select(x => x.At).ToList();
            var dates = a.Concat(d).Concat(r).Concat(l).Concat(m).ToList();
            var name = $"{c.FirstName} {c.LastName}".Trim();
            return new CoachActivityRow(name.Length > 0 ? name : c.UserName ?? "Trajner", a.Count, d.Count, r.Count,
                dates.Select(BillingClock.LocalDate).Distinct().Count(), dates.Count > 0 ? dates.Max() : null, l.Count, m.Count);
        }).OrderByDescending(x => x.Points).ThenBy(x => x.Name).ToList();
        return new CoachActivityReport { From = from, To = to, Rows = rows };
    }
}
