using System.ComponentModel.DataAnnotations;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Services;

public partial class BillingService
{
    public Task<int> ChangeFirstMonthAsync(FirstMonthFeeModel input,string actor)=>WriteAsync(async()=>{
        Validator.ValidateObject(input,new(input),true);
        var duplicate=await db.FirstMonthFeeChanges.AsNoTracking().Include(x=>x.Period).SingleOrDefaultAsync(x=>x.RequestId==input.RequestId);
        if(duplicate!=null){
            if(duplicate.PeriodId!=input.PeriodId||duplicate.Amount!=input.Amount||duplicate.ActorId!=actor||duplicate.Reason!=input.Reason.Trim())throw new InvalidOperationException("Formulari është përdorur. Rihapeni.");
            return duplicate.Period!.StudentId;
        }
        var period=await db.MonthlyFeePeriods.SingleOrDefaultAsync(x=>x.Id==input.PeriodId)??throw new InvalidOperationException("Fletëpagesa nuk u gjet.");
        await db.Entry(period).ReloadAsync();
        var invoice=(await GetInvoicesAsync(period.StudentId)).Single(x=>x.Period.Id==period.Id);
        if(!invoice.CanEditFirstMonth)throw new InvalidOperationException("Ndryshimi lejohet vetëm për muajin e regjistrimit, brenda muajit aktual, pa pagesa, pa lirim dhe jashtë sezonit.");
        if(invoice.Amount!=input.ExpectedAmount)throw new InvalidOperationException("Shuma ka ndryshuar ndërkohë. Rihapni formularin.");
        if(input.Amount==invoice.Amount)return period.StudentId;
        var registration=BillingClock.LocalDate(invoice.Student.RegistrationDate);
        var full=period.FullMonthlyAmount??(await new TariffService(db).ResolveAsync(invoice.Student,new(period.Year,period.Month,1))).Amount;
        period.FullMonthlyAmount=full;period.ProratedFrom=registration;
        period.FirstMonthWeeks??=FirstMonthPricing.Weeks(registration);
        period.FirstMonthSuggested??=FirstMonthPricing.Calculate(full,registration);
        period.FirstMonthFinal=input.Amount;period.FirstMonthReason=input.Reason.Trim();
        var change=new FirstMonthFeeChange{RequestId=input.RequestId,PeriodId=period.Id,PreviousAmount=invoice.Amount,Amount=input.Amount,Reason=input.Reason.Trim(),ActorId=actor};
        db.FirstMonthFeeChanges.Add(change);await db.SaveChangesAsync();
        var difference=input.Amount-invoice.Amount;
        var description=$"Muaji i parë {invoice.Amount:N2} → {input.Amount:N2} €: {change.Reason}";
        db.StudentTransactions.Add(Entry(period.StudentId,period.Year,period.Month,"MonthlyFeeAdjustment",description[..Math.Min(250,description.Length)],Math.Max(0,difference),Math.Max(0,-difference),"FirstMonthFeeChange",change.Id,actor));
        await db.SaveChangesAsync();return period.StudentId;
    });

    public async Task ApplyFirstMonthOverrideAsync(int periodId,decimal? amount,string? reason,string actor)
    {
        if(!amount.HasValue)return;
        var period=await db.MonthlyFeePeriods.AsNoTracking().SingleAsync(x=>x.Id==periodId);
        var invoice=(await GetInvoicesAsync(period.StudentId)).Single(x=>x.Period.Id==periodId);
        if(invoice.Period.IsFeeWaived&&amount==0)return;
        await ChangeFirstMonthAsync(new(){PeriodId=periodId,Amount=amount.Value,ExpectedAmount=invoice.Amount,Reason=reason??""},actor);
    }
}
