using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ReasonReportService(ApplicationDbContext db)
{
    public async Task<ReasonReportModel> LoadAsync(MoneyDateFilter filter,string? category=null,string? action=null,string? search=null)
    {
        Validator.ValidateObject(filter,new(filter),true);
        var start=BillingClock.UtcDate(filter.From!.Value);
        var end=BillingClock.UtcDate(filter.To!.Value.AddDays(1));
        var rows=new List<ReasonReportRow>();
        async Task Read(IQueryable<ReasonReportRow> query)=>rows.AddRange(await query.Where(x=>x.Date>=start&&x.Date<end).ToListAsync());
        // Ledger reversals retain the actual actor/date, including cancelled receipts
        // whose original documents do not store the cancelling user.
        var ledger=await db.StudentTransactions.AsNoTracking().Include(x=>x.Student)
            .Where(x=>x.CreatedAt>=start&&x.CreatedAt<end&&(x.TransactionType.EndsWith("Reversal")||x.TransactionType=="Exemption"||x.TransactionType=="SaleWaiver"||x.TransactionType=="TrainingWaiver")).ToListAsync();
        foreach(var x in ledger){
            var waiver=x.TransactionType.Contains("Waiver")||x.TransactionType.StartsWith("Exemption");
            rows.Add(new(){Date=x.CreatedAt,Category=x.TransactionType.StartsWith("Sale")?"Rekuizitat":x.TransactionType.StartsWith("Training")?"Trajnimet personale":x.TransactionType.StartsWith("Exemption")?"Lirimet mujore":"Pagesat",
                Action=waiver?(x.TransactionType.EndsWith("Reversal")?"Kthim lirimi":"Lirim"):"Anulim",Subject=x.Student?.FullName??$"Lojtari #{x.StudentId}",Document=$"Regjistrimi #{x.Id} · {x.Month:00}.{x.Year}",Reason=x.Description,Amount=Math.Max(x.Debit,x.Credit),ActorId=x.CreatedByUserId});
        }
        // Older cancelled receipts may have no ledger reversal. Do not invent
        // a cancelling actor or substitute the receipt's original payment date.
        await Read(db.Payments.AsNoTracking().Where(x=>x.IsCancelled&&x.CancelledAt!=null&&!db.StudentTransactions.Any(t=>t.ReferenceId==x.Id&&(t.ReferenceType=="Payment"||t.ReferenceType=="SalePayment"||t.ReferenceType=="TrainingPayment")&&t.TransactionType.EndsWith("Reversal")))
            .Select(x=>new ReasonReportRow{Date=x.CancelledAt!.Value,Category=x.SaleId!=null?"Rekuizitat":x.PersonalChargeId!=null?"Trajnimet personale":"Pagesat",Action="Anulim",Subject=x.Student!.FirstName+" "+x.Student.LastName,Document="Pagesa #"+x.Id,Reason=x.CancellationReason??"Arsye e paregjistruar",Amount=x.Amount}));
        await Read(db.Expenses.AsNoTracking().Where(x=>x.CancelledAt!=null).Select(x=>new ReasonReportRow{Date=x.CancelledAt!.Value,Category="Shpenzimet",Action="Anulim",Subject=x.CategoryName+" · "+x.Description,Document="Shpenzimi #"+x.Id,Reason=x.CancellationReason??"",Amount=x.Amount,ActorId=x.CancelledById}));
        await Read(db.TreasuryEntries.AsNoTracking().Where(x=>x.CancelledAt!=null).Select(x=>new ReasonReportRow{Date=x.CancelledAt!.Value,Category="Arkat",Action="Anulim",Subject=x.Kind==TreasuryEntry.Opening?"Gjendja fillestare":"Tërheqje nga pronari",Document="Arka #"+x.Id,Reason=x.CancellationReason??"",Amount=x.Amount,ActorId=x.CancelledById}));
        await Read(db.FieldBookingChanges.AsNoTracking().Where(x=>x.Description.StartsWith("Anul")).Select(x=>new ReasonReportRow{Date=x.CreatedAt,Category="Terminet",Action="Anulim",Subject=x.FieldBooking!.CustomerName,Document="Termini #"+x.FieldBookingId,Reason=x.Description,ActorId=x.ActorId}));
        await Read(db.FirstMonthFeeChanges.AsNoTracking().Select(x=>new ReasonReportRow{Date=x.CreatedAt,Category="Tarifat",Subject=x.Period!.Student!.FirstName+" "+x.Period.Student.LastName,Document="Muaji i parë #"+x.PeriodId,Reason=x.Reason+" · "+x.PreviousAmount+" → "+x.Amount,Amount=x.Amount,ActorId=x.ActorId}));
        // Fee assignments retain financial family-package changes; general profile audits are excluded.
        await Read(db.StudentFeeAssignments.AsNoTracking().Where(x=>x.Reason!="").Select(x=>new ReasonReportRow{Date=x.CreatedAt,Category=x.FeePlan!.IsWaiver?"Lirimet mujore":"Tarifat",Action=x.FeePlan.IsWaiver?"Lirim":"Ndryshim",Subject=x.Student!.FirstName+" "+x.Student.LastName,Document=x.FeePlan.Name+" · "+x.EffectiveMonth,Reason=x.Reason,ActorId=x.CreatedByUserId}));
        await Read(db.FeePlanPrices.AsNoTracking().Where(x=>x.Reason!="").Select(x=>new ReasonReportRow{Date=x.CreatedAt,Category="Tarifat",Subject=x.FeePlan!.Name,Document="Tarifa #"+x.Id,Reason=x.Reason,Amount=x.Amount,ActorId=x.CreatedByUserId}));
        await Read(db.PendingBankPayments.AsNoTracking().Where(x=>x.ReviewedAt!=null&&x.RejectionReason!=null).Select(x=>new ReasonReportRow{Date=x.ReviewedAt!.Value,Category="Banka",Action=x.ReviewedById=="System"?"Mbyllje automatike":"Refuzim",Subject=x.Student!=null?x.Student.FirstName+" "+x.Student.LastName:x.Description??"Kërkesë bankare",Document="Banka #"+x.Id,Reason=x.RejectionReason!,Amount=x.Amount,ActorId=x.ReviewedById}));
        var personal=await db.PersonalTrainingAudits.AsNoTracking().Include(x=>x.PersonalTraining).ThenInclude(x=>x!.Student).Include(x=>x.PersonalTariff).Where(x=>x.CreatedAt>=start&&x.CreatedAt<end).ToListAsync();
        foreach(var x in personal){
            string? operation=null;
            try{
                using var json=JsonDocument.Parse(x.Snapshot);
                var snapshot=json.RootElement;
                if(snapshot.TryGetProperty("Action",out var value))operation=value.GetString();
                if(x.PersonalTariffId.HasValue){
                    // Renaming a tariff or editing its notes does not affect billing.
                    if(!snapshot.TryGetProperty("Before",out var before)||!snapshot.TryGetProperty("After",out var after))continue;
                    // Mode cannot be edited and is absent from the historical Before snapshot.
                    var amountChanged=before.TryGetProperty("Amount",out var oldAmount)&&after.TryGetProperty("Amount",out var newAmount)&&
                        oldAmount.TryGetDecimal(out var previousAmount)&&newAmount.TryGetDecimal(out var nextAmount)&&previousAmount!=nextAmount;
                    var activeChanged=before.TryGetProperty("IsActive",out var oldActive)&&after.TryGetProperty("IsActive",out var newActive)&&oldActive.ValueKind!=newActive.ValueKind;
                    var financialChange=amountChanged||activeChanged;
                    if(!financialChange)continue;
                }
                else if(operation is not ("Cancel" or "Stop" or "Waive" or "Restore")&&!snapshot.TryGetProperty("Rate",out _))continue;
            }catch(JsonException){continue;}
            if(operation is "Waive" or "Restore")continue; // Included once through the ledger.
            rows.Add(new(){Date=x.CreatedAt,Category="Trajnimet personale",Action=operation=="Cancel"?"Anulim":"Ndryshim",Subject=x.PersonalTraining?.Student?.FullName??x.PersonalTariff?.Name??"Trajnim personal",Document=x.PersonalTrainingId.HasValue?$"Trajnimi #{x.PersonalTrainingId}":$"Tarifa #{x.PersonalTariffId}",Reason=x.Reason,ActorId=x.ActorId});
        }
        var actors=rows.Select(x=>x.ActorId).Where(x=>x!=null).Distinct().ToList();
        var users=await db.Users.AsNoTracking().Where(x=>actors.Contains(x.Id)).Select(x=>new{x.Id,x.FirstName,x.LastName,x.UserName}).ToListAsync();
        foreach(var row in rows){if(string.IsNullOrWhiteSpace(row.Reason))row.Reason="Arsye e paregjistruar";var user=users.FirstOrDefault(x=>x.Id==row.ActorId);row.Actor=row.ActorId=="System"?"Sistemi":user==null?"I paregjistruar":string.IsNullOrWhiteSpace(user.FirstName+user.LastName)?user.UserName??"Përdorues":(user.FirstName+" "+user.LastName).Trim();}
        search=search?.Trim();
        return new(){Filter=filter,Category=category,Action=action,Search=search,Rows=rows.Where(x=>(string.IsNullOrEmpty(category)||x.Category==category)&&(string.IsNullOrEmpty(action)||x.Action==action)&&(string.IsNullOrEmpty(search)||$"{x.Subject} {x.Document} {x.Reason} {x.Actor}".Contains(search,StringComparison.OrdinalIgnoreCase))).OrderBy(x=>Array.IndexOf(ReasonReportModel.Categories,x.Category)).ThenByDescending(x=>x.Date).ThenBy(x=>x.Document).ToList()};
    }
}
