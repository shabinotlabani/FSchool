using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Services;

public class LeagueService(ApplicationDbContext db)
{
    public IQueryable<League> Eligible(int team,DateOnly date) => db.Leagues.Where(l=>l.IsActive&&l.StartsOn<=date&&l.EndsOn>=date&&l.Teams.Any(t=>t.TrainingTeamId==team));
    public async Task Save(LeagueInput input,string actor)
    {
        Validator.ValidateObject(input,new(input),true);
        if(input.TeamIds==null)throw new ValidationException("Kontrolloni ekipet.");
        if(string.IsNullOrWhiteSpace(input.Name)||input.StartsOn.Year<1900||input.EndsOn.Year>2200||input.EndsOn<input.StartsOn)throw new ValidationException("Kontrolloni emrin dhe datat e sezonit.");
        if(input.RulesConfirmed && (!input.YellowThreshold.HasValue||!input.YellowBan.HasValue||!input.RedBan.HasValue||!input.SecondYellowBan.HasValue))throw new ValidationException("Plotësoni të gjitha rregullat e kartonëve.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var ids=input.TeamIds.Distinct().ToList();
        if(await db.TrainingTeams.CountAsync(t=>ids.Contains(t.Id))!=ids.Count)throw new ValidationException("Zgjidhni ekipet e këtij klubi.");
        var row=input.Id==0?new League():await db.Leagues.Include(l=>l.Teams).SingleOrDefaultAsync(l=>l.Id==input.Id)??throw new UnauthorizedAccessException();
        input.Kind=input.Id==0?"Official":row.Kind;
        if(input.Id!=0&&row.Revision!=input.Revision)throw new ValidationException("Liga ndryshoi. Rihapeni para ruajtjes.");
        if(await db.Leagues.AnyAsync(l=>l.Id!=input.Id&&l.Name.ToUpper()==input.Name.Trim().ToUpper()&&l.StartsOn==input.StartsOn))throw new ValidationException("Kjo ligë ekziston për këtë sezon.");
        if(input.Id!=0&&await db.MatchGames.AnyAsync(g=>g.LeagueId==input.Id&&(g.Kind!=input.Kind||g.Date<input.StartsOn||g.Date>input.EndsOn||!ids.Contains(g.TrainingTeamId))))throw new ValidationException("Ruani ekipet, llojin dhe datat që përfshijnë ndeshjet e regjistruara.");
        row.Name=input.Name.Trim();row.Kind=input.Kind;row.StartsOn=input.StartsOn;row.EndsOn=input.EndsOn;row.IsActive=input.IsActive;row.RulesConfirmed=input.RulesConfirmed;
        row.YellowThreshold=input.YellowThreshold??0;row.YellowBan=input.YellowBan??0;row.RedBan=input.RedBan??0;row.SecondYellowBan=input.SecondYellowBan??0;row.CountDismissalYellows=input.CountDismissalYellows;
        row.UpdatedById=actor;row.UpdatedAt=DateTime.UtcNow;row.Revision=Guid.NewGuid();
        foreach(var removed in row.Teams.Where(t=>!ids.Contains(t.TrainingTeamId)).ToList()){db.LeagueTeams.Remove(removed);row.Teams.Remove(removed);}
        foreach(var id in ids.Where(id=>!row.Teams.Any(t=>t.TrainingTeamId==id)))row.Teams.Add(new(){TrainingTeamId=id});
        if(input.Id==0)db.Leagues.Add(row);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
