using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class NutritionDraftProcessor(ApplicationDbContext db,GeminiMenuService gemini)
{
    public async Task<bool> Process(int id,CancellationToken ct)
    {
        string? actor;int attempt;
        await using(var tx=await db.Database.BeginTransactionAsync(ct)){
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2023)",ct);
            var row=await db.CareReferrals.SingleOrDefaultAsync(x=>x.Id==id,ct);
            if(row==null||row.Kind!=Roles.Nutritionist||row.ClosedAt!=null||row.MenuStatus is not ("Pending" or "Processing")||row.MenuNextAttempt>DateTime.UtcNow)return false;
            if(row.MenuAttempts>=1){row.MenuStatus="Failed";await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return false;}
            actor=row.MenuForCoach?row.CoachId:await db.CareAssignments.Where(x=>x.StudentId==row.StudentId&&x.Kind==Roles.Nutritionist&&x.Active&&x.Student.IsActive&&
                (x.Staff.LockoutEnd==null||x.Staff.LockoutEnd<DateTimeOffset.UtcNow)&&db.UserRoles.Any(ur=>ur.UserId==x.StaffId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name==Roles.Nutritionist)))
                .OrderBy(x=>x.Id).Select(x=>x.StaffId).FirstOrDefaultAsync(ct);
            if(actor==null){row.MenuError="Në pritje të nutricistit të caktuar.";row.MenuNextAttempt=DateTime.UtcNow.AddMinutes(1);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return false;}
            row.MenuStatus="Processing";attempt=++row.MenuAttempts;row.MenuNextAttempt=DateTime.UtcNow.AddMinutes(3);row.MenuError=null;
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        }
        MenuInput? draft=null;string? error=null;
        try{draft=await gemini.GenerateReferral(id,actor,ct);}
        catch(Exception ex) when(ex is ValidationException or UnauthorizedAccessException or InvalidOperationException){error=ex is ValidationException?ex.Message:"Rasti ose caktimi ka ndryshuar.";}
        ct.ThrowIfCancellationRequested();
        db.ChangeTracker.Clear();
        await using var finish=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2023)",ct);
        var current=await db.CareReferrals.SingleAsync(x=>x.Id==id,ct);
        if(current.ClosedAt==null&&current.MenuStatus=="Processing"&&current.MenuAttempts==attempt){
            current.MenuDraft=draft==null?null:JsonSerializer.Serialize(draft);
            current.MenuStatus=draft!=null?"Ready":"Failed";
            current.MenuError=error;current.MenuNextAttempt=null;
            await db.SaveChangesAsync(ct);
        }
        await finish.CommitAsync(ct);return draft!=null;
    }
}
public class NutritionDraftWorker(IServiceScopeFactory scopes,ILogger<NutritionDraftWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested){
            try{
              foreach(var clubId in await ClubJobs.ActiveIds(scopes,stoppingToken)){
               try{
                using var scope=scopes.CreateScope();var db=ClubJobs.SetClub(scope.ServiceProvider,clubId);
                var ids=await db.CareReferrals.AsNoTracking().Where(x=>x.ClosedAt==null&&(x.MenuStatus=="Pending"||x.MenuStatus=="Processing")&&(x.MenuNextAttempt==null||x.MenuNextAttempt<=DateTime.UtcNow))
                    .OrderBy(x=>x.CreatedAt).Select(x=>x.Id).Take(10).ToListAsync(stoppingToken);
                foreach(var id in ids){using var job=scopes.CreateScope();ClubJobs.SetClub(job.ServiceProvider,clubId);await job.ServiceProvider.GetRequiredService<NutritionDraftProcessor>().Process(id,stoppingToken);}
               }catch(Exception)when(!stoppingToken.IsCancellationRequested){logger.LogWarning("Club {ClubId}: nutrition draft deferred.",clubId);}
              }
            }catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception){logger.LogWarning("Automatic nutrition draft processing failed; retrying on the next scan.");}
            await Task.Delay(TimeSpan.FromSeconds(3),stoppingToken);
        }
    }
}
