using System.Net;
using System.Text.Json;
using _2Korriku.Data;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ParentPushWorker(IServiceScopeFactory scopes,ILogger<ParentPushWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(15)};
        var client=new PushServiceClient(http){AutoRetryAfter=false,DefaultTimeToLive=3600};
        while(!stoppingToken.IsCancellationRequested){
            try {
                for(var i=0;i<30&&!stoppingToken.IsCancellationRequested;i++){
                    using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await using var tx=await db.Database.BeginTransactionAsync(stoppingToken);
                    // Serialize workers across Railway replicas; one delivery is committed at a time.
                    await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)",stoppingToken);
                    var settings=await db.ParentPushSettings.SingleOrDefaultAsync(stoppingToken);if(settings==null)break;
                    var delivery=await db.ParentPushDeliveries.Include(x=>x.Message).Include(x=>x.Subscription.Student)
                        .Where(x=>x.SentAt==null&&!x.Failed&&x.NextAttempt<=DateTime.UtcNow).OrderBy(x=>x.Id).FirstOrDefaultAsync(stoppingToken);
                    if(delivery==null)break;
                    var sub=delivery.Subscription;var token=await new ParentPushService(db).AccessToken(sub);
                    if(!sub.Enabled||!sub.Student.IsActive||token==null||sub.TokenHash!=ParentPushService.Hash(token)||!ParentPushService.AllowedEndpoint(sub.Endpoint)
                        ||delivery.Message.CreatedAt<DateTime.UtcNow.AddDays(-1))delivery.Failed=true;
                    else {
                        delivery.Attempts++;
                        try {
                            using var auth=new VapidAuthentication(settings.PublicKey,settings.PrivateKey){Subject=settings.Subject};
                            var subscription=new PushSubscription{Endpoint=sub.Endpoint,Keys=new Dictionary<string,string>{{"p256dh",sub.P256dh},{"auth",sub.Auth}}};
                            var payload=JsonSerializer.Serialize(new{title=delivery.Message.Title,body=delivery.Message.Body,url="/p/"+token+"?studentId="+sub.StudentId,tag="parent-"+delivery.Id});
                            await client.RequestPushMessageDeliveryAsync(subscription,new PushMessage(payload),auth,stoppingToken);
                            delivery.SentAt=DateTime.UtcNow;
                        }catch(PushServiceClientException e){
                            if(e.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound){sub.Enabled=false;delivery.Failed=true;}
                            else if(e.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge)delivery.Failed=true;
                        }catch(Exception e)when(e is HttpRequestException or TaskCanceledException or ArgumentException){
                            if(stoppingToken.IsCancellationRequested)throw;
                        }
                        if(delivery.SentAt==null){delivery.Failed|=delivery.Attempts>=5;delivery.NextAttempt=DateTime.UtcNow.AddMinutes(Math.Pow(2,delivery.Attempts));}
                    }
                    await db.SaveChangesAsync(stoppingToken);await tx.CommitAsync(stoppingToken);
                }
            }catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception e){logger.LogWarning("Push worker deferred: {ErrorType}",e.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromSeconds(15),stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
