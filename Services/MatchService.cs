using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class MatchService(ApplicationDbContext db)
{
    public IQueryable<TrainingTeam> Teams(string actor) => db.TrainingTeams.Where(t=>t.IsActive&&(t.CoachId==actor||t.AssistantCoachId==actor));
    private async Task<TrainingTeam> Team(int id,string actor) => await Teams(actor).AsNoTracking().SingleOrDefaultAsync(t=>t.Id==id) ?? throw new UnauthorizedAccessException();
    public async Task<PlayerMatchMinutes> PlayerMinutes(string actor,int studentId,int? year=null)
    {
        await new PlayerDevelopmentService(db).Player(studentId,actor);
        var teams=Teams(actor).Select(t=>t.Id);
        var minutes=await db.MatchPlayers.AsNoTracking().Where(p=>p.StudentId==studentId&&p.MatchGame.Status=="Completed"&&teams.Contains(p.MatchGame.TrainingTeamId)&&(!year.HasValue||p.MatchGame.Date.Year==year)&&p.Minutes>0).Select(p=>p.Minutes).ToListAsync();
        return new(minutes.Sum(),minutes.Count);
    }
    public async Task<MatchList> List(string actor,int? teamId,int year,string? kind)
    {
        if(year is <1900 or >2200 || kind is not(null or "Official" or "Cup" or "Friendly"))throw new ValidationException("Kontrolloni filtrat.");
        var teams=await Teams(actor).AsNoTracking().OrderBy(t=>t.Name).ToListAsync();
        if(teamId.HasValue&&!teams.Any(t=>t.Id==teamId))throw new UnauthorizedAccessException();
        var ids=teams.Select(t=>t.Id).ToArray();
        var games=await db.MatchGames.AsNoTracking().Include(m=>m.TrainingTeam).Include(m=>m.Players)
            .Where(m=>ids.Contains(m.TrainingTeamId)&&(!teamId.HasValue||m.TrainingTeamId==teamId)&&m.Date.Year==year&&(kind==null||m.Kind==kind))
            .OrderByDescending(m=>m.Date).ThenByDescending(m=>m.Kickoff).ToListAsync();
        var result=new MatchList{Teams=teams,TeamId=teamId,Year=year,Kind=kind,Matches=games};
        foreach(var t in teams.Where(t=>!teamId.HasValue||t.Id==teamId)){
            var leagues=await db.Leagues.AsNoTracking().Where(l=>l.IsActive&&l.Teams.Any(x=>x.TrainingTeamId==t.Id)).ToListAsync();
            foreach(var league in leagues){
                var next=games.Where(g=>g.TrainingTeamId==t.Id&&g.LeagueId==league.Id&&g.Status=="Draft"&&g.Date>=BillingClock.Today).OrderBy(g=>g.Date).ThenBy(g=>g.Kickoff).FirstOrDefault();
                foreach(var a in await new FootballService(db).Alerts(t.Id,league.Kind,next?.Date??BillingClock.Today,next?.Kickoff,next?.Id??0,league.Id))result.DisciplineAlerts.Add((t.Id,t.Name,league.Name,a));
            }
        }
        return result;
    }
    public async Task<MatchInput> Load(string actor,int id,int teamId)
    {
        MatchGame? game=null;
        if(id!=0){game=await db.MatchGames.AsNoTracking().Include(m=>m.Players).SingleOrDefaultAsync(m=>m.Id==id)??throw new UnauthorizedAccessException();teamId=game.TrainingTeamId;}
        var team=await Team(teamId,actor);
        var sport=game?.Sport??await new SportDevelopmentService(db).Sport();
        var input=game==null?new MatchInput{TeamId=teamId,Sport=sport,Duration=sport=="Basketball"?40:sport=="Handball"?60:90,PlayersOnCourt=sport switch{"Basketball"=>5,"Volleyball"=>6,"Handball"=>7,"Tennis"=>1,_=>11}}:
            new MatchInput{Id=game.Id,RequestId=game.RequestId,Revision=game.Revision,TeamId=teamId,Sport=sport,Opponent=game.Opponent,Competition=game.Competition,Venue=game.Venue,Date=game.Date,Kickoff=game.Kickoff,Kind=game.Kind,Status=game.Status,Home=game.Home,Duration=game.Duration,PlayersOnCourt=game.PlayersOnCourt,ScoreFor=game.ScoreFor,ScoreAgainst=game.ScoreAgainst,Formation=game.Formation};
        input.TeamName=team.Name;
        input.LeagueId=game?.LeagueId;
        input.Leagues=await db.Leagues.AsNoTracking().Where(l=>(l.IsActive&&l.Teams.Any(t=>t.TrainingTeamId==teamId))||l.Id==input.LeagueId).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync();
        if(game==null)input.LeagueId=await new LeagueService(db).Eligible(teamId,input.Date).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).Select(l=>(int?)l.Id).FirstOrDefaultAsync();
        if(game==null)input.Kind=input.LeagueId.HasValue?input.Leagues.Single(l=>l.Id==input.LeagueId).Kind:"Friendly";
        input.Events=game?.Events??[];
        if(sport=="Football"){input.DisciplineAlerts=await new FootballService(db).Alerts(teamId,input.Kind,input.Date,input.Kickoff,input.Id,input.LeagueId);input.HasDisciplineRule=await new FootballService(db).Rule(teamId,input.Kind,input.Date,input.LeagueId)!=null;}
        var stored=game?.Players.ToDictionary(p=>p.StudentId)??[];
        var students=await db.Students.AsNoTracking().Where(s=>(s.TrainingTeamId==teamId&&s.IsActive)||stored.Keys.Contains(s.Id)).OrderBy(s=>s.FirstName).ThenBy(s=>s.LastName).ToListAsync();
        var care=await new PlayerCareService(db).TrainingNotices(students.Select(s=>s.Id).ToList(),input.Date);
        input.Players=students.Select(s=>{
            stored.TryGetValue(s.Id,out var row);
            var notice=care.FirstOrDefault(c=>c.StudentId==s.Id);
            return new MatchPlayerInput{StudentId=s.Id,Name=row?.Name??s.FullName,Role=row?.Role??"NotSelected",CoachRating=row?.CoachRating,CoachNote=row?.CoachNote,PitchSlot=row?.PitchSlot,Intervals=row?.Stints??[],Care=notice==null?null:CareLabels.Status(notice.Status)};
        }).ToList();
        return input;
    }
    public static Dictionary<int,int> ValidateMinutes(MatchInput input)
    {
        Validator.ValidateObject(input,new(input),true);
        if(input.Date<new DateOnly(1900,1,1)||input.Date>new DateOnly(2200,12,31))throw new ValidationException("Zgjidhni datën e ndeshjes.");
        if(input.Status is not("Draft" or "Live" or "Completed" or "Cancelled")||input.Kind is not("Official" or "Cup" or "Friendly"))throw new ValidationException("Zgjidhni llojin dhe statusin.");
        if(input.Status=="Completed"&&(input.Date>BillingClock.Today||input.ScoreFor==null||input.ScoreAgainst==null))throw new ValidationException("Ndeshja e përfunduar kërkon datë deri sot dhe rezultatin.");
        if(input.Players==null||input.Players.Count>1000||input.Players.Any(p=>p==null)||input.Players.Select(p=>p.StudentId).Distinct().Count()!=input.Players.Count)throw new ValidationException("Lista e lojtarëve është e pavlefshme. Rihapeni.");
        if(input.Players.Any(p=>p.PitchSlot is <0 or >29))throw new ValidationException("Pozicioni në fushë nuk është i vlefshëm.");
        var slots=input.Players.Where(p=>p.Role=="Starter"&&p.PitchSlot.HasValue).Select(p=>p.PitchSlot!.Value).ToList();
        if(slots.Distinct().Count()!=slots.Count||slots.Any(s=>s>=input.PlayersOnCourt))throw new ValidationException("Kontrolloni vendosjen e titullarëve në fushë.");
        var minutes=new Dictionary<int,int>();var events=new List<(int Minute,int Change)>();
        foreach(var player in input.Players){
            Validator.ValidateObject(player,new(player),true);
            if(player.Role is not("NotSelected" or "Unused" or "Starter" or "Substitute"))throw new ValidationException("Zgjidhni rolin e lojtarit.");
            if(player.Intervals==null||player.Intervals.Count>50||player.Intervals.Any(x=>x==null))throw new ValidationException("Kontrolloni hyrjet dhe daljet.");
            var intervals=player.Intervals.Where(x=>x.From.HasValue||x.To.HasValue).OrderBy(x=>x.From).ToList();
            if((player.Role is "NotSelected" or "Unused")&&intervals.Count>0)throw new ValidationException("Lojtari jashtë lojës nuk mund të ketë minuta. Kontrolloni rolin.");
            if(input.Status=="Completed"&&(player.Role is "Starter" or "Substitute")&&intervals.Count==0)throw new ValidationException("Plotësoni hyrjen dhe daljen për çdo lojtar që ka luajtur.");
            var end=-1;var total=0;
            foreach(var stint in intervals){
                if(stint.From==null||stint.To==null||stint.From<0||stint.To>input.Duration||stint.From>=stint.To||stint.From<end)throw new ValidationException("Minutat duhet të jenë brenda kohëzgjatjes, hyrja para daljes dhe pa mbivendosje.");
                end=stint.To.Value;total+=stint.To.Value-stint.From.Value;
                events.Add((stint.From.Value,1));events.Add((stint.To.Value,-1));
            }
            if(intervals.Count>0&&((player.Role=="Starter"&&intervals[0].From!=0)||(player.Role=="Substitute"&&intervals[0].From==0)))throw new ValidationException("Titullari fillon në minutën 0; lojtari nga stoli hyn pas fillimit.");
            player.Intervals=intervals;minutes[player.StudentId]=total;
        }
        if(input.Players.Count(p=>p.Role=="Starter")>input.PlayersOnCourt)throw new ValidationException("Ka më shumë titullarë se lojtarë të lejuar në fushë.");
        if(input.Status=="Completed"&&!input.Players.Any(p=>p.Role=="Starter"&&minutes[p.StudentId]>0))throw new ValidationException("Regjistroni formacionin fillestar para përfundimit.");
        var count=0;
        foreach(var point in events.OrderBy(e=>e.Minute).ThenBy(e=>e.Change)){count+=point.Change;if(count>input.PlayersOnCourt)throw new ValidationException("Në një interval ka më shumë lojtarë në fushë se numri i lejuar. Kontrolloni zëvendësimet.");}
        return minutes;
    }
    public async Task<int> Save(string actor,MatchInput input)
    {
        if(input.Events==null)throw new ValidationException("Kontrolloni ngjarjet.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        await Team(input.TeamId,actor);
        if(input.RequestId==Guid.Empty)throw new ValidationException("Rihapni formularin.");
        if(input.Id==0){var duplicate=await db.MatchGames.AsNoTracking().SingleOrDefaultAsync(m=>m.RequestId==input.RequestId);if(duplicate!=null){await Team(duplicate.TrainingTeamId,actor);return duplicate.Id;}}
        var game=input.Id==0?new MatchGame{TrainingTeamId=input.TeamId,RequestId=input.RequestId,Sport=await new SportDevelopmentService(db).Sport()}:
            await db.MatchGames.Include(m=>m.Players).SingleOrDefaultAsync(m=>m.Id==input.Id&&m.TrainingTeamId==input.TeamId)??throw new UnauthorizedAccessException();
        if(input.Id!=0&&game.Revision!=input.Revision)throw new ValidationException("Ndeshja është ndryshuar nga një trajner tjetër. Rihapeni para ruajtjes.");
        if(input.Id!=0&&game.Status!="Draft"&&game.LeagueId!=input.LeagueId)throw new ValidationException("Liga nuk ndryshohet pas konfirmimit.");
        League? league=null;
        if(input.LeagueId.HasValue){
            league=await db.Leagues.SingleOrDefaultAsync(l=>l.Id==input.LeagueId&&l.Teams.Any(t=>t.TrainingTeamId==input.TeamId));
            if(league==null||input.Date<league.StartsOn||input.Date>league.EndsOn||(!league.IsActive&&game.LeagueId!=league.Id))throw new ValidationException("Zgjidhni një ligë aktive të ekipit për datën e ndeshjes.");
        }
        input.Kind=league?.Kind??(input.Id!=0&&game.LeagueId==null?game.Kind:input.Kind);
        var minutes=ValidateMinutes(input);
        game.LeagueId=input.LeagueId;
        if(game.Sport=="Football"&&input.Id!=0){
            if(game.Status=="Draft"&&input.Status=="Completed")throw new ValidationException("Konfirmoni formacionin para përfundimit të ndeshjes.");
            if(game.Status=="Draft"&&input.Status=="Live"){
                if(!input.Players.Any(p=>p.Role=="Starter")||input.Players.Any(p=>p.Role=="Substitute")||input.Events.Count>0||input.Players.Where(p=>p.Role=="Starter").Any(p=>p.Intervals.Count!=1||p.Intervals[0].From!=0||p.Intervals[0].To!=input.Duration))throw new ValidationException("Zgjidhni titullarët për formacionin bazë, pa ngjarje ose ndërrime.");
            }
            if(game.Status=="Live"){
                if(input.Status=="Draft")throw new ValidationException("Formacioni tashmë është konfirmuar.");
                var original=game.Players.Where(p=>p.Role=="Starter").Select(p=>p.StudentId).Order().ToArray();
                var posted=input.Players.Where(p=>p.Role=="Starter").Select(p=>p.StudentId).Order().ToArray();
                if(!original.SequenceEqual(posted)||game.Formation!=input.Formation||game.PlayersOnCourt!=input.PlayersOnCourt)throw new ValidationException("Formacioni bazë është konfirmuar. Përdorni ndërrimin për hyrje dhe dalje.");
            }
            if(input.Status=="Draft"&&input.Events.Count>0)throw new ValidationException("Konfirmoni formacionin para regjistrimit të ngjarjeve.");
        }
        FootballService.Validate(input,game.Sport);
        game.EventData=JsonSerializer.Serialize(input.Events);
        var ids=input.Players.Select(p=>p.StudentId).ToList();var old=game.Players.ToDictionary(p=>p.StudentId);
        var students=await db.Students.Where(s=>ids.Contains(s.Id)).ToDictionaryAsync(s=>s.Id);
        foreach(var p in input.Players)
            if(!students.TryGetValue(p.StudentId,out var s)||(!old.ContainsKey(p.StudentId)&&(s.TrainingTeamId!=input.TeamId||!s.IsActive)))throw new UnauthorizedAccessException();
        var notices=await new PlayerCareService(db).TrainingNotices(ids,input.Date);
        if(input.Status!="Cancelled"&&notices.Any(n=>n.Status=="Unavailable"&&input.Players.Any(p=>p.StudentId==n.StudentId&&(p.Role is "Starter" or "Substitute"))))throw new ValidationException("Një lojtar ka ndalim mjekësor në datën e ndeshjes. Nuk mund të shënohet në lojë.");
        game.Opponent=input.Opponent.Trim();game.Competition=league?.Name??input.Competition?.Trim();game.Venue=input.Venue?.Trim();game.Kind=input.Kind;game.Status=input.Status;game.Date=input.Date;game.Kickoff=input.Kickoff;game.Home=input.Home;
        game.Formation=input.Formation;game.Duration=input.Duration;game.PlayersOnCourt=input.PlayersOnCourt;game.ScoreFor=input.ScoreFor;game.ScoreAgainst=input.ScoreAgainst;game.UpdatedById=actor;game.UpdatedAt=DateTime.UtcNow;game.Revision=Guid.NewGuid();
        foreach(var removed in game.Players.Where(p=>!input.Players.Any(i=>i.StudentId==p.StudentId&&i.Role!="NotSelected")).ToList()){db.MatchPlayers.Remove(removed);game.Players.Remove(removed);}
        foreach(var p in input.Players.Where(p=>p.Role!="NotSelected")){
            var row=game.Players.SingleOrDefault(x=>x.StudentId==p.StudentId);
            if(row==null){row=new(){StudentId=p.StudentId,Name=students[p.StudentId].FullName};game.Players.Add(row);}
            row.CoachRating=p.CoachRating;row.CoachNote=string.IsNullOrWhiteSpace(p.CoachNote)?null:p.CoachNote.Trim();
            row.PitchSlot=p.PitchSlot;row.Role=p.Role;row.Minutes=minutes[p.StudentId];row.Intervals=JsonSerializer.Serialize(p.Intervals);
        }
        if(input.Id==0)db.MatchGames.Add(game);
        db.MatchChanges.Add(new(){MatchGame=game,ActorId=actor,CreatedAt=DateTime.UtcNow,Snapshot=JsonSerializer.Serialize(input)});
        await db.SaveChangesAsync();await tx.CommitAsync();return game.Id;
    }
    public async Task<MatchReport> Report(string actor,int? teamId,int year,string? kind,int? studentId,int? leagueId=null)
    {
        var list=await List(actor,teamId,year,kind);
        var teamIds=list.Teams.Where(t=>!teamId.HasValue||t.Id==teamId).Select(t=>t.Id).ToList();
        var leagues=await db.Leagues.AsNoTracking().Where(l=>l.Teams.Any(t=>teamIds.Contains(t.TrainingTeamId))).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync();
        if(leagueId.HasValue&&!leagues.Any(l=>l.Id==leagueId))throw new UnauthorizedAccessException();
        if(studentId.HasValue&&!list.Matches.Any(m=>m.Players.Any(p=>p.StudentId==studentId))&&!await new CoachChatService(db).Assigned(actor).AnyAsync(s=>s.Id==studentId))throw new UnauthorizedAccessException();
        var games=list.Matches.Where(g=>g.Status=="Completed"&&(!leagueId.HasValue||g.LeagueId==leagueId)&&(!studentId.HasValue||g.Players.Any(p=>p.StudentId==studentId))).ToList();
        var totals=games.SelectMany(g=>g.Players.Where(p=>!studentId.HasValue||p.StudentId==studentId).Select(p=>new{Game=g,Player=p})).GroupBy(x=>x.Player.StudentId)
            .Select(g=>new MatchPlayerTotal(g.Key,g.First().Player.Name,g.Count(x=>x.Player.Minutes>0),g.Count(x=>x.Player.Role=="Starter"),g.Count(x=>x.Player.Role=="Substitute"),g.Count(x=>x.Player.Role=="Unused"),g.Where(x=>x.Game.Kind=="Official").Sum(x=>x.Player.Minutes),g.Where(x=>x.Game.Kind=="Friendly").Sum(x=>x.Player.Minutes)) { Goals=g.Sum(x=>x.Game.Events.Count(e=>e.PlayerId==g.Key&&(e.Kind is "Goal" or "PenaltyGoal"))), Assists=g.Sum(x=>x.Game.Events.Count(e=>e.AssistId==g.Key&&e.Kind=="Goal")), YellowCards=g.Sum(x=>x.Game.Events.Count(e=>e.PlayerId==g.Key&&(e.Kind is "Yellow" or "SecondYellow"))), RedCards=g.Sum(x=>x.Game.Events.Count(e=>e.PlayerId==g.Key&&FootballLabels.Dismissal(e))), CupMinutes=g.Where(x=>x.Game.Kind=="Cup").Sum(x=>x.Player.Minutes) })
            .OrderByDescending(x=>x.OfficialMinutes+x.CupMinutes+x.FriendlyMinutes).ThenBy(x=>x.Name).ToList();
        return new(){Teams=list.Teams,TeamId=teamId,Year=year,Kind=kind,LeagueId=leagueId,Leagues=leagues,Matches=games,StudentId=studentId,Totals=totals};
    }
}
