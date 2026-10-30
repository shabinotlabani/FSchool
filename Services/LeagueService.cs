using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Services;

public class LeagueService(ApplicationDbContext db)
{
    public static (string Name,DateOnly StartsOn,DateOnly EndsOn) CurrentSeason(DateOnly? day=null)
    {
        var today=day??BillingClock.Today;var year=today.Month>=7?today.Year:today.Year-1;
        return($"{year}/{(year+1).ToString()[2..]}",new(year,7,1),new(year+1,6,30));
    }
    public static readonly IReadOnlyList<CompetitionCatalogItem> Catalog = new CompetitionCatalogItem[]{
        new("ffk-superliga","Kosovë","Superliga e Kosovës","FFK","Kosovë","Male","Senior","Domestic","League","LeaguePhase"),
        new("ffk-first","Kosovë","Liga e Parë e Kosovës","FFK","Kosovë","Male","Senior","Domestic","League","LeaguePhase"),
        new("ffk-second","Kosovë","Liga e Dytë e Kosovës","FFK","Kosovë","Male","Senior","Domestic","League","LeaguePhase"),
        new("ffk-cup","Kosovë","Kupa e Kosovës","FFK","Kosovë","Male","Senior","Domestic","Cup","Knockout"),
        new("ffk-women","Kosovë","Liga e Femrave e Kosovës","FFK","Kosovë","Female","Senior","Domestic","League","LeaguePhase"),
        new("ffk-women-cup","Kosovë","Kupa e Kosovës për Femra","FFK","Kosovë","Female","Senior","Domestic","Cup","Knockout"),
        new("ffk-u21","Kosovë","Liga U21 e Kosovës","FFK","Kosovë","Male","U21","Domestic","League","LeaguePhase"),
        new("ffk-u19","Kosovë","Liga U19 e Kosovës","FFK","Kosovë","Male","U19","Domestic","League","LeaguePhase"),
        new("ffk-u17","Kosovë","Liga U17 e Kosovës","FFK","Kosovë","Male","U17","Domestic","League","LeaguePhase"),
        new("uefa-ucl","UEFA","UEFA Champions League","UEFA","International","Male","Senior","International","League","Qualifying"),
        new("uefa-uel","UEFA","UEFA Europa League","UEFA","International","Male","Senior","International","League","Qualifying"),
        new("uefa-uecl","UEFA","UEFA Conference League","UEFA","International","Male","Senior","International","League","Qualifying"),
        new("uefa-super","UEFA","UEFA Super Cup","UEFA","International","Male","Senior","International","SuperCup","Final"),
        new("uefa-wcl","UEFA","UEFA Women's Champions League","UEFA","International","Female","Senior","International","League","Qualifying"),
        new("uefa-wec","UEFA","UEFA Women's Europa Cup","UEFA","International","Female","Senior","International","Cup","Qualifying"),
        new("uefa-youth","UEFA","UEFA Youth League","UEFA","International","Male","U19","International","Tournament","Qualifying"),
        new("uefa-regions","UEFA","UEFA Regions' Cup","UEFA","International","Male","Senior","Regional","Tournament","Qualifying"),
        new("fifa-cwc","FIFA","FIFA Club World Cup","FIFA","International","Male","Senior","International","Tournament","GroupStage"),
        new("fifa-intercontinental","FIFA","FIFA Intercontinental Cup","FIFA","International","Male","Senior","International","Tournament","Knockout")
    };
    public IQueryable<League> Eligible(int team,DateOnly date) => db.Leagues.Where(l=>l.IsActive&&l.StartsOn<=date&&l.EndsOn>=date&&l.Teams.Any(t=>t.TrainingTeamId==team&&(t.Status=="Registered"||t.Status=="Qualified")));
    public async Task Save(LeagueInput input,string actor)
    {
        Validator.ValidateObject(input,new(input),true);
        if(input.TeamIds==null)throw new ValidationException("Kontrolloni ekipet.");
        input.TeamStatuses??=[];
        input.TeamDeadlines??=[];
        if(string.IsNullOrWhiteSpace(input.Name)||input.StartsOn.Year<1900||input.EndsOn.Year>2200||input.EndsOn<input.StartsOn)throw new ValidationException("Kontrolloni emrin dhe datat e sezonit.");
        var current=CurrentSeason();
        if(input.Id==0&&(input.Season.Trim()!=current.Name||input.StartsOn<current.StartsOn||input.EndsOn>current.EndsOn))throw new ValidationException($"Aktualisht mund të krijohen gara vetëm për sezonin {current.Name}.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var ids=input.TeamIds.Distinct().ToList();
        var selectedTeams=await db.TrainingTeams.Where(t=>ids.Contains(t.Id)).ToListAsync();
        if(selectedTeams.Count!=ids.Count)throw new ValidationException("Zgjidhni ekipet e këtij klubi.");
        if(selectedTeams.Any(t=>t.Gender!="Mixed"&&input.Gender!="Mixed"&&t.Gender!=input.Gender))throw new ValidationException("Gjinia e ekipit duhet të përputhet me garën.");
        var statuses=new[]{"Registered","Pending","Qualified","Withdrawn","Eliminated"};
        if(input.TeamStatuses.Values.Any(x=>!statuses.Contains(x)))throw new ValidationException("Kontrolloni statusin e pjesëmarrjes.");
        var row=input.Id==0?new League():await db.Leagues.Include(l=>l.Teams).SingleOrDefaultAsync(l=>l.Id==input.Id)??throw new UnauthorizedAccessException();
        if(input.Id!=0&&input.Season.Trim()!=row.Season)throw new ValidationException("Sezoni i garës nuk mund të ndryshohet pas krijimit.");
        input.Kind=input.CompetitionType=="Friendly"?"Friendly":input.CompetitionType is "Cup" or "SuperCup"?"Cup":"Official";
        if(input.IsOfficial&&!input.IsVerified)throw new ValidationException("Gara mund të shënohet zyrtare vetëm pasi të verifikohet.");
        if(input.Id!=0&&row.Revision!=input.Revision)throw new ValidationException("Liga ndryshoi. Rihapeni para ruajtjes.");
        if(await db.Leagues.AnyAsync(l=>l.Id!=input.Id&&l.Name.ToUpper()==input.Name.Trim().ToUpper()&&l.Season==input.Season.Trim()))throw new ValidationException("Kjo garë ekziston për këtë sezon.");
        if(input.Id!=0&&await db.MatchGames.AnyAsync(g=>g.LeagueId==input.Id&&(g.Kind!=input.Kind||g.Date<input.StartsOn||g.Date>input.EndsOn||!ids.Contains(g.TrainingTeamId))))throw new ValidationException("Ruani ekipet, llojin dhe datat që përfshijnë ndeshjet e regjistruara.");
        if(input.Id!=0&&await db.CompetitionPlayerRegistrations.AnyAsync(x=>x.LeagueTeam.LeagueId==input.Id&&!ids.Contains(x.LeagueTeam.TrainingTeamId)))throw new ValidationException("Ekipi ka listë lojtarësh në këtë garë. Ndryshoni statusin e pjesëmarrjes në vend që ta hiqni.");
        row.Name=input.Name.Trim();row.Kind=input.Kind;row.Country=input.Country.Trim();row.Organizer=input.Organizer.Trim();row.Sport=input.Sport;row.Gender=input.Gender;row.Season=input.Season.Trim();
        row.AgeCategory=input.AgeCategory.Trim();row.CompetitionScope=input.CompetitionScope;row.CompetitionType=input.CompetitionType;row.Division=input.Division?.Trim();row.Group=input.Group?.Trim();row.Stage=input.Stage;row.IsOfficial=input.IsOfficial;row.IsVerified=input.IsVerified;
        row.StartsOn=input.StartsOn;row.EndsOn=input.EndsOn;row.IsActive=input.IsActive;row.RulesConfirmed=true;
        row.YellowThreshold=3;row.YellowBan=1;row.RedBan=1;row.SecondYellowBan=1;row.CountDismissalYellows=false;
        row.UpdatedById=actor;row.UpdatedAt=DateTime.UtcNow;row.Revision=Guid.NewGuid();
        foreach(var removed in row.Teams.Where(t=>!ids.Contains(t.TrainingTeamId)).ToList()){db.LeagueTeams.Remove(removed);row.Teams.Remove(removed);}
        foreach(var registration in row.Teams.Where(t=>ids.Contains(t.TrainingTeamId))){registration.Status=input.TeamStatuses.GetValueOrDefault(registration.TrainingTeamId,"Registered");registration.Stage=input.Stage;registration.Group=input.Group;registration.SquadDeadline=input.TeamDeadlines.GetValueOrDefault(registration.TrainingTeamId);}
        foreach(var id in ids.Where(id=>!row.Teams.Any(t=>t.TrainingTeamId==id)))row.Teams.Add(new(){TrainingTeamId=id,Status=input.TeamStatuses.GetValueOrDefault(id,"Registered"),RegisteredOn=BillingClock.Today,Stage=input.Stage,Group=input.Group,SquadDeadline=input.TeamDeadlines.GetValueOrDefault(id)});
        if(input.Id==0)db.Leagues.Add(row);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }

    public async Task<int> AddFromCatalog(CompetitionCatalogModel input,string actor)
    {
        Validator.ValidateObject(input,new(input),true);
        input.SelectedKeys??=[];
        if(input.EndsOn<input.StartsOn)throw new ValidationException("Kontrolloni datat e sezonit.");
        var current=CurrentSeason();
        if(input.Season.Trim()!=current.Name||input.StartsOn<current.StartsOn||input.EndsOn>current.EndsOn)throw new ValidationException($"Aktualisht mund të shtohen gara vetëm për sezonin {current.Name}.");
        var selected=Catalog.Where(x=>input.SelectedKeys.Contains(x.Key,StringComparer.OrdinalIgnoreCase)).ToList();
        if(selected.Count==0)throw new ValidationException("Zgjidhni së paku një garë.");
        if(selected.Count!=input.SelectedKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count())throw new ValidationException("Përzgjedhja përmban garë të pavlefshme.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var names=selected.Select(x=>x.Name.ToUpper()).ToList();
        var existing=await db.Leagues.Where(x=>x.Season==input.Season&&names.Contains(x.Name.ToUpper())).Select(x=>x.Name).ToListAsync();
        if(existing.Count>0)throw new ValidationException($"Tashmë ekziston për këtë sezon: {string.Join(", ",existing)}.");
        foreach(var x in selected)db.Leagues.Add(new(){Name=x.Name,Organizer=x.Organizer,Country=x.Country,Gender=x.Gender,AgeCategory=x.AgeCategory,CompetitionScope=x.Scope,CompetitionType=x.Type,Stage=x.Stage,Season=input.Season.Trim(),StartsOn=input.StartsOn,EndsOn=input.EndsOn,Kind=x.Type is "Cup" or "SuperCup"?"Cup":"Official",IsActive=true,IsOfficial=false,IsVerified=false,RulesConfirmed=true,YellowThreshold=3,YellowBan=1,RedBan=1,SecondYellowBan=1,CountDismissalYellows=false,UpdatedById=actor,UpdatedAt=DateTime.UtcNow});
        await db.SaveChangesAsync();await tx.CommitAsync();return selected.Count;
    }

}
