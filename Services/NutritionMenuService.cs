using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class NutritionMenuService(ApplicationDbContext db)
{
    public static MenuInput Default()=>new(){Recommendations="Menu orientuese. Përshtatni ushqimet për alergjitë dhe porcionet me nutricistin. Pini ujë rregullisht gjatë ditës dhe stërvitjes.",Days=[
        new(){Breakfast="Tërshërë me qumësht dhe banane",Lunch="Pulë, oriz dhe sallatë",Dinner="Supë me thjerrëza dhe bukë",Snack="Kos natyral dhe fruta"},
        new(){Breakfast="Vezë, bukë dhe domate",Lunch="Fasule me perime dhe bukë",Dinner="Makarona me perime dhe djathë",Snack="Fruta dhe kos"},
        new(){Breakfast="Kos me tërshërë dhe fruta",Lunch="Peshk pa hala, patate dhe perime",Dinner="Omëletë me perime dhe bukë",Snack="Sanduiç i vogël me djathë"},
        new(){Breakfast="Bukë me djathë dhe kastravec",Lunch="Pulë me makarona dhe perime",Dinner="Supë me fasule dhe bukë",Snack="Banane dhe kos"},
        new(){Breakfast="Tërshërë me qumësht dhe mollë",Lunch="Oriz me qiqra dhe perime",Dinner="Sanduiç me pulë dhe sallatë",Snack="Kos dhe fruta"},
        new(){Breakfast="Vezë, bukë dhe fruta",Lunch="Mish i pjekur, patate dhe sallatë",Dinner="Supë me perime, thjerrëza dhe bukë",Snack="Qumësht ose kos dhe fruta"},
        new(){Breakfast="Kos, tërshërë dhe banane",Lunch="Pulë me oriz dhe perime",Dinner="Makarona me fasule dhe domate",Snack="Bukë me djathë dhe fruta"}]};
    public async Task Authorize(int id,string actor)
    {
        var care=new PlayerCareService(db);
        if(await care.Kind(actor)!=Roles.Nutritionist)throw new UnauthorizedAccessException();
        if(id<0)throw new UnauthorizedAccessException();
        if(id>0)await care.Assigned(id,actor);
    }
    public async Task<MenuInput> Effective(int id)
    {
        var rows=await db.NutritionMenus.AsNoTracking().Where(x=>x.Scope==0||x.Scope==id).ToListAsync();
        var own=rows.SingleOrDefault(x=>x.Scope==id&&id>0);
        var row=own??rows.SingleOrDefault(x=>x.Scope==0);
        var result=row==null?Default():new MenuInput{Days=JsonSerializer.Deserialize<List<MenuDay>>(row.Data)!,Recommendations=row.Recommendations,ApprovedRole=row.ApprovedRole,UpdatedAt=row.UpdatedAt};
        result.StudentId=id;result.Individual=own!=null;
        result.Revision=(id==0?row:own)?.Revision??Guid.Empty;
        return result;
    }
    public static void Validate(MenuInput input)
    {
        Validator.ValidateObject(input,new(input),true);
        if(input.Days==null||input.Days.Count!=7||input.Days.Any(x=>x==null))throw new ValidationException("Plotësoni të shtatë ditët.");
        foreach(var day in input.Days)Validator.ValidateObject(day,new(day),true);
    }
    public async Task Save(MenuInput input,string actor,bool reset=false)
        =>await SaveCore(input,actor,reset,false);
    public async Task SaveForCoach(MenuInput input,string actor)
        =>await SaveCore(input,actor,false,true);
    private async Task SaveCore(MenuInput input,string actor,bool reset,bool forCoach)
    {
        if(!reset)Validate(input);
        await using var tx=await db.Database.BeginTransactionAsync();
        if(forCoach){await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);}
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        if(forCoach){await new CoachMenuService(db).Authorize(input.StudentId,actor);if(input.DraftReferralId<=0)throw new UnauthorizedAccessException();}else await Authorize(input.StudentId,actor);
        if(input.DraftReferralId>0){
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2023)");
            var referral=await db.CareReferrals.SingleOrDefaultAsync(x=>x.Id==input.DraftReferralId&&x.StudentId==input.StudentId&&x.Kind==Roles.Nutritionist&&x.ClosedAt==null&&x.MenuStatus=="Ready");
            if(referral==null)throw new ValidationException("Propozimi nuk është më i disponueshëm. Rihapni menunë.");
            if(forCoach&&(!referral.MenuForCoach||referral.CoachId!=actor))throw new UnauthorizedAccessException();
            if(!input.AllergiesReviewed)throw new ValidationException("Kontrolloni alergjitë dhe kufizimet para publikimit.");
            referral.MenuStatus="Published";
        }
        var row=await db.NutritionMenus.SingleOrDefaultAsync(x=>x.Scope==input.StudentId);
        if((row?.Revision??Guid.Empty)!=input.Revision)throw new ValidationException("Menuja ka ndryshuar. Rihapeni para ruajtjes.");
        var data=JsonSerializer.Serialize(input.Days);
        var changed=reset?row!=null:row==null||row.Data!=data||row.Recommendations!=(input.Recommendations?.Trim()??"");
        if(reset){if(input.StudentId==0)throw new ValidationException("Zgjidhni një lojtar.");if(row!=null)db.NutritionMenus.Remove(row);}
        else if(changed){
            if(row==null){row=new(){Scope=input.StudentId};db.NutritionMenus.Add(row);}
            row.Data=data;row.Recommendations=input.Recommendations?.Trim()??"";row.UpdatedBy=actor;row.UpdatedAt=DateTime.UtcNow;row.Revision=Guid.NewGuid();row.ApprovedRole=forCoach?Roles.Coach:Roles.Nutritionist;
        }
        await db.SaveChangesAsync();
        if(changed)await new MenuNotificationService(db).Queue(input.StudentId,actor,reset);
        await tx.CommitAsync();
    }
}
