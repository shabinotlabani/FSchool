using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ParentPortalService(ApplicationDbContext db)
{
    public async Task<ParentPortalModel?> LoadAsync(string token, DateOnly month, bool finance)
    {
        if (token.Length != 64 || token.Any(c => !char.IsAsciiHexDigit(c))) return null;
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var student = await db.Students.AsNoTracking().Where(s => s.ParentLinkToken == token)
            .Select(s => new { s.Id, s.FirstName, s.LastName, s.TrainingTeamId, s.IsActive }).SingleOrDefaultAsync();
        if (student == null) return null;
        var position = (await new BillingService(db).GetFinancialPositionsAsync(student.Id)).GetValueOrDefault(student.Id);
        var result = new ParentPortalModel { Debt = position?.Debt ?? 0 };
        // Debt responses never load or return schedules, attendance or account details.
        if (result.Debt > 0) return result;
        result.Name = student.FirstName + " " + student.LastName;
        result.Advance = position?.Advance ?? 0;
        result.Month = month;
        result.Finance = finance;
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
            var today = BillingClock.Today;
            var records = await db.AttendancePlayers.AsNoTracking()
                .Where(a => a.StudentId == student.Id && a.TeamAttendance!.Date >= month && a.TeamAttendance.Date < end && a.TeamAttendance.Date <= today)
                .Select(a => new { a.TeamAttendance!.Date, a.Present }).ToListAsync();
            result.Attendance = records.GroupBy(a => a.Date).OrderByDescending(g => g.Key)
                .Select(g => new ParentAttendance(g.Key, g.Any(a => a.Present))).ToList();
        }
        return result;
    }
}
