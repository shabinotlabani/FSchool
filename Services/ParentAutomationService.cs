using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ParentAutomationService(ApplicationDbContext db)
{
    private record Source(string Key,string Name,bool Active,string Hash,int? TeamId,int? StudentId,
        DateOnly? From,DateOnly? To,bool Once,List<ParentSchedule> Slots);
    private static string Fingerprint(object value)=>ParentPushService.Hash(JsonSerializer.Serialize(value));
    private async Task<List<Source>> Sources()
    {
        var teams=await db.TrainingTeams.AsNoTracking().Include(x=>x.Sessions).ToListAsync();
        var personal=await db.PersonalTrainings.AsNoTracking().Include(x=>x.Sessions).ToListAsync();
        var result=new List<Source>();
        foreach(var team in teams){
            var slots=team.Sessions.Select(x=>new ParentSchedule(x.Day,x.StartsAt,x.EndsAt)).OrderBy(x=>x.Day).ThenBy(x=>x.Start).ToList();
            result.Add(new($"T:{team.Id}",team.Name,team.IsActive,
                Fingerprint(new{team.IsActive,team.FootballFieldId,team.Location,Slots=slots}),team.Id,null,null,null,false,slots));
        }
        foreach(var training in personal){
            var slots=training.Sessions.Select(x=>new ParentSchedule(x.Day,x.StartsAt,x.EndsAt)).OrderBy(x=>x.Day).ThenBy(x=>x.Start).ToList();
            result.Add(new($"P:{training.Id}",training.Name,!training.IsCancelled,
                Fingerprint(new{training.IsCancelled,training.StartsOn,training.EndsOn,training.Location,training.Mode,Slots=slots}),null,training.StudentId,
                training.StartsOn,training.EndsOn,training.Mode!="Monthly",slots));
        }
        return result;
    }
    private async Task<List<ParentPushSubscription>> Subscribers()
    {
        var rows=await db.ParentPushSubscriptions.Include(x=>x.Student).Where(x=>x.Enabled&&x.Student.IsActive).ToListAsync();
        var service=new ParentPushService(db);var result=new List<ParentPushSubscription>();
        foreach(var row in rows){var token=await service.AccessToken(row);if(token!=null&&row.TokenHash==ParentPushService.Hash(token))result.Add(row);}
        return result;
    }
    private async Task Add(string eventKey,string kind,string title,string body,string audience,List<ParentPushSubscription> rows,
        DateTime now,DateTime expiry,string? source=null,string? scheduleHash=null)
    {
        if(rows.Count==0)return;
        var id=new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(eventKey))[..16]);
        if(await db.ParentPushMessages.AnyAsync(x=>x.RequestId==id))return;
        db.ParentPushMessages.Add(new(){RequestId=id,AutomaticKind=kind,SourceKey=source,ScheduleHash=scheduleHash,ExpiresAt=expiry,
            Title=title,Body=body.Length>400?body[..397]+"...":body,Audience=audience.Length>150?audience[..150]:audience,CreatedBy="System",CreatedAt=now,
            Deliveries=rows.Select(x=>new ParentPushDelivery{SubscriptionId=x.Id,NextAttempt=now}).ToList()});
        await db.SaveChangesAsync();
    }
    public async Task<decimal> Debt(ParentPushSubscription subscription)
    {
        var ids=subscription.FamilyId.HasValue
            ?(await new FamilyService(db).MembersAsync(subscription.FamilyId.Value,TariffService.CurrentMonth)).Select(x=>x.StudentId).ToList()
            :new List<int>{subscription.StudentId};
        decimal debt=0;var billing=new BillingService(db);
        foreach(var id in ids)debt+=(await billing.GetFinancialPositionsAsync(id)).GetValueOrDefault(id)?.Debt??0;
        return debt;
    }
    public async Task Scan(DateTime utcNow)
    {
        utcNow=DateTime.SpecifyKind(utcNow,DateTimeKind.Utc);
        await using var tx=await db.Database.BeginTransactionAsync();
        // Billing is acquired first, as in financial writes; the push lock serializes replicas.
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
        var settings=await db.ParentPushSettings.SingleOrDefaultAsync();
        if(settings==null)return;
        var local=TimeZoneInfo.ConvertTimeFromUtc(utcNow,BillingClock.Zone);var today=DateOnly.FromDateTime(local);
        var sources=await Sources();var subscriptions=await Subscribers();
        var states=await db.ParentScheduleStates.ToDictionaryAsync(x=>x.SourceKey);
        foreach(var source in sources){
            var rows=subscriptions.Where(x=>source.TeamId.HasValue?x.Student.TrainingTeamId==source.TeamId:x.StudentId==source.StudentId).ToList();
            if(!states.TryGetValue(source.Key,out var state)){
                state=new(){SourceKey=source.Key,Hash=source.Hash};db.ParentScheduleStates.Add(state);
            }else if(state.Hash!=source.Hash){
                state.Hash=source.Hash;state.Version++;
                if(settings.ScheduleNotifications){
                    var schedule=string.Join("; ",source.Slots.Select(x=>$"{TeamService.DayName(x.Day)} {x.Start:HH:mm}–{x.End:HH:mm}"));
                    var cancelled=!source.Active||source.Slots.Count==0;
                    await Add($"schedule:{source.Key}:{state.Version}","Schedule",cancelled?"Orari u anulua":"Orari u ndryshua",
                        cancelled?$"{source.Name}: stërvitjet janë anuluar.":$"{source.Name}: {schedule}. Hapni portalin për orarin.",
                        source.Name,rows,utcNow,utcNow.AddDays(1),source.Key,source.Hash);
                }
            }
            if(!settings.TrainingReminders||!source.Active)continue;
            // Include tomorrow so reminders for sessions after midnight are not missed.
            foreach(var date in new[]{today,today.AddDays(1)}){
                if(source.From.HasValue&&(date<source.From||source.Once&&date!=source.From)||source.To.HasValue&&date>source.To)continue;
                var weekday=((int)date.DayOfWeek+6)%7+1;
                foreach(var slot in source.Slots.Where(x=>source.Once||x.Day==weekday)){
                    var wall=date.ToDateTime(slot.Start);
                    if(BillingClock.Zone.IsInvalidTime(wall))continue;
                    var start=TimeZoneInfo.ConvertTimeToUtc(wall,BillingClock.Zone);
                    var due=start.AddHours(-1);
                    // A short recovery window avoids late reminders after a long outage.
                    if(utcNow<due||utcNow>=due.AddMinutes(5))continue;
                    foreach(var child in rows.GroupBy(x=>x.StudentId))
                        await Add($"training:{source.Key}:{child.Key}:{start:O}","Training","Kujtesë për stërvitjen",
                            $"{child.First().Student.FullName}: {source.Name}, më {date:dd/MM/yyyy} në {slot.Start:HH:mm}.",
                            child.First().Student.FullName,child.ToList(),utcNow,start,source.Key,source.Hash);
                }
            }
        }
        // A deleted schedule is an explicit cancellation for its current audience.
        foreach(var state in states.Values.Where(x=>!sources.Any(s=>s.Key==x.SourceKey)&&x.Hash!="deleted")){
            state.Hash="deleted";state.Version++;
            var parts=state.SourceKey.Split(':');var id=int.Parse(parts[1]);
            var rows=parts[0]=="T"?subscriptions.Where(x=>x.Student.TrainingTeamId==id).ToList():[];
            if(settings.ScheduleNotifications)await Add($"schedule:{state.SourceKey}:{state.Version}","Schedule","Orari u anulua",
                "Orari i stërvitjes është anuluar. Kontaktoni klubin për orarin e ri.","Orari",rows,utcNow,utcNow.AddDays(1),state.SourceKey,"deleted");
        }
        var day=Math.Min(settings.PaymentDay,DateTime.DaysInMonth(today.Year,today.Month));
        if(settings.PaymentReminders&&today.Day==day&&local.Hour>=settings.PaymentHour){
            foreach(var account in subscriptions.GroupBy(x=>x.FamilyId.HasValue?$"F:{x.FamilyId}":$"S:{x.StudentId}")){
                var debt=await Debt(account.First());if(debt<=0)continue;
                // One family notification per device, even when siblings share the subscription.
                var rows=account.GroupBy(x=>x.EndpointHash).Select(x=>x.First()).ToList();
                await Add($"payment:{account.Key}:{today:yyyy-MM}","Payment","Kujtesë për pagesën",
                    $"Keni {debt:N2} për t’u paguar. Ju lutem kontrolloni pagesat në portal.",
                    account.First().FamilyId.HasValue?"Familja":account.First().Student.FullName,rows,utcNow,
                    TimeZoneInfo.ConvertTimeToUtc(today.AddDays(1).ToDateTime(TimeOnly.MinValue),BillingClock.Zone));
            }
        }
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<bool> IsCurrent(ParentPushDelivery delivery,ParentPushSettings settings,DateTime now)
    {
        var message=delivery.Message;
        if(message.AutomaticKind==null)return true;
        if(message.AutomaticKind=="Menu")return await new MenuNotificationService(db).IsCurrent(delivery.Subscription.StudentId,message.ScheduleHash);
        if(message.ExpiresAt<=now)return false;
        if(message.AutomaticKind=="Payment")return settings.PaymentReminders&&await Debt(delivery.Subscription)>0;
        if(message.AutomaticKind=="Training"&&!settings.TrainingReminders||message.AutomaticKind=="Schedule"&&!settings.ScheduleNotifications)return false;
        var source=(await Sources()).SingleOrDefault(x=>x.Key==message.SourceKey);
        if(source==null)return message.AutomaticKind=="Schedule"&&message.ScheduleHash=="deleted";
        if(source.Hash!=message.ScheduleHash)return false;
        return source.TeamId.HasValue?delivery.Subscription.Student.TrainingTeamId==source.TeamId:delivery.Subscription.StudentId==source.StudentId;
    }
}
