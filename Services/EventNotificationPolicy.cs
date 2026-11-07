using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public class EventNotificationPolicy(ApplicationDbContext db)
{
    public async Task<bool> IsCurrent(ParentPushDelivery delivery)
    {
        if(!await ParentPortalModules.Enabled(db,"Events"))return false;
        var key=delivery.Message.SourceKey;
        if(key==null||!key.StartsWith("E:")||!int.TryParse(key[2..],out var id))return false;
        var i=await db.EventInvitations.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.ClubEventId==id&&x.StudentId==delivery.Subscription.StudentId);
        if(i==null||i.Event.Status=="Draft"||i.Event.Version.ToString()!=delivery.Message.ScheduleHash)return false;
        if(delivery.Message.AutomaticKind=="EventDeadline")return i.Status=="Pending"&&i.Event.Status=="Published"&&i.Event.Deadline>DateTime.UtcNow;
        if(delivery.Message.AutomaticKind=="EventStart")return i.Status=="Accepted"&&i.Event.Status=="Published"&&i.Event.StartsAt>DateTime.UtcNow;
        return true;
    }
}
