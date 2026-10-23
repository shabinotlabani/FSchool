using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ParentPushService(ApplicationDbContext db)
{
    public async Task<string?> AccessToken(ParentPushSubscription row)
    {
        if(!row.FamilyId.HasValue)return row.Student.ParentLinkToken;
        var current=await new FamilyService(db).Latest(TariffService.CurrentMonth).AnyAsync(x=>x.StudentId==row.StudentId&&x.FamilyId==row.FamilyId&&x.FeePlan!.IsFamily);
        return current?await db.PlayerFamilies.Where(x=>x.Id==row.FamilyId).Select(x=>x.ParentLinkToken).SingleOrDefaultAsync():null;
    }
    public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static bool AllowedEndpoint(string value)
    {
        if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Port!=443||uri.UserInfo!=""||uri.Fragment!="")return false;
        var host=uri.IdnHost;
        return host=="fcm.googleapis.com"||host=="web.push.apple.com"||host.EndsWith(".push.apple.com",StringComparison.Ordinal)
            ||host=="updates.push.services.mozilla.com"||host.EndsWith(".notify.windows.com",StringComparison.Ordinal);
    }
    public static bool ValidKeys(string? key,string? auth)
    {
        try {
            var bytes=WebEncoders.Base64UrlDecode(key??"");
            if(bytes.Length!=65||bytes[0]!=4||WebEncoders.Base64UrlDecode(auth??"").Length!=16)return false;
            using var ec=ECDiffieHellman.Create(new ECParameters{Curve=ECCurve.NamedCurves.nistP256,Q=new ECPoint{X=bytes[1..33],Y=bytes[33..65]}});
            return true;
        }catch(Exception e)when(e is FormatException or CryptographicException or ArgumentException){return false;}
    }
    public async Task Configure(string subject)
    {
        if(subject.Length>300||!Uri.TryCreate(subject,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.IsLoopback||uri.UserInfo!=""||uri.Query!=""||uri.Fragment!="")
            throw new ValidationException("Shkruani adresën HTTPS publike të platformës.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
        var settings=await db.ParentPushSettings.SingleOrDefaultAsync();
        if(settings==null || string.IsNullOrEmpty(settings.PublicKey)){
            // A browser push subscription belongs to this origin, not to one club.
            // Reuse only the infrastructure key pair; messages and preferences stay scoped.
            var platformKeys=await db.ParentPushSettings.IgnoreQueryFilters().AsNoTracking().Where(x=>x.PublicKey!="")
                .OrderBy(x=>x.Id).Select(x=>new{x.PublicKey,x.PrivateKey}).FirstOrDefaultAsync();
            if(settings==null){settings=new(){Id=db.CurrentClubId};db.ParentPushSettings.Add(settings);}
            if(platformKeys!=null){settings.PublicKey=platformKeys.PublicKey;settings.PrivateKey=platformKeys.PrivateKey;}
            else{
                using var ec=ECDsa.Create(ECCurve.NamedCurves.nistP256);var keys=ec.ExportParameters(true);
                settings.PublicKey=WebEncoders.Base64UrlEncode(new byte[]{4}.Concat(keys.Q.X!).Concat(keys.Q.Y!).ToArray());
                settings.PrivateKey=WebEncoders.Base64UrlEncode(keys.D!);
            }
        }
        settings.Subject=uri.GetLeftPart(UriPartial.Authority);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Save(int studentId,string token,ParentPushInput input,bool enabled)
    {
        if(!AllowedEndpoint(input.Endpoint)||enabled&&!ValidKeys(input.P256dh,input.Auth))throw new ValidationException("Pajisja nuk mund të regjistrohet për njoftime.");
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
        var access=await new ParentAccessService(db).Resolve(token);
        if(access==null||!access.Students.Any(x=>x.Id==studentId&&(!enabled||x.IsActive)))throw new ValidationException("Lidhja nuk është aktive.");
        if(!await db.ParentPushSettings.AnyAsync())throw new ValidationException("Njoftimet nuk janë aktivizuar ende nga klubi.");
        var hash=Hash(input.Endpoint);
        var row=await db.ParentPushSubscriptions.SingleOrDefaultAsync(x=>x.StudentId==studentId&&x.EndpointHash==hash);
        if(row==null){
            if(!enabled)return;
            if(await db.ParentPushSubscriptions.CountAsync(x=>x.StudentId==studentId&&x.Enabled)>=20)throw new ValidationException("Është arritur numri maksimal i pajisjeve.");
            row=new(){StudentId=studentId,Endpoint=input.Endpoint,EndpointHash=hash};db.ParentPushSubscriptions.Add(row);
        }
        if(!enabled||row.TokenHash!=Hash(token))
            await db.ParentPushDeliveries.Where(x=>x.SubscriptionId==row.Id&&x.SentAt==null&&!x.Failed).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Failed,true));
        row.Enabled=enabled;row.FamilyId=access.Family?.Id;row.TokenHash=Hash(token);row.UpdatedAt=DateTime.UtcNow;
        if(enabled){row.P256dh=input.P256dh!;row.Auth=input.Auth!;}
        await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();
    }
    public async Task<int> Queue(ParentNotificationInput input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        if(input.RequestId==Guid.Empty||input.TeamId.HasValue&&input.StudentId.HasValue)throw new ValidationException("Zgjidhni ekipin ose lojtarin.");
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
        var existing=await db.ParentPushMessages.Include(x=>x.Deliveries).SingleOrDefaultAsync(x=>x.RequestId==input.RequestId);
        if(existing!=null)return existing.Deliveries.Count;
        if(!await db.ParentPushSettings.AnyAsync())throw new ValidationException("Aktivizoni fillimisht shërbimin e njoftimeve.");
        var audience="Të gjithë prindërit";
        if(input.TeamId.HasValue)audience=await db.TrainingTeams.Where(x=>x.Id==input.TeamId).Select(x=>x.Name).SingleOrDefaultAsync()??throw new ValidationException("Ekipi nuk u gjet.");
        if(input.StudentId.HasValue)audience=await db.Students.Where(x=>x.Id==input.StudentId&&x.IsActive).Select(x=>x.FirstName+" "+x.LastName).SingleOrDefaultAsync()??throw new ValidationException("Lojtari nuk u gjet.");
        var rows=await db.ParentPushSubscriptions.Include(x=>x.Student).Where(x=>x.Enabled&&x.Student.IsActive
            &&(!input.TeamId.HasValue||x.Student.TrainingTeamId==input.TeamId)&&(!input.StudentId.HasValue||x.StudentId==input.StudentId)).ToListAsync();
        var eligible=new List<ParentPushSubscription>();
        foreach(var row in rows){var token=await AccessToken(row);if(token!=null&&row.TokenHash==Hash(token))eligible.Add(row);}
        rows=eligible;
        if(rows.Count==0)throw new ValidationException("Nuk ka pajisje me njoftime aktive për këtë përzgjedhje.");
        var message=new ParentPushMessage{RequestId=input.RequestId,Title=input.Title.Trim(),Body=input.Body.Trim(),Audience=audience,CreatedBy=actor,
            Deliveries=rows.Select(x=>new ParentPushDelivery{SubscriptionId=x.Id}).ToList()};
        db.ParentPushMessages.Add(message);await db.SaveChangesAsync();await tx.CommitAsync();return rows.Count;
    }
}
