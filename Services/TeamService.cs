using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class TeamService(ApplicationDbContext db)
{
    public const string LockSql="SELECT pg_advisory_xact_lock(20260929, 2005)";
    public static readonly string[] Days=["E hënë","E martë","E mërkurë","E enjte","E premte","E shtunë","E diel"];
    public static string DayName(int day)=>day is >=1 and <=7?Days[day-1]:"—";
    public static int Age(DateOnly birth,DateOnly? asOf=null)
    {
        var date=asOf??BillingClock.Today;var age=date.Year-birth.Year;
        if(birth.AddYears(age)>date)age--;return age;
    }
    public async Task<List<TeamOption>> OptionsAsync(DateOnly? birth=null,bool includeInactive=false,int? excludingStudentId=null)
    {
        var query=db.TrainingTeams.AsNoTracking().Include(t=>t.FootballField).Include(t=>t.Sessions).AsQueryable();
        if(!includeInactive)query=query.Where(t=>t.IsActive);
        if(birth.HasValue)
        {
            if(birth>BillingClock.Today||birth==DateOnly.MinValue)return [];
            var age=Age(birth.Value);query=query.Where(t=>t.MinAge<=age&&t.MaxAge>=age);
        }
        var teams=await query.OrderBy(t=>t.MinAge).ThenBy(t=>t.Name).ToListAsync();
        var counts=await db.Students.AsNoTracking().Where(s=>s.IsActive && s.TrainingTeamId!=null && (!excludingStudentId.HasValue||s.Id!=excludingStudentId.Value)).GroupBy(s=>s.TrainingTeamId!.Value).Select(g=>new{Id=g.Key,Count=g.Count()}).ToDictionaryAsync(g=>g.Id,g=>g.Count);
        return teams.Select(t=>new TeamOption{Team=t,Enrolled=counts.GetValueOrDefault(t.Id)}).ToList();
    }
    public async Task<int> SaveAsync(TeamEditModel input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        foreach(var session in input.Sessions)Validator.ValidateObject(session,new ValidationContext(session),true);
        await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync(LockSql);
        var name=input.Name.Trim();var normalized=name.ToUpperInvariant();
        if(await db.TrainingTeams.AnyAsync(t=>t.Id!=input.Id&&t.Name.ToUpper()==normalized))throw new InvalidOperationException("Ekziston një ekip me këtë emër.");
        var team=input.Id==0?new TrainingTeam():await db.TrainingTeams.Include(t=>t.Sessions).SingleOrDefaultAsync(t=>t.Id==input.Id)??throw new InvalidOperationException("Ekipi nuk u gjet.");
        FootballField? field = null;
        if (input.FootballFieldId.HasValue)
        {
            field = await db.FootballFields.SingleOrDefaultAsync(f => f.Id == input.FootballFieldId && (f.IsActive || !input.IsActive))
                ?? throw new InvalidOperationException("Zgjidhni një fushë aktive.");
            if (input.IsActive)
            {
                var privateBookings = await db.FieldBookings.AsNoTracking().Where(b => b.FootballFieldId == field.Id && !b.IsCancelled && b.Date >= BillingClock.Today).ToListAsync();
                if (privateBookings.Any(b => input.Sessions.Any(s => s.Day == ((int)b.Date.DayOfWeek + 6) % 7 + 1 && s.StartsAt < b.EndsAt && s.EndsAt > b.StartsAt)))
                    throw new InvalidOperationException("Orari përputhet me një termin privat të rezervuar në këtë fushë. Zhvendosni terminin ose ndryshoni orarin / fushën e ekipit.");
            }
        }
        if(input.Id!=0)
        {
            if(team.Revision!=input.Revision)throw new InvalidOperationException("Ekipi u ndryshua ndërkohë. Hapeni përsëri para se të ruani.");
            var players=await db.Students.AsNoTracking().Where(s=>s.IsActive&&s.TrainingTeamId==team.Id).ToListAsync();
            if(input.Capacity<players.Count)throw new InvalidOperationException($"Kapaciteti nuk mund të jetë nën {players.Count} lojtarët aktivë të regjistruar.");
            if((team.MinAge!=input.MinAge||team.MaxAge!=input.MaxAge)&&players.Any(s=>Age(s.DateOfBirth)<input.MinAge||Age(s.DateOfBirth)>input.MaxAge))throw new InvalidOperationException("Intervali i ri përjashton lojtarë aktivë të ekipit. Transferojini para ndryshimit të moshës.");
            db.TrainingSessions.RemoveRange(team.Sessions);team.Sessions.Clear();
        }
        team.FootballFieldId=input.FootballFieldId;
        team.Name=name;team.MinAge=input.MinAge;team.MaxAge=input.MaxAge;team.Capacity=input.Capacity;team.IsActive=input.IsActive;team.Location=input.Location?.Trim();team.Notes=input.Notes?.Trim();team.Revision=Guid.NewGuid();
        foreach(var row in input.Sessions)team.Sessions.Add(new(){Day=row.Day,StartsAt=row.StartsAt!.Value,EndsAt=row.EndsAt!.Value});
        if(input.Id==0)db.TrainingTeams.Add(team);
        await db.SaveChangesAsync();
        var schedule=(field != null ? field.Name + " · " : "Pa fushë · ")+string.Join("; ",team.Sessions.OrderBy(s=>s.Day).ThenBy(s=>s.StartsAt).Select(s=>$"{DayName(s.Day)} {s.StartsAt:HH:mm}–{s.EndsAt:HH:mm}"));
        db.TeamChanges.Add(new(){TrainingTeamId=team.Id,ActorId=actor,Reason=input.Id==0?"Krijimi i ekipit":input.Reason!.Trim(),Snapshot=$"{team.Name} · {team.MinAge}–{team.MaxAge} vjeç · maksimumi {team.Capacity} lojtarë · {(team.IsActive?"Aktiv":"Joaktiv")} · {team.Location} · {schedule} · {team.Notes}"});
        await db.SaveChangesAsync();await tx.CommitAsync();return team.Id;
    }
    public async Task<TrainingTeam?> ValidatePlacementAsync(int? teamId,DateOnly birth,int? excludingStudentId=null)
    {
        if(db.Database.CurrentTransaction==null)throw new InvalidOperationException("Caktimi në ekip kërkon transaksion.");
        await db.Database.ExecuteSqlRawAsync(LockSql);
        if(!teamId.HasValue)return null;
        var team=await db.TrainingTeams.AsNoTracking().SingleOrDefaultAsync(t=>t.Id==teamId.Value&&t.IsActive)??throw new InvalidOperationException("Ekipi nuk është aktiv ose nuk ekziston.");
        var age=Age(birth);
        if(birth>BillingClock.Today||age<team.MinAge||age>team.MaxAge)throw new InvalidOperationException("Mosha e lojtarit nuk përputhet me ekipin e zgjedhur.");
        var count=await db.Students.CountAsync(s=>s.IsActive&&s.TrainingTeamId==teamId&&(!excludingStudentId.HasValue||s.Id!=excludingStudentId.Value));
        if(count>=team.Capacity)throw new InvalidOperationException("Ekipi është i mbushur. Zgjidhni një ekip tjetër ose regjistrojeni pa ekip.");
        return team;
    }
    public async Task ChangeStudentAsync(ChangeStudentTeamModel input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync(LockSql);
        var student=await db.Students.Include(s=>s.TrainingTeam).SingleOrDefaultAsync(s=>s.Id==input.StudentId)??throw new InvalidOperationException("Lojtari nuk u gjet.");
        await db.Entry(student).ReloadAsync();
        if(student.TrainingTeamId==input.TeamId)return;
        if(student.TrainingTeamId!=input.ExpectedTeamId)throw new InvalidOperationException("Ekipi i lojtarit ka ndryshuar. Hapeni formularin përsëri.");
        if(!student.IsActive&&input.TeamId.HasValue)throw new InvalidOperationException("Vetëm lojtarët aktivë mund të caktohen në ekip.");
        var target=await ValidatePlacementAsync(input.TeamId,student.DateOfBirth,student.Id);
        var oldName=student.TrainingTeamId.HasValue?await db.TrainingTeams.Where(t=>t.Id==student.TrainingTeamId).Select(t=>t.Name).SingleAsync():null;
        db.StudentTeamChanges.Add(new(){StudentId=student.Id,FromTeamId=student.TrainingTeamId,FromTeamName=oldName,ToTeamId=target?.Id,ToTeamName=target?.Name,Reason=input.Reason.Trim(),ActorId=actor});
        student.TrainingTeamId=target?.Id;student.UpdatedAt=DateTime.UtcNow;student.Revision=Guid.NewGuid();
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
