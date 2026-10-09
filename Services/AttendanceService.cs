using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class AttendanceService(ApplicationDbContext db, TimeProvider? clock=null)
{
    private DateTime Now => (clock??TimeProvider.System).GetUtcNow().UtcDateTime;
    private async Task<DateTime> Deadline(TeamAttendance saved) =>
        (await db.AttendanceChanges.Where(x=>x.TeamAttendanceId==saved.Id).MinAsync(x=>(DateTime?)x.CreatedAt)??saved.UpdatedAt).AddHours(1);
    private async Task<TrainingTeam> Team(int id, string role, string actor)
    {
        if(role is not ("head" or "assistant")) throw new UnauthorizedAccessException();
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor);
        if(user==null || UserAdministrationService.IsDisabled(user)) throw new UnauthorizedAccessException();
        var roles=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==actor select r.Name).ToListAsync();
        var team=await db.TrainingTeams.AsNoTracking().Include(x=>x.FootballField).SingleOrDefaultAsync(x=>x.Id==id);
        if(team==null || (!roles.Contains(Roles.Admin) && (!roles.Contains(Roles.Coach) || (role=="head"?team.CoachId:team.AssistantCoachId)!=actor)))
            throw new UnauthorizedAccessException();
        return team;
    }
    private static string Token(IEnumerable<AttendancePlayerInput> players) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(players.OrderBy(x=>x.StudentId).Select(x=>new{x.StudentId,x.Name})))));
    private Task<List<AttendancePlayerInput>> Roster(int teamId) => db.Students.AsNoTracking().Where(x=>x.TrainingTeamId==teamId && x.IsActive)
        .OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).ThenBy(x=>x.Id).Select(x=>new AttendancePlayerInput{StudentId=x.Id,Name=x.FirstName+" "+x.LastName,Notes=x.Notes}).ToListAsync();
    public async Task<AttendanceModel> LoadAsync(int teamId, string role, DateOnly date, string actor)
    {
        if(date<new DateOnly(1900,1,1)||date>BillingClock.Today) throw new ValidationException("Zgjidhni datë deri sot.");
        var team=await Team(teamId,role,actor);
        var saved=await db.TeamAttendances.AsNoTracking().Include(x=>x.Players).ThenInclude(x=>x.Student).Include(x=>x.SavedBy).SingleOrDefaultAsync(x=>x.TrainingTeamId==teamId&&x.Date==date);
        var players=saved==null?await Roster(teamId):saved.Players.OrderBy(x=>x.StudentName).ThenBy(x=>x.StudentId)
            .Select(x=>new AttendancePlayerInput{StudentId=x.StudentId,Name=x.StudentName,Present=x.Present,Notes=x.Student?.Notes}).ToList();
        var deadline=saved==null?(DateTime?)null:await Deadline(saved);
        if(players.Count>0){
            var positions=await new BillingService(db).GetFinancialPositionsAsync();
            foreach(var player in players) player.Debt=positions.GetValueOrDefault(player.StudentId)?.Debt??0;
        }
        var today=BillingClock.Today;
        var historyFrom=today.AddDays(-14);
        return new(){TeamId=teamId,CoachRole=role,Date=date,TeamName=team.Name,FieldName=team.FootballField?.Name??team.Location,
            EditableUntil=deadline,IsLocked=deadline.HasValue&&Now>=deadline.Value,
            Revision=saved?.Revision??Guid.Empty,RosterToken=Token(players),Players=players,SavedAt=saved?.UpdatedAt,
            SavedBy=saved?.SavedBy==null?null:$"{saved.SavedBy.FirstName} {saved.SavedBy.LastName}",
            History=await db.TeamAttendances.AsNoTracking().Include(x=>x.Players).Where(x=>x.TrainingTeamId==teamId&&x.Date>=historyFrom&&x.Date<today).OrderByDescending(x=>x.Date).ToListAsync()};
    }
    public async Task SaveAsync(AttendanceModel model, string actor)
    {
        Validator.ValidateObject(model,new(model),true);
        foreach(var player in model.Players) Validator.ValidateObject(player,new(player),true);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var team=await Team(model.TeamId,model.CoachRole,actor);
        var saved=await db.TeamAttendances.Include(x=>x.Players).SingleOrDefaultAsync(x=>x.TrainingTeamId==model.TeamId&&x.Date==model.Date);
        if(saved!=null && Now>=await Deadline(saved)) throw new InvalidOperationException("Ka kaluar një orë nga ruajtja e parë. Prezenca nuk mund të ndryshohet.");
        if(saved==null && !team.IsActive) throw new InvalidOperationException("Ekipi nuk është aktiv.");
        if((saved?.Revision??Guid.Empty)!=model.Revision) throw new InvalidOperationException("Lista është ruajtur ndërkohë nga një trajner. Rihapeni para ndryshimit.");
        var roster=saved==null?await Roster(model.TeamId):saved.Players.Select(x=>new AttendancePlayerInput{StudentId=x.StudentId,Name=x.StudentName}).ToList();
        if(Token(roster)!=model.RosterToken || !roster.Select(x=>x.StudentId).Order().SequenceEqual(model.Players.Select(x=>x.StudentId).Order()))
            throw new InvalidOperationException("Lojtarët e ekipit kanë ndryshuar. Rihapni listën e prezencës.");
        if(saved==null){saved=new(){TrainingTeamId=model.TeamId,Date=model.Date!.Value};db.TeamAttendances.Add(saved);
            saved.Players=roster.Select(x=>new AttendancePlayer{StudentId=x.StudentId,StudentName=x.Name}).ToList();}
        foreach(var player in saved.Players) player.Present=model.Players.Single(x=>x.StudentId==player.StudentId).Present!.Value;
        saved.Revision=Guid.NewGuid();saved.UpdatedAt=Now;saved.SavedById=actor;saved.CoachRole=model.CoachRole;
        db.AttendanceChanges.Add(new(){TeamAttendance=saved,ActorId=actor,CreatedAt=saved.UpdatedAt,Snapshot=JsonSerializer.Serialize(new{model.CoachRole,Players=saved.Players.Select(x=>new{x.StudentId,x.StudentName,x.Present})})});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
