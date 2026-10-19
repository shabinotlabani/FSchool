using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public partial class BankVerificationService
{
    public static bool SupportsDistribution(string kind) => kind is "Monthly" or "Family" or "Sale" or "Personal" or "Seasonal";

    public async Task<BankClientBalance> ClientBalance(PendingBankPayment request)
    {
        var billing=new BillingService(db);
        var result=new BankClientBalance();
        result.FamilyId=request.Kind=="Family"?request.TargetId:request.StudentId.HasValue?await billing.CurrentFamilyIdAsync(request.StudentId.Value):null;
        if(result.FamilyId.HasValue){
            result.StudentIds=(await new FamilyService(db).MembersAsync(result.FamilyId.Value,TariffService.CurrentMonth)).Select(x=>x.StudentId).Order().ToList();
        }else if(request.StudentId.HasValue)result.StudentIds.Add(request.StudentId.Value);
        if(result.StudentIds.Count==0)throw new InvalidOperationException("Klienti nuk ka llogari aktive për këtë pagesë.");
        SeasonalQuote? season=null;
        if(request.Kind=="Seasonal"){
            var input=JsonSerializer.Deserialize<SeasonalPaymentModel>(request.Payload)!;
            season=await billing.QuoteSeasonalAsync(input);
            if(season.Rate!=input.ExpectedRate)throw new InvalidOperationException("Tarifa sezonale ka ndryshuar. Rihapni kërkesën.");
        }
        foreach(var studentId in result.StudentIds){
            var invoices=await billing.GetInvoicesAsync(studentId);
            var due=invoices.Where(x=>season==null||new DateOnly(x.Period.Year,x.Period.Month,1)<season.Start||new DateOnly(x.Period.Year,x.Period.Month,1)>season.End).Sum(x=>x.Due);
            if(due>0)result.Items.Add(new("Monthly",studentId,studentId,due));
        }
        if(season!=null)result.Items.Add(new("Seasonal",request.Id,request.StudentId!.Value,season.Total));
        var sales=await db.Sales.AsNoTracking().Where(x=>x.StudentId.HasValue&&result.StudentIds.Contains(x.StudentId.Value)&&x.Status!="Cancelled"&&x.RequestId!=null&&x.GrandTotal>x.AmountPaid).OrderBy(x=>x.Date).ThenBy(x=>x.Id).ToListAsync();
        result.Items.AddRange(sales.Select(x=>new BankDebtItem("Sale",x.Id,x.StudentId!.Value,x.Due)));
        var charges=await db.PersonalCharges.AsNoTracking().Include(x=>x.PersonalTraining).Where(x=>!x.IsCancelled&&result.StudentIds.Contains(x.PersonalTraining!.StudentId)&&x.Amount>x.Paid+x.Waived).OrderBy(x=>x.Period).ThenBy(x=>x.Id).ToListAsync();
        result.Items.AddRange(charges.Select(x=>new BankDebtItem("Personal",x.Id,x.PersonalTraining!.StudentId,x.Due)));
        result.State=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{result.FamilyId,result.StudentIds,result.Items}))));
        return result;
    }

    private async Task Distribute(PendingBankPayment request,BankClientBalance balance,DateOnly date,string actor)
    {
        var billing=new BillingService(db);
        var remaining=request.ConfirmedAmount!.Value;
        using var payload=JsonDocument.Parse(request.Payload);
        var notes=payload.RootElement.TryGetProperty("Notes",out var note)&&note.ValueKind==JsonValueKind.String?note.GetString():null;
        var allocations=new List<BankPaymentAllocation>();
        var first=true;
        Guid NextRequest(){if(first){first=false;return request.RequestId;}return Guid.NewGuid();}
        async Task Add(int paymentId,string label){
            var payment=await db.Payments.SingleAsync(x=>x.Id==paymentId);
            payment.PaymentDate=BillingClock.UtcDate(date);
            foreach(var entry in await db.StudentTransactions.Where(x=>x.ReferenceId==paymentId&&(x.ReferenceType=="SalePayment"||x.ReferenceType=="TrainingPayment")).ToListAsync()){
                entry.TransactionDate=payment.PaymentDate;if(entry.ReferenceType=="SalePayment"){entry.Year=date.Year;entry.Month=date.Month;}
            }
            allocations.Add(new(){BankPaymentId=request.Id,PaymentId=paymentId,Amount=payment.Amount,Label=label});
        }
        async Task Monthly(decimal amount,string label){
            if(amount<=0)return;
            if(balance.FamilyId.HasValue){
                var family=await billing.GetFamilyAccountAsync(balance.FamilyId.Value)??throw new InvalidOperationException("Familja nuk u gjet.");
                var id=await billing.RegisterFlexibleFamilyBankPaymentAsync(new(){FamilyId=family.Family.Id,Amount=amount,ExpectedState=family.State,PaymentDate=date,PaymentMethod="Bank",RequestId=NextRequest(),Notes=notes},actor);
                foreach(var paymentId in await db.Payments.Where(x=>x.FamilyPaymentId==id).Select(x=>x.Id).ToListAsync())await Add(paymentId,label);
            }else{
                var id=await billing.RegisterPaymentAsync(new(){StudentId=balance.StudentIds.Single(),Amount=amount,PaymentDate=date,PaymentMethod="Bank",RequestId=NextRequest(),Notes=notes},actor);
                await Add(id,label);
            }
            remaining-=amount;
        }
        await Monthly(Math.Min(remaining,balance.Items.Where(x=>x.Kind=="Monthly").Sum(x=>x.Due)),"Mujore");
        foreach(var item in balance.Items.Where(x=>x.Kind!="Monthly")){
            if(remaining<=0)break;
            var amount=Math.Min(remaining,item.Due);
            int id;
            if(item.Kind=="Seasonal"){
                var seasonal=JsonSerializer.Deserialize<SeasonalPaymentModel>(request.Payload)!;seasonal.PaymentDate=date;seasonal.RequestId=NextRequest();
                id=await billing.RegisterSeasonalBankAmountAsync(seasonal,actor,amount);
            }else if(item.Kind=="Sale"){
                id=await new IssueService(db).PayAsync(new(){SaleId=item.Id,Amount=amount,PaymentMethod="Bank",RequestId=NextRequest(),Notes=notes},actor);
            }else{
                id=await new PersonalTrainingService(db).PayAsync(new(){ChargeId=item.Id,Amount=amount,PaymentMethod="Bank",RequestId=NextRequest(),Notes=notes},actor);
            }
            await Add(id,item.Kind=="Sale"?"Rekuizita":item.Kind=="Personal"?"Trajnim personal":"Sezonale");
            remaining-=amount;
        }
        await Monthly(remaining,"Parapagim");
        if(allocations.Sum(x=>x.Amount)!=request.ConfirmedAmount)throw new InvalidOperationException("Ndarja e pagesës nuk përputhet me shumën bankare.");
        db.BankPaymentAllocations.AddRange(allocations);
        request.PaymentId=allocations.First().PaymentId;
        await db.SaveChangesAsync();
        // Other requests for the same settled documents are no longer payable.
        var others=await db.PendingBankPayments.Where(x=>x.Id!=request.Id&&x.ReviewedAt==null&&((x.StudentId.HasValue&&balance.StudentIds.Contains(x.StudentId.Value))||(balance.FamilyId.HasValue&&x.Kind=="Family"&&x.TargetId==balance.FamilyId))).ToListAsync();
        foreach(var other in others.Where(x=>x.Kind is "Sale" or "Personal")){
            var due=other.Kind=="Sale"?(await db.Sales.SingleAsync(x=>x.Id==other.TargetId)).Due:(await db.PersonalCharges.SingleAsync(x=>x.Id==other.TargetId)).Due;
            if(due<=0){other.ReviewedAt=DateTime.UtcNow;other.ReviewedById="System";other.RejectionReason="Detyrimi u shlye nga pagesa bankare #"+request.Id;}
            else if(due<other.Amount){other.Amount=due;}
        }
        await db.SaveChangesAsync();
        await SyncAutomaticAsync();
    }
}
