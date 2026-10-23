using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public class CoachMenuService(ApplicationDbContext db)
{
    public async Task<Student> Authorize(int id,string actor)
    {
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor);
        if(user==null||UserAdministrationService.IsDisabled(user)||!await db.UserRoles.AnyAsync(x=>x.UserId==actor&&db.Roles.Any(r=>r.Id==x.RoleId&&r.Name==Roles.Coach)))throw new UnauthorizedAccessException();
        return await new PlayerDevelopmentService(db).Player(id,actor);
    }
    public async Task<CoachMenuPage> Load(int id,string actor)
    {
        var player=await Authorize(id,actor);
        var current=await new NutritionMenuService(db).Effective(id);
        var proposal=await db.CareReferrals.AsNoTracking().Where(x=>x.StudentId==id&&x.CoachId==actor&&x.MenuForCoach&&x.Kind==Roles.Nutritionist&&x.ClosedAt==null).OrderByDescending(x=>x.Id).FirstOrDefaultAsync();
        var draft=proposal?.MenuStatus=="Ready"&&proposal.MenuDraft!=null?JsonSerializer.Deserialize<MenuInput>(proposal.MenuDraft):null;
        return new(player,current,proposal,draft);
    }
    public async Task Request(int id,string actor,string? instructions)
    {
        if(instructions?.Length>2000)throw new ValidationException("Udhëzimi mund të ketë deri në 2000 karaktere.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2023)");
        await Authorize(id,actor);
        if(!await db.CareReferrals.AnyAsync(x=>x.StudentId==id&&x.CoachId==actor&&x.MenuForCoach&&x.ClosedAt==null&&(x.MenuStatus=="Pending"||x.MenuStatus=="Processing"))){
            db.CareReferrals.Add(new(){StudentId=id,CoachId=actor,Kind=Roles.Nutritionist,Reason=string.IsNullOrWhiteSpace(instructions)?"Përshtat menunë sipas matjeve, moshës dhe ngarkesës së regjistruar.":instructions.Trim(),CreatedAt=DateTime.UtcNow,MenuForCoach=true,MenuStatus="Pending",MenuNextAttempt=DateTime.UtcNow});
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }
    public async Task Approve(int id,string actor,bool reviewed)
    {
        var row=await db.CareReferrals.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.CoachId==actor&&x.MenuForCoach&&x.Kind==Roles.Nutritionist&&x.ClosedAt==null)??throw new UnauthorizedAccessException();
        await Authorize(row.StudentId,actor);
        if(row.MenuStatus=="Published")return;
        if(row.MenuStatus!="Ready"||row.MenuDraft==null)throw new ValidationException("Propozimi nuk është ende gati.");
        var draft=JsonSerializer.Deserialize<MenuInput>(row.MenuDraft)!;draft.AllergiesReviewed=reviewed;draft.StudentId=row.StudentId;draft.DraftReferralId=id;
        await new NutritionMenuService(db).SaveForCoach(draft,actor);
    }
}
