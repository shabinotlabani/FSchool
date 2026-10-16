using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ParentPortalService(ApplicationDbContext db)
{
    public async Task<ParentPortalModel?> LoadAsync(string token, DateOnly month, bool finance, Func<decimal, bool>? acknowledged = null, DateOnly? currentDate = null)
    {
        if (token.Length != 64 || token.Any(c => !char.IsAsciiHexDigit(c))) return null;
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var student = await db.Students.AsNoTracking().Where(s => s.ParentLinkToken == token)
            .Select(s => new { s.Id, s.FirstName, s.LastName, s.TrainingTeamId, s.IsActive }).SingleOrDefaultAsync();
        if (student == null) return null;
        var position = (await new BillingService(db).GetFinancialPositionsAsync(student.Id)).GetValueOrDefault(student.Id);
        var today = currentDate ?? BillingClock.Today;
        var result = new ParentPortalModel { Debt = position?.Debt ?? 0, Today = today, Month = month, Finance = finance };
        result.NeedsAcknowledgement = ParentPortalPolicy.NeedsAcknowledgement(result.Debt, acknowledged?.Invoke(result.Debt) ?? false);
        // The gate is enforced before loading any private data, including parking.
        if (result.NeedsAcknowledgement) return result;
        result.Name = student.FirstName + " " + student.LastName;
        result.Advance = position?.Advance ?? 0;
        result.Month = month;
        result.Finance = finance;
        if (student.IsActive)
        {
            var weekday = ((int)today.DayOfWeek + 6) % 7 + 1;
            if (student.TrainingTeamId.HasValue)
                result.TodaySessions = await db.TrainingSessions.AsNoTracking()
                    .Where(s => s.TrainingTeamId == student.TrainingTeamId && s.TrainingTeam!.IsActive && s.Day == weekday)
                    .Select(s => new ParentSchedule(s.Day, s.StartsAt, s.EndsAt)).ToListAsync();
            var personal = await db.PersonalTrainings.AsNoTracking().Include(t => t.Sessions)
                .Where(t => t.StudentId == student.Id && !t.IsCancelled && t.StartsOn <= today && (t.EndsOn == null || t.EndsOn >= today)
                    && (t.Mode == "Monthly" || t.StartsOn == today)).ToListAsync();
            result.TodaySessions.AddRange(personal.SelectMany(t => t.Sessions.Where(s => t.Mode != "Monthly" || s.Day == weekday))
                .Select(s => new ParentSchedule(weekday, s.StartsAt, s.EndsAt)));
            result.TodaySessions = result.TodaySessions.Distinct().OrderBy(s => s.Start).ToList();
            if (result.TodaySessions.Count > 0) result.ParkingTicketId = ParentPortalPolicy.TicketId(student.Id, today);
        }
        var end = month.AddMonths(1);
        if (finance)
        {
            var fromUtc = BillingClock.UtcDate(month); var toUtc = BillingClock.UtcDate(end);
            result.Movements = await db.StudentTransactions.AsNoTracking()
                .Where(t => t.StudentId == student.Id && !t.IsCancelled && t.TransactionDate >= fromUtc && t.TransactionDate < toUtc)
                .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                .Select(t => new ParentMovement(t.TransactionDate, t.Debit, t.Credit)).ToListAsync();
        }
        else
        {
            if (student.IsActive && student.TrainingTeamId.HasValue)
                result.Schedule = await db.TrainingSessions.AsNoTracking().Where(s => s.TrainingTeamId == student.TrainingTeamId && s.TrainingTeam!.IsActive)
                    .OrderBy(s => s.Day).ThenBy(s => s.StartsAt).Select(s => new ParentSchedule(s.Day, s.StartsAt, s.EndsAt)).ToListAsync();
            var records = await db.AttendancePlayers.AsNoTracking()
                .Where(a => a.StudentId == student.Id && a.TeamAttendance!.Date >= month && a.TeamAttendance.Date < end && a.TeamAttendance.Date <= today)
                .Select(a => new { a.TeamAttendance!.Date, a.Present }).ToListAsync();
            result.Attendance = records.GroupBy(a => a.Date).OrderByDescending(g => g.Key)
                .Select(g => new ParentAttendance(g.Key, g.Any(a => a.Present))).ToList();
        }
        return result;
    }
}
