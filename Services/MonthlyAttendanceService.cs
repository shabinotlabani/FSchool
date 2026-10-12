using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class MonthlyAttendanceService(ApplicationDbContext db)
{
    public async Task PopulateAsync(IEnumerable<MonthlyInvoiceView> invoices, int year, int month)
    {
        var list=invoices.ToList();
        var ids=list.Select(x=>x.Student.Id).Distinct().ToList();
        if(ids.Count==0)return;
        var start=new DateOnly(year,month,1);
        var end=start.AddMonths(1);
        var teamIds=list.Where(x=>x.Student.TrainingTeamId.HasValue).Select(x=>x.Student.TrainingTeamId!.Value).Distinct().ToList();
        var sessions=await db.TrainingSessions.AsNoTracking().Where(x=>teamIds.Contains(x.TrainingTeamId)).ToListAsync();
        var attendance=await db.AttendancePlayers.AsNoTracking().Where(x=>ids.Contains(x.StudentId)&&x.TeamAttendance!.Date>=start&&x.TeamAttendance.Date<end)
            .Select(x=>new{x.StudentId,x.TeamAttendance!.Date,x.Present}).ToListAsync();
        foreach(var invoice in list){
            var recorded=attendance.Where(x=>x.StudentId==invoice.Student.Id).ToList();
            var weekdays=sessions.Where(x=>x.TrainingTeamId==invoice.Student.TrainingTeamId).Select(x=>x.Day).ToHashSet();
            var registered=BillingClock.LocalDate(invoice.Student.RegistrationDate);
            var dates=new HashSet<DateOnly>();
            for(var date=start;date<end;date=date.AddDays(1))
                if(date>=registered&&weekdays.Contains(((int)date.DayOfWeek+6)%7+1))dates.Add(date);
            // Attendance is recorded once per team/day. Include recorded extra sessions
            // and previous-team dates without counting the same day twice.
            dates.UnionWith(recorded.Select(x=>x.Date));
            invoice.Attendance=new(recorded.Where(x=>x.Present).Select(x=>x.Date).Distinct().Count(),dates.Count);
        }
    }
}
