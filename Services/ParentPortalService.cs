using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ParentPortalService(ApplicationDbContext db)
{
    public async Task<ParentPortalModel?> LoadAsync(string token, DateOnly month, bool finance, Func<decimal, bool>? acknowledged = null, DateOnly? currentDate = null, int? studentId = null)
    {
        if (token.Length != 64 || token.Any(c => !char.IsAsciiHexDigit(c))) return null;
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var access=await new ParentAccessService(db).Resolve(token);
        if(access==null)return null;
        var student=studentId.HasValue?access.Students.SingleOrDefault(x=>x.Id==studentId):access.Students.FirstOrDefault(x=>x.IsActive)??access.Students.FirstOrDefault();
        if (student == null) return null;
        var members=new List<ParentFamilyMember>();
        foreach(var child in access.Students){
            var position=(await new BillingService(db).GetFinancialPositionsAsync(child.Id)).GetValueOrDefault(child.Id);
            members.Add(new(child.Id,child.FullName,position?.Debt??0,position?.Advance??0));
        }
        var today = currentDate ?? BillingClock.Today;
        var result = new ParentPortalModel { Debt = members.Sum(x=>x.Debt), Today = today, Month = month, Finance = finance };
        result.NeedsAcknowledgement = ParentPortalPolicy.NeedsAcknowledgement(result.Debt, acknowledged?.Invoke(result.Debt) ?? false);
        // The gate is enforced before loading any private data, including parking.
        if (result.NeedsAcknowledgement) return result;
        result.StudentId=student.Id;result.FamilyName=access.Family?.Name;
        result.BirthDate=student.DateOfBirth;
        result.Age=today.Year-student.DateOfBirth.Year-(today<student.DateOfBirth.AddYears(today.Year-student.DateOfBirth.Year)?1:0);
        result.HasPhoto=await db.Set<StudentPhoto>().AnyAsync(p=>p.StudentId==student.Id);
        var sport=await new SportDevelopmentService(db).Sport();
        var profiles=await db.DevelopmentRecords.AsNoTracking().Where(r=>r.StudentId==student.Id&&r.Sport==sport&&r.Kind=="Profile"&&r.Date<=today).OrderByDescending(r=>r.Date).ThenByDescending(r=>r.Id).ToListAsync();
        string? Profile(string key)=>profiles.FirstOrDefault(p=>p.Values.ContainsKey(key))?.Values.GetValueOrDefault(key);
        result.Position=Profile("PlayerRole")=="Portier"?"Portier":Profile("PrimaryPosition")??Profile("Position");
        result.Foot=Profile("Foot");
        var chatCoaches=(await new CoachChatService(db).Recipients(student.Id)).Select(x=>x.Id).ToList();
        result.UnreadMessages=await db.CoachMessages.CountAsync(x=>x.StudentId==student.Id&&chatCoaches.Contains(x.CoachId)&&!x.FromParent&&x.ReadAt==null);
        result.Progress=await new ParentProgressService(db).Load(student.Id,today);
        result.WeeklyMenu=await new NutritionMenuService(db).Effective(student.Id);
        result.CareInstructions=await new PlayerCareService(db).ParentNotices(student.Id);
        if(access.Family!=null)result.Members=members;
        result.StudentDebt=members.Single(x=>x.Id==student.Id).Debt;
        result.Name = student.FirstName + " " + student.LastName;
        if(student.TrainingTeamId.HasValue){
            var team=await db.TrainingTeams.AsNoTracking().Where(x=>x.Id==student.TrainingTeamId)
                .Select(x=>new{x.Name,Coach=x.Coach==null?null:x.Coach.FirstName+" "+x.Coach.LastName,Assistant=x.AssistantCoach==null?null:x.AssistantCoach.FirstName+" "+x.AssistantCoach.LastName}).SingleOrDefaultAsync();
            result.TeamName=team?.Name;
            result.CoachName=team?.Coach?.Trim();
            result.AssistantCoachName=team?.Assistant?.Trim();
        }
        result.Advance = members.Sum(x=>x.Advance);
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
            var records = await db.AttendancePlayers.AsNoTracking()
                .Where(a => a.StudentId == student.Id && a.TeamAttendance!.Date >= month && a.TeamAttendance.Date < end && a.TeamAttendance.Date <= today)
                .Select(a => new { a.TeamAttendance!.Date, a.Present }).ToListAsync();
            result.Attendance = records.GroupBy(a => a.Date).OrderByDescending(g => g.Key)
                .Select(g => new ParentAttendance(g.Key, g.Any(a => a.Present))).ToList();

        {
            var fromUtc = BillingClock.UtcDate(month); var toUtc = BillingClock.UtcDate(end);
            result.Movements = await db.StudentTransactions.AsNoTracking()
                .Where(t => t.StudentId == student.Id && !t.IsCancelled && t.TransactionDate >= fromUtc && t.TransactionDate < toUtc)
                .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                .Select(t => new ParentMovement(t.TransactionDate, t.Debit, t.Credit)).ToListAsync();
        }
        {
            if (student.IsActive && student.TrainingTeamId.HasValue)
                result.Schedule = await db.TrainingSessions.AsNoTracking().Where(s => s.TrainingTeamId == student.TrainingTeamId && s.TrainingTeam!.IsActive)
                    .OrderBy(s => s.Day).ThenBy(s => s.StartsAt).Select(s => new ParentSchedule(s.Day, s.StartsAt, s.EndsAt)).ToListAsync();
        }
        return result;
    }
}
