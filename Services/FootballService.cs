using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class FootballService(ApplicationDbContext db)
{
    public static void Validate(MatchInput input, string sport)
    {
        if(input.Events==null || input.Events.Count>500 || input.Events.Any(e=>e==null))throw new ValidationException("Kontrolloni ngjarjet.");
        if(sport!="Football" && input.Events.Count>0)throw new ValidationException("Këto ngjarje janë për futboll.");
        var roster=input.Players.Where(p=>p.Role!="NotSelected").ToDictionary(p=>p.StudentId);
        var yellows=new HashSet<int>();var dismissed=new HashSet<int>();var reductions=new List<int>();
        bool Playing(int id,int minute)=>roster.TryGetValue(id,out var p)&&p.Intervals.Any(x=>x.From<=minute&&x.To>=minute);
        foreach(var e in input.Events.OrderBy(e=>e.Minute)){
            Validator.ValidateObject(e,new(e),true);
            if(!FootballLabels.Events.ContainsKey(e.Kind)||e.Minute>input.Duration)throw new ValidationException("Kontrolloni llojin dhe minutën e ngjarjes.");
            var opponent=e.Kind is "OpponentGoal" or "OpponentOwnGoal";
            if(opponent){if(e.PlayerId!=null||e.AssistId!=null)throw new ValidationException("Goli i kundërshtarit nuk ka lojtar nga ekipi ynë.");}
            else if(e.PlayerId==null||!roster.ContainsKey(e.PlayerId.Value)||dismissed.Contains(e.PlayerId.Value))throw new ValidationException("Zgjidhni lojtar nga lista, që nuk është përjashtuar.");
            if(!FootballLabels.Card(e)&&!opponent&&!Playing(e.PlayerId!.Value,e.Minute))throw new ValidationException("Golashënuesi duhet të jetë në fushë në atë minutë.");
            if(e.AssistId.HasValue&&(e.Kind!="Goal"||e.AssistId==e.PlayerId||dismissed.Contains(e.AssistId.Value)||!Playing(e.AssistId.Value,e.Minute)))throw new ValidationException("Asistuesi duhet të jetë lojtar tjetër në fushë.");
            if(e.Kind=="Yellow"&&!yellows.Add(e.PlayerId!.Value))e.Kind="SecondYellow";
            if(e.Kind=="SecondYellow"&&!yellows.Contains(e.PlayerId!.Value))throw new ValidationException("Regjistroni së pari kartonin e parë të verdhë.");
            if(e.SuspensionGames.HasValue&&(!FootballLabels.Dismissal(e)||string.IsNullOrWhiteSpace(e.Decision)))throw new ValidationException("Për masën disiplinore shkruani vendimin.");
            if(FootballLabels.Dismissal(e)){
                var p=roster[e.PlayerId!.Value];dismissed.Add(p.StudentId);
                if(p.Intervals.Any(x=>x.To>e.Minute))throw new ValidationException("Lojtari i përjashtuar nuk mund të luajë pas kartonit të kuq. Rregulloni daljen.");
                if(Playing(p.StudentId,e.Minute))reductions.Add(e.Minute);
            }
        }
        foreach(var minute in roster.Values.SelectMany(p=>p.Intervals).Select(x=>x.From!.Value).Concat(reductions).Distinct()){
            var count=roster.Values.Count(p=>p.Intervals.Any(x=>x.From<=minute&&x.To>minute));
            if(count>input.PlayersOnCourt-reductions.Count(m=>m<=minute))throw new ValidationException("Lojtari i përjashtuar nuk mund të zëvendësohet.");
        }
        if(input.ScoreFor.HasValue&&input.Events.Count(e=>e.Kind is "Goal" or "PenaltyGoal" or "OpponentOwnGoal")>input.ScoreFor || input.ScoreAgainst.HasValue&&input.Events.Count(e=>e.Kind is "OwnGoal" or "OpponentGoal")>input.ScoreAgainst)throw new ValidationException("Golat e regjistruar tejkalojnë rezultatin. Përditësoni rezultatin.");
    }
    public async Task<DisciplineRule?> Rule(int team,string kind,DateOnly date,int? leagueId)
    {
        if(leagueId.HasValue)return (await db.Leagues.AsNoTracking().SingleOrDefaultAsync(l=>l.Id==leagueId))?.Rule;
        return await db.DisciplineRules.AsNoTracking().SingleOrDefaultAsync(r=>r.TrainingTeamId==team&&r.Kind==kind&&r.StartsOn<=date&&r.EndsOn>=date);
    }
    public async Task<List<DisciplineAlert>> Alerts(int team,string kind,DateOnly date,TimeOnly? kickoff,int exclude=0,int? leagueId=null)
    {
        if(!leagueId.HasValue&&exclude!=0)leagueId=await db.MatchGames.Where(g=>g.Id==exclude&&g.TrainingTeamId==team).Select(g=>g.LeagueId).SingleOrDefaultAsync();
        var rule=await Rule(team,kind,date,leagueId);
        var start=leagueId.HasValue?await db.Leagues.Where(l=>l.Id==leagueId).Select(l=>l.StartsOn).SingleAsync():rule?.StartsOn??new DateOnly(date.Month>=7?date.Year:date.Year-1,7,1);
        var games=await db.MatchGames.AsNoTracking().Include(g=>g.Players).Where(g=>g.TrainingTeamId==team&&g.LeagueId==leagueId&&g.Kind==kind&&g.Sport=="Football"&&g.Status=="Completed"&&g.Id!=exclude&&g.Date>=start&&g.Date<=date).OrderBy(g=>g.Date).ThenBy(g=>g.Kickoff).ThenBy(g=>g.Id).ToListAsync();
        var state=new Dictionary<int,DisciplineAlert>();
        foreach(var g in games.Where(g=>g.Date<date||!kickoff.HasValue||g.Kickoff<kickoff)){
            foreach(var id in state.Keys.ToList())if(!g.Players.Any(p=>p.StudentId==id))state[id]=state[id] with{Remaining=Math.Max(0,state[id].Remaining-1)};
            foreach(var group in g.Events.Where(e=>FootballLabels.Card(e)&&e.PlayerId.HasValue).GroupBy(e=>e.PlayerId!.Value)){
                var s=state.GetValueOrDefault(group.Key)??new(group.Key,g.Players.FirstOrDefault(p=>p.StudentId==group.Key)?.Name??"Lojtar",0,false,0,rule?.YellowThreshold);
                var dismissal=group.SingleOrDefault(FootballLabels.Dismissal);
                var yellow=s.Yellows+((dismissal?.Kind=="SecondYellow"&&rule?.CountDismissalYellows!=true)?0:group.Count(e=>e.Kind is "Yellow" or "SecondYellow"));
                var ban=s.Remaining;var unknown=s.NeedsDecision;
                if(rule!=null&&yellow>=rule.YellowThreshold){ban+=yellow/rule.YellowThreshold*rule.YellowBan;yellow%=rule.YellowThreshold;}
                if(dismissal!=null){var penalty=dismissal.SuspensionGames??(rule==null?(int?)null:dismissal.Kind=="Red"?rule.RedBan:rule.SecondYellowBan);if(penalty.HasValue)ban+=penalty.Value;else unknown=true;}
                state[group.Key]=s with{Remaining=ban,NeedsDecision=unknown,Yellows=yellow};
            }
        }
        return state.Values.Where(s=>s.Remaining>0||s.NeedsDecision||s.Yellows>0&&(rule==null||s.Yellows==rule.YellowThreshold-1)).OrderByDescending(s=>s.Remaining).ThenBy(s=>s.Name).ToList();
    }
}
