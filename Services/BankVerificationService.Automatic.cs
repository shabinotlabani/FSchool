using System.Text.Json;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class BankVerificationService
{
    // All callers share the billing lock. Only unpaid monthly fees belong here;
    // equipment, personal training and seasonal payments are never inferred.
    public async Task SyncAutomaticAsync(CancellationToken ct=default)
    {
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(ct):null;
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock,ct);
        var billing=new BillingService(db);
        var invoices=await billing.GetInvoicesAsync();
        var month=TariffService.CurrentMonth;
        var memberships=await new FamilyService(db).Latest(month).Where(x=>x.FamilyId!=null&&x.FeePlan!.IsFamily).ToDictionaryAsync(x=>x.StudentId,ct);
        var families=await db.PlayerFamilies.AsNoTracking().ToListAsync(ct);
        var pending=await db.PendingBankPayments.Where(x=>x.ReviewedAt==null||x.IsAutomatic&&x.AutomaticMonth==month).ToListAsync(ct);
        var activeKeys=new HashSet<(string Kind,int Id)>();
        var seasonalMonths=pending.Where(x=>x.Kind=="Seasonal"&&x.ReviewedAt==null).Select(x=>JsonSerializer.Deserialize<SeasonalPaymentModel>(x.Payload)!).ToList();

        void Upsert(string kind,int target,int? student,decimal due,string description,Func<Guid,object> payload)
        {
            if(due<=0)return;
            var key=(kind,target);activeKeys.Add(key);
            bool Matches(PendingBankPayment x)=>x.Kind==kind&&(kind=="Monthly"?x.StudentId==student:x.TargetId==target);
            var open=pending.FirstOrDefault(x=>Matches(x)&&x.ReviewedAt==null);
            if(open!=null&&!open.IsAutomatic)return;
            // A bank officer's rejection is final for this billing month.
            if(open==null&&pending.Any(x=>Matches(x)&&x.IsAutomatic&&x.AutomaticMonth==month&&x.ReviewedAt!=null&&x.ConfirmedAmount==null&&x.PaymentId==null&&x.ResultId==null&&x.ReviewedById!="System"))return;
            if(open==null){open=new(){RequestId=Guid.NewGuid(),Kind=kind,StudentId=student,TargetId=target,IsAutomatic=true,CreatedById="System"};db.PendingBankPayments.Add(open);pending.Add(open);}
            open.Amount=due;open.AutomaticMonth=month;open.Description=description;
            var value=payload(open.RequestId);open.Payload=JsonSerializer.Serialize(value,value.GetType());
        }

        foreach(var group in invoices.Where(x=>!memberships.ContainsKey(x.Student.Id)).GroupBy(x=>x.Student.Id)){
            var student=group.First().Student;
            if(!student.IsActive||student.PreferredPaymentMethod!="Bank")continue;
            var due=group.Where(x=>!seasonalMonths.Any(s=>s.StudentId==student.Id&&TariffService.TryMonth(s.StartMonth,out var start)&&new DateOnly(x.Period.Year,x.Period.Month,1)>=start&&new DateOnly(x.Period.Year,x.Period.Month,1)<start.AddMonths(s.Months))).Sum(x=>x.Due);
            Upsert("Monthly",student.Id,student.Id,due,"Mujorja · automatike",id=>new RegisterPaymentModel{RequestId=id,StudentId=student.Id,Amount=due,PaymentMethod="Bank"});
        }
        foreach(var family in families.Where(x=>x.PreferredPaymentMethod=="Bank")){
            var account=await billing.GetFamilyAccountAsync(family.Id);
            if(account==null||account.Due<=0)continue;
            Upsert("Family",family.Id,null,account.Due,family.Name+" · automatike",id=>new FamilyPaymentModel{RequestId=id,FamilyId=family.Id,Amount=account.Due,ExpectedState=account.State,PaymentMethod="Bank"});
        }
        foreach(var request in pending.Where(x=>x.IsAutomatic&&x.ReviewedAt==null)){
            var key=(request.Kind,request.Kind=="Monthly"?request.StudentId??0:request.TargetId??0);
            if(activeKeys.Contains(key))continue;
            request.ReviewedAt=DateTime.UtcNow;request.ReviewedById="System";
            request.RejectionReason="Mbyllur automatikisht: detyrimi u shlye ose mënyra e zakonshme ndryshoi.";
        }
        await db.SaveChangesAsync(ct);if(tx!=null)await tx.CommitAsync(ct);
    }
}
