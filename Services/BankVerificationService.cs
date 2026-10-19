using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public partial class BankVerificationService(ApplicationDbContext db)
{
    internal static Task<bool> IsConfirmedAmount(ApplicationDbContext db,Guid request,string kind,decimal amount,string actor) =>
        db.Database.CurrentTransaction==null?Task.FromResult(false):db.PendingBankPayments.AnyAsync(x=>x.RequestId==request&&x.Kind==kind&&x.ReviewedAt!=null&&x.ReviewedById==actor&&x.ConfirmedAmount==amount&&x.PaymentId==null&&x.ResultId==null);
    public Task<int> QueueMonthly(RegisterPaymentModel input,string actor)=>Queue("Monthly",new RegisterPaymentModel{StudentId=input.StudentId,RequestId=input.RequestId,Amount=input.Amount,PaymentDate=input.PaymentDate,PaymentMethod="Bank",Notes=input.Notes?.Trim()},actor);
    public Task<int> QueueSeasonal(SeasonalPaymentModel input,string actor)=>Queue("Seasonal",new SeasonalPaymentModel{StudentId=input.StudentId,RequestId=input.RequestId,StartMonth=input.StartMonth,Months=input.Months,ExpectedRate=input.ExpectedRate,PaymentDate=input.PaymentDate,PaymentMethod="Bank",Notes=input.Notes?.Trim()},actor);
    private async Task CheckActor(string actor,bool admin,bool fieldPayment=false)
    {
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor);
        var roles=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==actor select r.Name).ToListAsync();
        if(user==null||UserAdministrationService.IsDisabled(user)||!(roles.Contains(Roles.Admin)||!admin&&(roles.Contains(Roles.Operator)||fieldPayment&&roles.Contains(Roles.FieldOperator))))throw new InvalidOperationException("Nuk keni të drejtë për këtë veprim.");
    }
    private async Task<int> Queue<T>(string kind,T input,string actor) where T:class
    {
        Validator.ValidateObject(input,new(input),true);
        var monthly=input as RegisterPaymentModel;var seasonal=input as SeasonalPaymentModel;
        var studentId=monthly?.StudentId??seasonal!.StudentId;
        var requestId=monthly?.RequestId??seasonal!.RequestId;
        var payload=JsonSerializer.Serialize(input);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        await CheckActor(actor,false);
        var existing=await db.PendingBankPayments.SingleOrDefaultAsync(x=>x.RequestId==requestId);
        if(existing!=null){if(existing.Payload!=payload||existing.CreatedById!=actor||existing.Kind!=kind)throw new InvalidOperationException("Formulari është përdorur. Rihapeni.");return existing.Id;}
        if(await db.Payments.AnyAsync(x=>x.RequestId==requestId))throw new InvalidOperationException("Kjo pagesë është regjistruar tashmë.");
        if(!await db.Students.AnyAsync(x=>x.Id==studentId))throw new InvalidOperationException("Lojtari nuk u gjet.");
        if(await db.PendingBankPayments.AnyAsync(x=>x.StudentId==studentId&&x.Kind==kind&&x.ReviewedAt==null))throw new InvalidOperationException("Ky lojtar ka një pagesë bankare në pritje. Verifikojeni ose refuzojeni para një kërkese tjetër.");
        var billing=new BillingService(db);
        decimal amount;
        if(seasonal!=null){var quote=await billing.QuoteSeasonalAsync(seasonal);if(quote.Rate!=seasonal.ExpectedRate)throw new InvalidOperationException("Tarifa ka ndryshuar. Rillogaritni sezonin.");amount=quote.Total;}
        else{if(await billing.CurrentFamilyIdAsync(studentId)!=null)throw new InvalidOperationException("Përdorni pagesën familjare.");amount=monthly!.Amount!.Value;}
        var pending=new PendingBankPayment{StudentId=studentId,RequestId=requestId,Kind=kind,Amount=amount,Payload=payload,CreatedById=actor};
        db.PendingBankPayments.Add(pending);await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();return pending.Id;
    }
    public async Task<int?> Review(BankReviewModel input,string actor,bool approve)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        // Only field operations need the schedule lock, before the money lock.
        if(await db.PendingBankPayments.AnyAsync(x=>x.Id==input.Id&&x.Kind=="Field"))await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        if(!await CanReview(actor))throw new InvalidOperationException("Nuk keni të drejtë të verifikoni bankën.");
        var pending=await db.PendingBankPayments.SingleOrDefaultAsync(x=>x.Id==input.Id)??throw new InvalidOperationException("Kërkesa nuk u gjet.");
        if(pending.ReviewedAt.HasValue){if(approve&&(pending.PaymentId??pending.ResultId).HasValue){if(input.ReceivedAmount.HasValue&&input.ReceivedAmount!=(pending.ConfirmedAmount??pending.Amount))throw new InvalidOperationException("Kjo kërkesë është konfirmuar me një shumë tjetër.");return pending.PaymentId??pending.ResultId;}throw new InvalidOperationException("Kërkesa është trajtuar më parë.");}
        if(!approve){if(string.IsNullOrWhiteSpace(input.Reason)||input.Reason.Length>250)throw new InvalidOperationException("Shkruani arsyen e refuzimit (deri 250 karaktere).");pending.RejectionReason=input.Reason.Trim();}
        else{
            if(pending.IsAutomatic&&input.ExpectedAmount!=pending.Amount)throw new InvalidOperationException("Shuma ka ndryshuar. Rifreskoni listën dhe kontrolloni shumën para konfirmimit.");
            if(await db.Payments.AnyAsync(x=>x.RequestId==pending.RequestId)||await db.FieldPayments.AnyAsync(x=>x.RequestId==pending.RequestId)||await db.FamilyPayments.AnyAsync(x=>x.RequestId==pending.RequestId)||await db.Expenses.AnyAsync(x=>x.RequestId==pending.RequestId)||await db.TreasuryEntries.AnyAsync(x=>x.RequestId==pending.RequestId))throw new InvalidOperationException("Ky formular është përdorur për një pagesë tjetër. Kontrolloni kartelën dhe refuzoni kërkesën e dyfishtë.");
            if(!input.ReceivedOn.HasValue||input.ReceivedOn>BillingClock.Today||input.ReceivedOn<new DateOnly(2000,1,1))throw new InvalidOperationException("Zgjidhni datën reale të veprimit në bankë, jo në të ardhmen.");
            var received=input.ReceivedAmount??pending.Amount;
            if(received<=0||received>999999999.99m||decimal.Round(received,2)!=received)throw new InvalidOperationException("Shuma e pranuar duhet të jetë pozitive, deri në 999999999.99, me dy shifra dhjetore.");
            BankClientBalance? client=null;
            if(input.AllocateAll&&SupportsDistribution(pending.Kind)){
                client=await ClientBalance(pending);
                if(input.ExpectedClientState!=client.State)throw new InvalidOperationException("Detyrimet kanë ndryshuar. Rifreskoni listën para konfirmimit.");
            }
            if(client==null&&pending.Kind=="Seasonal"&&received!=pending.Amount)throw new InvalidOperationException("Për këtë dokument konfirmoni shumën e kërkesës.");
            // Close this request before posting, so automatic reconciliation can
            // create a distinct request for any remainder. The outer transaction
            // rolls back both the review and the receipt on failure.
            pending.ConfirmedAmount=received;pending.ReviewedAt=DateTime.UtcNow;pending.ReviewedById=actor;
            await db.SaveChangesAsync();
            var billing=new BillingService(db);
            if(client!=null){await Distribute(pending,client,input.ReceivedOn.Value,actor);}
            else if(pending.Kind=="Seasonal"){
                var model=JsonSerializer.Deserialize<SeasonalPaymentModel>(pending.Payload)!;model.PaymentDate=input.ReceivedOn;
                pending.PaymentId=await billing.RegisterSeasonalAsync(model,actor);
            }else if(pending.Kind=="Monthly"){
                var model=JsonSerializer.Deserialize<RegisterPaymentModel>(pending.Payload)!;model.PaymentDate=input.ReceivedOn;model.Amount=received;
                pending.PaymentId=await billing.RegisterPaymentAsync(model,actor);
            }else await ConfirmAdditional(pending,input.ReceivedOn.Value,actor);
        }
        if(approve)pending.RejectionReason=null;
        pending.ReviewedAt=DateTime.UtcNow;pending.ReviewedById=actor;
        await db.SaveChangesAsync();await tx.CommitAsync();return pending.PaymentId??pending.ResultId;
    }
}
