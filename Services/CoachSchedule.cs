using _2Korriku.Models;

namespace _2Korriku.Services;

public static class CoachSchedule
{
    public static List<CoachTeamCard> Order(IEnumerable<TrainingTeam> teams, DateTime utcNow)
    {
        var now=TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow,DateTimeKind.Utc),BillingClock.Zone);
        var weekday=((int)now.DayOfWeek+6)%7+1;
        return teams.Select(team=>{
            var next=team.Sessions.Where(s=>s.Day is >=1 and <=7 && s.EndsAt>s.StartsAt).Select(s=>{
                var date=now.Date.AddDays((s.Day-weekday+7)%7);
                var start=date.Add(s.StartsAt.ToTimeSpan());
                var end=date.Add(s.EndsAt.ToTimeSpan());
                if(end<=now){start=start.AddDays(7);end=end.AddDays(7);}
                return new{Start=start,End=end,Active=start<=now&&now<end};
            }).OrderByDescending(s=>s.Active).ThenBy(s=>s.Start).FirstOrDefault();
            var label=next==null?"Pa orar":next.Active?"Në stërvitje tani":next.Start.Date==now.Date?"Sot":next.Start.Date==now.Date.AddDays(1)?"Nesër":$"{TeamService.DayName(((int)next.Start.DayOfWeek+6)%7+1)} · {next.Start:dd.MM}";
            return new CoachTeamCard(team,next?.Start,next?.End,next?.Active??false,label);
        }).OrderByDescending(x=>x.InProgress).ThenBy(x=>x.StartsAt??DateTime.MaxValue).ThenBy(x=>x.Team.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(x=>x.Team.Id).ToList();
    }
}
