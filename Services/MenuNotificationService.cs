using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public class MenuNotificationService(ApplicationDbContext db)
{
    // Called within the menu transaction: publishing and queuing commit together.
    public async Task Queue(int studentId,string actor,bool reset)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
        if(!await db.ParentPushSettings.AnyAsync())return;
        var rows=await db.ParentPushSubscriptions.Include(x=>x.Student).Where(x=>x.Enabled&&x.Student.IsActive&&
            (studentId>0?x.StudentId==studentId:!db.NutritionMenus.Any(m=>m.Scope==x.StudentId))).ToListAsync();
        var eligible=new List<ParentPushSubscription>();var push=new ParentPushService(db);
        foreach(var sub in rows){var token=await push.AccessToken(sub);if(token!=null&&sub.TokenHash==ParentPushService.Hash(token))eligible.Add(sub);}
        if(eligible.Count==0)return;
        var revision=await db.NutritionMenus.Where(x=>x.Scope==(reset?0:studentId)).Select(x=>(Guid?)x.Revision).SingleOrDefaultAsync()??Guid.Empty;
        db.ParentPushMessages.Add(new(){RequestId=Guid.NewGuid(),AutomaticKind="Menu",ScheduleHash=revision.ToString("N"),CreatedBy=actor,
            Title="Menuja ushqimore u përditësua",Body=studentId>0&&!reset?"Menuja individuale është përditësuar. Hapni portalin për ta parë.":"Menuja e përbashkët është përditësuar për lojtarin. Hapni portalin për ta parë.",
            Audience=studentId>0?"Menu individuale":"Menu e përbashkët",Deliveries=eligible.Select(x=>new ParentPushDelivery{SubscriptionId=x.Id}).ToList()});
        await db.SaveChangesAsync();
    }
    public async Task<bool> IsCurrent(int studentId,string? revision)
    {
        var menu=await db.NutritionMenus.AsNoTracking().Where(x=>x.Scope==studentId||x.Scope==0).OrderByDescending(x=>x.Scope).FirstOrDefaultAsync();
        return (menu?.Revision??Guid.Empty).ToString("N")==revision;
    }
}
