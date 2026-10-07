using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ExpenseService(ApplicationDbContext db)
{
    private async Task<T> Write<T>(Func<Task<T>> action)
    {
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        var result=await action();
        if(tx!=null)await tx.CommitAsync();
        return result;
    }
    public Task<int> SaveCategoryAsync(ExpenseCategoryModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        var category=m.Id==0?new ExpenseCategory():await db.ExpenseCategories.SingleOrDefaultAsync(x=>x.Id==m.Id)??throw new InvalidOperationException("Kategoria nuk u gjet.");
        if(m.Id!=0){await db.Entry(category).ReloadAsync();if(category.Revision!=m.Revision)throw new InvalidOperationException("Kategoria ka ndryshuar. Rihapni formularin.");if(category.ParentId!=m.ParentId)throw new InvalidOperationException("Kategoria prind nuk ndryshohet. Krijoni nënkategori të re.");}
        if(m.ParentId.HasValue){
            var parent=await db.ExpenseCategories.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==m.ParentId);
            if(parent==null||parent.ParentId.HasValue||m.ParentId==m.Id||(!parent.IsActive&&m.IsActive))throw new InvalidOperationException("Zgjidhni kategori kryesore aktive. Lejohen vetëm kategori dhe nënkategori.");
        }
        var name=m.Name.Trim();
        if(await db.ExpenseCategories.AnyAsync(x=>x.Id!=m.Id&&x.ParentId==m.ParentId&&x.Name.ToLower()==name.ToLower()))throw new InvalidOperationException("Ky emër ekziston në këtë kategori.");
        var before=JsonSerializer.Serialize(new{category.Name,category.IsActive,category.ParentId});
        category.Name=name;category.ParentId=m.ParentId;category.IsActive=m.IsActive;category.Revision=Guid.NewGuid();
        if(m.Id==0)db.ExpenseCategories.Add(category);
        db.ExpenseCategoryChanges.Add(new(){ExpenseCategory=category,Reason=m.Reason?.Trim()??"Krijim kategorie",Before=before,After=JsonSerializer.Serialize(new{category.Name,category.IsActive,category.ParentId}),ActorId=actor});
        await db.SaveChangesAsync();return category.Id;
    });
    private static string NormalizeMethod(string? method) => method?.Trim().ToUpperInvariant() switch { "CASH" => "Cash", "BANK" => "Bank", _ => "Other" };
    // Actual receipts only: invoices, stock entries and outstanding debts never create money.
    private async Task<List<MoneyMovement>> MovementsAsync()
    {
        var payments=await db.Payments.AsNoTracking().Include(x=>x.Student).ToListAsync();
        var expenses=await db.Expenses.AsNoTracking().ToListAsync();
        var result=new List<MoneyMovement>();
        foreach (var entry in await db.TreasuryEntries.AsNoTracking().ToListAsync())
        {
            var opening = entry.Kind == TreasuryEntry.Opening;
            result.Add(new(entry.Date, entry.Label, "", null, opening ? entry.Amount : 0, opening ? 0 : entry.Amount,
                entry.Number, entry.Owner ?? entry.Label, "Treasury", "Details", entry.Id, entry.Method));
            if (entry.CancelledAt.HasValue)
                result.Add(new(BillingClock.LocalDate(entry.CancelledAt.Value), entry.Label, "", null, opening ? -entry.Amount : 0, opening ? 0 : -entry.Amount,
                    entry.Number, "Anulim: " + entry.CancellationReason, "Treasury", "Details", entry.Id, entry.Method));
        }
        var fieldPayments = await db.FieldPayments.AsNoTracking().ToListAsync();
        foreach (var p in fieldPayments)
        {
            result.Add(new(BillingClock.LocalDate(p.CreatedAt), "Termine private", "", null, p.Amount, 0, p.Number, p.Description, "Fields", "Receipt", p.Id, NormalizeMethod(p.Method)));
            if (p.CancelledAt.HasValue) result.Add(new(BillingClock.LocalDate(p.CancelledAt.Value), "Termine private", "", null, -p.Amount, 0, p.Number, "Anulim: " + p.CancellationReason, "Fields", "Receipt", p.Id, NormalizeMethod(p.Method)));
        }
        foreach(var p in payments){
            var group=p.SaleId.HasValue?"Shitje të rekuizitave":p.PersonalChargeId.HasValue?"Trajnime shtesë":p.SeasonalStartMonth.HasValue?"Pagesa sezonale":"Mujorja e rregullt";
            var date=BillingClock.LocalDate(p.PaymentDate);
            var reference=$"ARK-{date.Year}-{p.Id:000000}";
            result.Add(new(date,group,"",null,p.Amount,0,reference,p.Student?.FullName??"Arkëtim","Payments","Receipt",p.Id,NormalizeMethod(p.PaymentMethod)));
            if(p.IsCancelled)result.Add(new(BillingClock.LocalDate(p.CancelledAt??p.PaymentDate),group,"",null,-p.Amount,0,reference,"Anulim: "+p.CancellationReason,"Payments","Receipt",p.Id,NormalizeMethod(p.PaymentMethod)));
        }
        // Older sales can carry paid money without a Payment row. Count only the
        // unrepresented paid part, never the sale total and never the same receipt twice.
        var legacySales=await db.Sales.AsNoTracking().Where(x=>x.RequestId==null&&x.AmountPaid>0&&x.Status=="Completed").ToListAsync();
        foreach(var sale in legacySales){
            var represented=payments.Where(p=>p.SaleId==sale.Id&&!p.IsCancelled).Sum(p=>p.Amount);
            var residual=Math.Max(0,sale.AmountPaid-represented);
            if(residual>0)result.Add(new(BillingClock.LocalDate(sale.Date),"Shitje të rekuizitave","",null,residual,0,sale.InvoiceNumber,"Arkëtim i vjetër i regjistruar te dalja","Sales","Details",sale.Id,NormalizeMethod(sale.PaymentMethod)));
        }
        foreach(var e in expenses){
            result.Add(new(e.Date,"Shpenzime",e.CategoryName,e.SubcategoryName,0,e.Amount,e.Number,e.Description,"Expenses","Details",e.Id,NormalizeMethod(e.Method)));
            if(e.CancelledAt.HasValue)result.Add(new(BillingClock.LocalDate(e.CancelledAt.Value),"Shpenzime",e.CategoryName,e.SubcategoryName,0,-e.Amount,e.Number,"Anulim: "+e.CancellationReason,"Expenses","Details",e.Id,NormalizeMethod(e.Method)));
        }
        return result;
    }
    public async Task<decimal> AvailableAsync()
    {
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead):null;
        var value=(await MovementsAsync()).Where(x=>x.Date<=BillingClock.Today).Sum(x=>x.Income-x.Outgoing);
        if(tx!=null)await tx.CommitAsync();return value;
    }
    internal async Task<decimal> AvailableFromAsync(DateOnly date, string? method = null)
    {
        var movements=(await MovementsAsync()).Where(x=>x.Date<=BillingClock.Today && (method == null || x.Method == method)).ToList();
        var balance=movements.Where(x=>x.Date<date).Sum(x=>x.Income-x.Outgoing);
        // Include the full selected day's receipts before testing daily closing funds.
        var days=movements.Where(x=>x.Date>=date).GroupBy(x=>x.Date).ToDictionary(x=>x.Key,x=>x.Sum(v=>v.Income-v.Outgoing));
        balance+=days.GetValueOrDefault(date);
        var minimum=balance;
        foreach(var day in days.Where(x=>x.Key>date).OrderBy(x=>x.Key)){balance+=day.Value;minimum=Math.Min(minimum,balance);}
        return minimum;
    }
    public Task<int> CreateAsync(ExpenseCreateModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{m.CategoryId,m.SubcategoryId,m.Date,m.Amount,Description=m.Description.Trim(),Recipient=m.Recipient?.Trim(),Document=m.DocumentNumber?.Trim(),m.Method,Notes=m.Notes?.Trim()}))));
        var existing=await db.Expenses.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==m.RequestId);
        if(existing!=null){if(existing.PayloadHash!=hash||existing.CreatedById!=actor)throw new InvalidOperationException("Ky formular është përdorur. Hapni një shpenzim të ri.");return existing.Id;}
        var category=await db.ExpenseCategories.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==m.CategoryId&&x.ParentId==null&&x.IsActive)??throw new InvalidOperationException("Zgjidhni kategori kryesore aktive.");
        ExpenseCategory? sub=null;
        if(m.SubcategoryId.HasValue)sub=await db.ExpenseCategories.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==m.SubcategoryId&&x.ParentId==category.Id&&x.IsActive)??throw new InvalidOperationException("Nënkategoria nuk i përket kategorisë ose është joaktive.");
        var available=await AvailableFromAsync(m.Date!.Value);
        if(m.Amount>available)throw new InvalidOperationException($"Mjete të pamjaftueshme. Për këtë datë mund të regjistroni deri në {Math.Max(0,available):N2} €. Kontrolli përfshin edhe shpenzimet e regjistruara në ditët pasuese.");
        var e=new Expense{RequestId=m.RequestId,PayloadHash=hash,CategoryId=category.Id,SubcategoryId=sub?.Id,CategoryName=category.Name,SubcategoryName=sub?.Name,Date=m.Date.Value,Amount=m.Amount,Description=m.Description.Trim(),Recipient=m.Recipient?.Trim(),DocumentNumber=m.DocumentNumber?.Trim(),Method=m.Method,Notes=m.Notes?.Trim(),CreatedById=actor};
        db.Expenses.Add(e);await db.SaveChangesAsync();return e.Id;
    });
    public Task<int> CancelAsync(ExpenseCancelModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        var expense=await db.Expenses.SingleOrDefaultAsync(x=>x.Id==m.Id)??throw new InvalidOperationException("Shpenzimi nuk u gjet.");
        await db.Entry(expense).ReloadAsync();
        if(expense.CancelledAt.HasValue)return expense.Id;
        expense.CancelledAt=DateTime.UtcNow;expense.CancelledById=actor;expense.CancellationReason=m.Reason.Trim();
        await db.SaveChangesAsync();return expense.Id;
    });
    // BillingService holds the same lock. A spent receipt cannot be removed while
    // retaining expenses that would leave the club with a negative money balance.
    internal async Task EnsureReceiptCancellationAsync(Payment payment)
    {
        if(payment.Amount>await AvailableAsync())throw new InvalidOperationException("Arkëtimi nuk mund të anulohet: mjetet janë përdorur për shpenzime. Kontrolloni dhe korrigjoni fillimisht shpenzimet përkatëse.");
    }
    public async Task<CashReportModel> ReportAsync(MoneyDateFilter filter)
    {
        Validator.ValidateObject(filter,new(filter),true);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead):null;
        var all=await MovementsAsync();
        var rows=all.Where(x=>x.Date>=filter.From&&x.Date<=filter.To).OrderBy(x=>x.Date).ThenBy(x=>x.Reference).ToList();
        var groups=new[]{"Mujorja e rregullt","Pagesa sezonale","Trajnime shtesë","Shitje të rekuizitave","Termine private"};
        var result=new CashReportModel{HasUndatedCancellations=await db.Payments.AnyAsync(x=>x.IsCancelled&&x.CancelledAt==null),Filter=filter,Opening=all.Where(x=>x.Date<filter.From).Sum(x=>x.Income-x.Outgoing),Movements=rows,
            IncomeRows=groups.Select(g=>new MoneyReportRow(g,rows.Where(x=>x.Group==g&&x.Income>0).Sum(x=>x.Income),-rows.Where(x=>x.Group==g&&x.Income<0).Sum(x=>x.Income))).ToList()};
        var methods = new List<(string Code, string Name)> { ("Cash", "Cash"), ("Bank", "Bankë") };
        if (all.Any(x => x.Method == "Other" && x.Date <= filter.To)) methods.Add(("Other", "Tjetër / e papërcaktuar"));
        result.MethodBalances = methods.Select(m => new MoneyMethodBalance(m.Code, m.Name,
            all.Where(x => x.Method == m.Code && x.Date < filter.From).Sum(x => x.Income - x.Outgoing),
            rows.Where(x => x.Method == m.Code && x.Group != TreasuryEntry.OpeningGroup).Sum(x => x.Income),
            rows.Where(x => x.Method == m.Code).Sum(x => x.Outgoing),
            rows.Where(x => x.Method == m.Code && x.Group == TreasuryEntry.OpeningGroup).Sum(x => x.Income))).ToList();
        if(tx!=null)await tx.CommitAsync();return result;
    }
}
