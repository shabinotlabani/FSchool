using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class MatchService(ApplicationDbContext db,bool enforceOfficialRest=true)
{
    public IQueryable<TrainingTeam> Teams(string actor,bool manageAll=false) => db.TrainingTeams.Where(t=>t.IsActive&&(manageAll||t.CoachId==actor||t.AssistantCoachId==actor||db.CoachTeams.Any(x=>x.TrainingTeamId==t.Id&&x.UserId==actor)));
    private async Task<TrainingTeam> Team(int id,string actor,bool manageAll=false) => await Teams(actor,manageAll).AsNoTracking().SingleOrDefaultAsync(t=>t.Id==id) ?? throw new UnauthorizedAccessException();
    public async Task<PlayerMatchMinutes> PlayerMinutes(string actor,int studentId,int? year=null)
    {
        await new PlayerDevelopmentService(db).Player(studentId,actor);
        var teams=Teams(actor).Select(t=>t.Id);
        var minutes=await db.MatchPlayers.AsNoTracking().Where(p=>p.StudentId==studentId&&p.MatchGame.Status=="Completed"&&(teams.Contains(p.MatchGame.TrainingTeamId)||(p.IsCallUp&&p.HomeTeamId!=null&&teams.Contains(p.HomeTeamId.Value)))&&(!year.HasValue||p.MatchGame.Date.Year==year)&&p.Minutes>0).Select(p=>p.Minutes).ToListAsync();
        return new(minutes.Sum(),minutes.Count);
    }
    public async Task<MatchList> List(string actor,int? teamId,int year,string? kind)
    {
        if(year is <1900 or >2200 || kind is not(null or "Official" or "Cup" or "Friendly"))throw new ValidationException("Kontrolloni filtrat.");
        var teams=await Teams(actor).AsNoTracking().OrderBy(t=>t.Name).ToListAsync();
        if(teamId.HasValue&&!teams.Any(t=>t.Id==teamId))throw new UnauthorizedAccessException();
        var ids=teams.Select(t=>t.Id).ToArray();
        var games=await db.MatchGames.AsNoTracking().Include(m=>m.TrainingTeam).Include(m=>m.Players)
            .Where(m=>(ids.Contains(m.TrainingTeamId)||m.Players.Any(p=>p.IsCallUp&&p.HomeTeamId!=null&&ids.Contains(p.HomeTeamId.Value)))&&(!teamId.HasValue||m.TrainingTeamId==teamId||m.Players.Any(p=>p.IsCallUp&&p.HomeTeamId==teamId))&&m.Date.Year==year&&(kind==null||m.Kind==kind))
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
    public async Task<MatchInput> Load(string actor,int id,int teamId,bool manageAll=false)
    {
        MatchGame? game=null;
        if(id!=0){game=await db.MatchGames.AsNoTracking().Include(m=>m.Players).SingleOrDefaultAsync(m=>m.Id==id)??throw new UnauthorizedAccessException();teamId=game.TrainingTeamId;}
        var team=await Team(teamId,actor,manageAll);
        var sport=game?.Sport??await new SportDevelopmentService(db).Sport();
        var input=game==null?new MatchInput{TeamId=teamId,Sport=sport,Duration=sport=="Basketball"?40:sport=="Handball"?60:90,PlayersOnCourt=sport switch{"Basketball"=>5,"Volleyball"=>6,"Handball"=>7,"Tennis"=>1,_=>11}}:
            new MatchInput{Id=game.Id,RequestId=game.RequestId,Revision=game.Revision,TeamId=teamId,Sport=sport,Opponent=game.Opponent,Competition=game.Competition,CompetitionStage=game.CompetitionStage,Venue=game.Venue,Date=game.Date,Kickoff=game.Kickoff,Kind=game.Kind,Status=game.Status,Home=game.Home,Duration=game.Duration,PlayersOnCourt=game.PlayersOnCourt,ScoreFor=game.ScoreFor,ScoreAgainst=game.ScoreAgainst,Formation=game.Formation};
        input.TeamName=team.Name;
        input.LeagueId=game?.LeagueId;
        input.Leagues=await db.Leagues.AsNoTracking().Where(l=>(l.IsActive&&l.Teams.Any(t=>t.TrainingTeamId==teamId&&(t.Status=="Registered"||t.Status=="Qualified")))||l.Id==input.LeagueId).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync();
        if(game==null)input.LeagueId=await new LeagueService(db).Eligible(teamId,input.Date).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).Select(l=>(int?)l.Id).FirstOrDefaultAsync();
        if(game==null)input.Kind=input.LeagueId.HasValue?input.Leagues.Single(l=>l.Id==input.LeagueId).Kind:"Friendly";
        if(game==null&&input.LeagueId.HasValue)input.CompetitionStage=input.Leagues.Single(l=>l.Id==input.LeagueId).Stage;
        input.Events=game?.Events??[];
        if(sport=="Football"){input.DisciplineAlerts=await new FootballService(db).Alerts(teamId,input.Kind,input.Date,input.Kickoff,input.Id,input.LeagueId);input.HasDisciplineRule=await new FootballService(db).Rule(teamId,input.Kind,input.Date,input.LeagueId)!=null;}
        var stored=game?.Players.ToDictionary(p=>p.StudentId)??[];
        var students=await db.Students.AsNoTracking().Where(s=>(s.TrainingTeamId==teamId&&s.IsActive)||stored.Keys.Contains(s.Id)).OrderBy(s=>s.FirstName).ThenBy(s=>s.LastName).ToListAsync();
        var care=await new PlayerCareService(db).TrainingNotices(students.Select(s=>s.Id).ToList(),input.Date);
        var teamNames=await db.TrainingTeams.AsNoTracking().ToDictionaryAsync(t=>t.Id,t=>t.Name);
        input.Players=students.Select(s=>{
            stored.TryGetValue(s.Id,out var row);
            var notice=care.FirstOrDefault(c=>c.StudentId==s.Id);
            return new MatchPlayerInput{StudentId=s.Id,Name=row?.Name??s.FullName,Role=row?.Role??"NotSelected",CoachRating=row?.CoachRating,CoachNote=row?.CoachNote,PitchSlot=row?.PitchSlot,Intervals=row?.Stints??[],Care=notice==null?null:CareLabels.Status(notice.Status),IsCallUp=row?.IsCallUp??false,HomeTeamName=row?.HomeTeamId is int home?teamNames.GetValueOrDefault(home):null};
        }).ToList();
        if(game!=null&&game.Status=="Draft")input.CallUpCandidates=await db.Students.AsNoTracking().Where(s=>s.IsActive&&s.TrainingTeamId!=null&&s.TrainingTeamId!=teamId&&!stored.Keys.Contains(s.Id)).OrderBy(s=>s.TrainingTeam!.Name).ThenBy(s=>s.FirstName).ThenBy(s=>s.LastName).Select(s=>new MatchCallUpCandidate(s.Id,s.FirstName+" "+s.LastName,s.TrainingTeam!.Name)).ToListAsync();
        return input;
    }
    private static DateTime Moment(DateOnly date,TimeOnly? time)=>date.ToDateTime(time??TimeOnly.MinValue);
    private async Task EnsureOfficialRest(int matchId,DateOnly date,TimeOnly? kickoff,IEnumerable<int> studentIds)
    {
        var ids=studentIds.Distinct().ToList();if(ids.Count==0)return;
        var moment=Moment(date,kickoff);var from=DateOnly.FromDateTime(moment.AddHours(-48));var to=DateOnly.FromDateTime(moment.AddHours(48));
        var conflicts=await db.MatchPlayers.AsNoTracking().Where(p=>ids.Contains(p.StudentId)&&p.MatchGameId!=matchId&&(p.MatchGame.Status=="Live"||p.MatchGame.Status=="Completed")&&(p.MatchGame.Kind=="Official"||p.MatchGame.Kind=="Cup")&&(p.Role=="Starter"||p.Role=="Substitute")&&p.MatchGame.Date>=from&&p.MatchGame.Date<=to)
            .Select(p=>new{p.StudentId,p.Name,p.MatchGame.Date,p.MatchGame.Kickoff,TeamName=p.MatchGame.TrainingTeam.Name}).ToListAsync();
        var conflict=conflicts.FirstOrDefault(x=>Math.Abs((Moment(x.Date,x.Kickoff)-moment).TotalHours)<48);
        if(conflict!=null)throw new ValidationException($"{conflict.Name} ka ndeshje zyrtare me {conflict.TeamName} më {conflict.Date:dd/MM/yyyy} dhe nuk mund të luajë dy ndeshje zyrtare brenda 48 orëve.");
    }
    public async Task CallUp(string actor,int matchId,int studentId,string? reason,bool manageAll=false)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        var game=await db.MatchGames.Include(g=>g.Players).SingleOrDefaultAsync(g=>g.Id==matchId)??throw new UnauthorizedAccessException();
        await Team(game.TrainingTeamId,actor,manageAll);
        if(game.Status!="Draft")throw new ValidationException("Lojtari mund të thirret vetëm para konfirmimit të formacionit.");
        var student=await db.Students.Include(s=>s.TrainingTeam).SingleOrDefaultAsync(s=>s.Id==studentId&&s.IsActive&&s.TrainingTeamId!=null&&s.TrainingTeamId!=game.TrainingTeamId)??throw new ValidationException("Zgjidhni një lojtar aktiv nga ekip tjetër.");
        if(game.LeagueId.HasValue){
            var registrationId=await db.LeagueTeams.Where(x=>x.LeagueId==game.LeagueId&&x.TrainingTeamId==game.TrainingTeamId).Select(x=>(int?)x.Id).SingleAsync();
            if(await db.CompetitionPlayerRegistrations.AnyAsync(x=>x.LeagueTeamId==registrationId&&x.Status=="Eligible")&&!await db.CompetitionPlayerRegistrations.AnyAsync(x=>x.LeagueTeamId==registrationId&&x.StudentId==studentId&&x.Status=="Eligible"&&(!x.EligibleFrom.HasValue||x.EligibleFrom<=game.Date)&&(!x.EligibleUntil.HasValue||x.EligibleUntil>=game.Date)))throw new ValidationException("Lojtari nuk është në listën e kësaj gare.");
        }
        if(game.Players.Any(p=>p.StudentId==studentId))throw new ValidationException("Lojtari është tashmë në listën e ndeshjes.");
        game.Players.Add(new MatchPlayer{StudentId=student.Id,Name=student.FullName,Role="Unused",IsCallUp=true,HomeTeamId=student.TrainingTeamId,CallUpReason=string.IsNullOrWhiteSpace(reason)?null:reason.Trim()});
        game.Revision=Guid.NewGuid();game.UpdatedAt=DateTime.UtcNow;game.UpdatedById=actor;
        db.MatchChanges.Add(new(){MatchGameId=game.Id,ActorId=actor,CreatedAt=game.UpdatedAt,Snapshot=JsonSerializer.Serialize(new{Action="CallUp",student.Id,student.FullName,HomeTeamId=student.TrainingTeamId,Reason=reason?.Trim()})});
        await db.SaveChangesAsync();await tx.CommitAsync();
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
    public async Task<int> Save(string actor,MatchInput input,bool manageAll=false)
    {
        if(input.Events==null)throw new ValidationException("Kontrolloni ngjarjet.");
        input.Players??=[];
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        await Team(input.TeamId,actor,manageAll);
        if(input.RequestId==Guid.Empty)throw new ValidationException("Rihapni formularin.");
        if(input.Id==0){var duplicate=await db.MatchGames.AsNoTracking().SingleOrDefaultAsync(m=>m.RequestId==input.RequestId);if(duplicate!=null){await Team(duplicate.TrainingTeamId,actor,manageAll);return duplicate.Id;}}
        var game=input.Id==0?new MatchGame{TrainingTeamId=input.TeamId,RequestId=input.RequestId,Sport=await new SportDevelopmentService(db).Sport()}:
            await db.MatchGames.Include(m=>m.Players).SingleOrDefaultAsync(m=>m.Id==input.Id&&m.TrainingTeamId==input.TeamId)??throw new UnauthorizedAccessException();
        if(input.Id!=0&&game.Revision!=input.Revision)throw new ValidationException("Ndeshja është ndryshuar nga një trajner tjetër. Rihapeni para ruajtjes.");
        if(!manageAll&&input.Id!=0&&game.Status!="Draft"&&game.LeagueId!=input.LeagueId)throw new ValidationException("Liga nuk ndryshohet pas konfirmimit.");
        League? league=null;
        if(input.LeagueId.HasValue){
            league=await db.Leagues.SingleOrDefaultAsync(l=>l.Id==input.LeagueId&&l.Teams.Any(t=>t.TrainingTeamId==input.TeamId&&((t.Status=="Registered"||t.Status=="Qualified")||(input.Id!=0&&game.LeagueId==l.Id))));
            if(league==null||input.Date<league.StartsOn||input.Date>league.EndsOn||(!league.IsActive&&game.LeagueId!=league.Id))throw new ValidationException("Zgjidhni një ligë aktive të ekipit për datën e ndeshjes.");
        }
        if(input.LeagueId.HasValue){
            var registrationId=await db.LeagueTeams.Where(x=>x.LeagueId==input.LeagueId&&x.TrainingTeamId==input.TeamId).Select(x=>(int?)x.Id).SingleAsync();
            var hasSquad=await db.CompetitionPlayerRegistrations.AnyAsync(x=>x.LeagueTeamId==registrationId&&x.Status=="Eligible");
            if(hasSquad){
                var eligible=await db.CompetitionPlayerRegistrations.Where(x=>x.LeagueTeamId==registrationId&&x.Status=="Eligible"&&(!x.EligibleFrom.HasValue||x.EligibleFrom<=input.Date)&&(!x.EligibleUntil.HasValue||x.EligibleUntil>=input.Date)).Select(x=>x.StudentId).Distinct().ToListAsync();
                if(input.Players.Any(x=>x.Role!="NotSelected"&&!eligible.Contains(x.StudentId)))throw new ValidationException("Lista e ndeshjes përmban lojtar pa të drejtë loje në këtë garë.");
            }
        }
        input.Kind=league?.Kind??(input.Id!=0&&game.LeagueId==null?game.Kind:input.Kind);
        var minutes=ValidateMinutes(input);
        game.LeagueId=input.LeagueId;
        if(!manageAll&&game.Sport=="Football"&&input.Id!=0){
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
        if(enforceOfficialRest&&(input.Status is "Live" or "Completed")&&(input.Kind is "Official" or "Cup"))await EnsureOfficialRest(game.Id,input.Date,input.Kickoff,input.Players.Where(p=>p.Role is "Starter" or "Substitute").Select(p=>p.StudentId));
        game.Opponent=input.Opponent.Trim();game.Competition=league?.Name??input.Competition?.Trim();game.CompetitionStage=string.IsNullOrWhiteSpace(input.CompetitionStage)?league?.Stage:input.CompetitionStage;game.Venue=input.Venue?.Trim();game.Kind=input.Kind;game.Status=input.Status;game.Date=input.Date;game.Kickoff=input.Kickoff;game.Home=input.Home;
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
    public async Task<MatchReport> Report(string actor,int? teamId,int year,string? kind,int? studentId,int? leagueId=null,string? competitionFilter=null)
    {
        if(competitionFilter is not(null or "DomesticLeague" or "DomesticCup" or "International" or "UEFA"))throw new ValidationException("Kontrolloni filtrin e garës.");
        var list=await List(actor,teamId,year,kind);
        var teamIds=list.Teams.Where(t=>!teamId.HasValue||t.Id==teamId).Select(t=>t.Id).ToList();
        var leagues=await db.Leagues.AsNoTracking().Where(l=>l.Teams.Any(t=>teamIds.Contains(t.TrainingTeamId))).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync();
        if(leagueId.HasValue&&!leagues.Any(l=>l.Id==leagueId))throw new UnauthorizedAccessException();
        if(studentId.HasValue&&!list.Matches.Any(m=>m.Players.Any(p=>p.StudentId==studentId))&&!await new CoachChatService(db).Assigned(actor).AnyAsync(s=>s.Id==studentId))throw new UnauthorizedAccessException();
        var filteredLeagueIds=leagues.Where(l=>competitionFilter switch{"DomesticLeague"=>l.CompetitionScope=="Domestic"&&l.CompetitionType=="League","DomesticCup"=>l.CompetitionScope=="Domestic"&&(l.CompetitionType=="Cup"||l.CompetitionType=="SuperCup"),"International"=>l.CompetitionScope=="International","UEFA"=>l.Organizer=="UEFA",_=>true}).Select(l=>l.Id).ToHashSet();
        var games=list.Matches.Where(g=>g.Status=="Completed"&&(!leagueId.HasValue||g.LeagueId==leagueId)&&(competitionFilter==null||(g.LeagueId.HasValue&&filteredLeagueIds.Contains(g.LeagueId.Value)))&&(!studentId.HasValue||g.Players.Any(p=>p.StudentId==studentId))).ToList();
        var totals=games.SelectMany(g=>g.Players.Where(p=>!studentId.HasValue||p.StudentId==studentId).Select(p=>new{Game=g,Player=p})).GroupBy(x=>x.Player.StudentId)
            .Select(g=>new MatchPlayerTotal(g.Key,g.First().Player.Name,g.Count(x=>x.Player.Minutes>0),g.Count(x=>x.Player.Role=="Starter"),g.Count(x=>x.Player.Role=="Substitute"),g.Count(x=>x.Player.Role=="Unused"),g.Where(x=>x.Game.Kind=="Official").Sum(x=>x.Player.Minutes),g.Where(x=>x.Game.Kind=="Friendly").Sum(x=>x.Player.Minutes)) { Goals=g.Sum(x=>x.Game.Events.Count(e=>e.PlayerId==g.Key&&(e.Kind is "Goal" or "PenaltyGoal"))), Assists=g.Sum(x=>x.Game.Events.Count(e=>e.AssistId==g.Key&&e.Kind=="Goal")), YellowCards=g.Sum(x=>x.Game.Events.Count(e=>e.PlayerId==g.Key&&(e.Kind is "Yellow" or "SecondYellow"))), RedCards=g.Sum(x=>x.Game.Events.Count(e=>e.PlayerId==g.Key&&FootballLabels.Dismissal(e))), CupMinutes=g.Where(x=>x.Game.Kind=="Cup").Sum(x=>x.Player.Minutes) })
            .OrderByDescending(x=>x.OfficialMinutes+x.CupMinutes+x.FriendlyMinutes).ThenBy(x=>x.Name).ToList();
        return new(){Teams=list.Teams,TeamId=teamId,Year=year,Kind=kind,LeagueId=leagueId,CompetitionFilter=competitionFilter,Leagues=leagues,Matches=games,StudentId=studentId,Totals=totals};
    }
}
