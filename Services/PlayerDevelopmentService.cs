using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class PlayerDevelopmentService(ApplicationDbContext db)
{
    public async Task SaveRecord(DevelopmentRecordInput input,string coach)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(20261024,{db.CurrentClubId})");
        await Player(input.StudentId,coach);
        var row=input.Id==0 ? new DevelopmentRecord{StudentId=input.StudentId,CoachId=coach,Kind=input.Kind,CreatedAt=DateTime.UtcNow} :
            await db.DevelopmentRecords.SingleOrDefaultAsync(x=>x.Id==input.Id && x.StudentId==input.StudentId && x.CoachId==coach && x.Kind==input.Kind) ?? throw new UnauthorizedAccessException();
        if(input.Id!=0 && row.Revision!=input.Revision)throw new ValidationException("Regjistrimi është ndryshuar. Rihapeni para ruajtjes.");
        var catalog=await new SportDevelopmentService(db).Catalog();
        if(input.Id==0&&input.Sport!=catalog.Sport)throw new ValidationException("Sporti i klubit ka ndryshuar. Rihapni formularin.");
        var schema=input.Id==0?catalog.Find(input.Kind):row.Schema;
        if(schema==null)throw new ValidationException("Kjo pjesë nuk është aktive për sportin e klubit.");
        var values=DevelopmentSections.Validate(input,schema);
        if(input.Id==0){row.Sport=catalog.Sport;row.SchemaData=System.Text.Json.JsonSerializer.Serialize(schema);}
        row.Date=input.Date;row.Data=System.Text.Json.JsonSerializer.Serialize(values);row.Notes=input.Notes?.Trim();
        row.UpdatedAt=DateTime.UtcNow;row.Revision=Guid.NewGuid();
        if(input.Id==0)db.DevelopmentRecords.Add(row);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    public IQueryable<Student> Players(string coach) => db.Students.Where(x => x.IsActive && x.TrainingTeam != null && x.TrainingTeam.IsActive &&
        (x.TrainingTeam.CoachId == coach || x.TrainingTeam.AssistantCoachId==coach || db.CoachTeams.Any(a=>a.TrainingTeamId==x.TrainingTeamId&&a.UserId==coach)));
    public async Task<Student> Player(int id, string coach) => await Players(coach).AsNoTracking().Include(x=>x.TrainingTeam).SingleOrDefaultAsync(x=>x.Id==id)
        ?? throw new UnauthorizedAccessException();
    public async Task Save(EvaluationInput input, string coach)
    {
        await Player(input.StudentId,coach);
        Validator.ValidateObject(input,new ValidationContext(input),true);
        if(input.Date==default || input.Date>BillingClock.Today) throw new ValidationException("Data e vlerësimit nuk mund të jetë në të ardhmen.");
        if(input.ReviewDate.HasValue && input.ReviewDate<=input.Date) throw new ValidationException("Rishikimi duhet të jetë pas datës së vlerësimit.");
        var row=input.Id==0 ? new PlayerEvaluation {StudentId=input.StudentId,CoachId=coach,CreatedAt=DateTime.UtcNow} :
            await db.PlayerEvaluations.SingleOrDefaultAsync(x=>x.Id==input.Id && x.StudentId==input.StudentId && x.CoachId==coach) ?? throw new UnauthorizedAccessException();
        if(input.Id!=0 && row.Revision!=input.Revision) throw new ValidationException("Vlerësimi është ndryshuar. Rihapeni para se të ruani.");
        row.Date=input.Date; row.Technique=input.Technique; row.Tactics=input.Tactics; row.Fitness=input.Fitness;
        row.Discipline=input.Discipline; row.Teamwork=input.Teamwork; row.Strengths=input.Strengths?.Trim();
        row.Improvements=input.Improvements?.Trim(); row.Goals=input.Goals.Trim(); row.ReviewDate=input.ReviewDate;
        row.UpdatedAt=DateTime.UtcNow; row.Revision=Guid.NewGuid();
        if(input.Id==0){row.Sport=await new SportDevelopmentService(db).Sport();db.PlayerEvaluations.Add(row);}
        await db.SaveChangesAsync();
    }
}
