using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class BankVerificationService
{
    public async Task<bool> CanReview(string actor)
    {
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor);
        if(user==null||UserAdministrationService.IsDisabled(user))return false;
        var roles=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==actor select r.Name).ToListAsync();
        return roles.Contains(Roles.Admin)||(roles.Contains(Roles.Operator)&&user.CanVerifyBank);
    }
    public Task<int> QueueSale(IssuePaymentModel m,string actor)=>QueueAdditional(new IssuePaymentModel{SaleId=m.SaleId,RequestId=m.RequestId,Amount=m.Amount,PaymentMethod="Bank",Notes=m.Notes?.Trim()},actor);
    public Task<int> QueuePersonal(PersonalPaymentModel m,string actor)=>QueueAdditional(new PersonalPaymentModel{ChargeId=m.ChargeId,RequestId=m.RequestId,Amount=m.Amount,PaymentMethod="Bank",Notes=m.Notes?.Trim()},actor);
    public Task<int> QueueField(FieldPaymentModel m,string actor)=>QueueAdditional(new FieldPaymentModel{WholeSeries=m.WholeSeries,ExpectedState=m.ExpectedState,BookingId=m.BookingId,Revision=m.Revision,RequestId=m.RequestId,Amount=m.Amount,Method="Bank"},actor);
    public Task<int> QueueFamily(FamilyPaymentModel m,string actor)=>QueueAdditional(new FamilyPaymentModel{FamilyId=m.FamilyId,ExpectedState=m.ExpectedState,StudentId=m.StudentId,RequestId=m.RequestId,Amount=m.Amount,PaymentDate=m.PaymentDate,PaymentMethod="Bank",Notes=m.Notes?.Trim()},actor);
    public Task<int> QueueExpense(ExpenseCreateModel m,string actor)=>QueueAdditional(new ExpenseCreateModel{RequestId=m.RequestId,CategoryId=m.CategoryId,SubcategoryId=m.SubcategoryId,MatchGameId=m.MatchGameId,Date=m.Date,Amount=m.Amount,Description=m.Description.Trim(),Recipient=m.Recipient?.Trim(),DocumentNumber=m.DocumentNumber?.Trim(),Method="Bank",Notes=m.Notes?.Trim()},actor);
    public Task<int> QueueTreasury(TreasuryCreateModel m,string actor)=>QueueAdditional(new TreasuryCreateModel{RequestId=m.RequestId,Kind=m.Kind,Date=m.Date,Amount=m.Amount,Owner=m.Owner?.Trim(),Notes=m.Notes?.Trim(),Method="Bank"},actor);

    private async Task<int> QueueAdditional<T>(T model,string actor) where T:class
    {
        Validator.ValidateObject(model,new(model),true);
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        if(model is FieldPaymentModel)await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        await CheckActor(actor,model is TreasuryCreateModel,model is FieldPaymentModel);
        var (kind,request,amount,target)=model switch {
            IssuePaymentModel m => ("Sale",m.RequestId,m.Amount,m.SaleId),
            PersonalPaymentModel m => ("Personal",m.RequestId,m.Amount,m.ChargeId),
            FieldPaymentModel m => ("Field",m.RequestId,m.Amount,m.BookingId),
            FamilyPaymentModel m => ("Family",m.RequestId,m.Amount!.Value,m.FamilyId),
            ExpenseCreateModel m => ("Expense",m.RequestId,m.Amount,0),
            TreasuryCreateModel m => (m.Kind,m.RequestId,m.Amount,0),
            _ => throw new InvalidOperationException("Lloji i pagesës nuk njihet.")
        };
        var payload=JsonSerializer.Serialize(model);
        var old=await db.PendingBankPayments.SingleOrDefaultAsync(x=>x.RequestId==request);
        if(old!=null){if(old.Payload!=payload||old.CreatedById!=actor||old.Kind!=kind)throw new InvalidOperationException("Formulari është përdorur. Rihapeni.");return old.Id;}
        if(await db.Payments.AnyAsync(x=>x.RequestId==request)||await db.FieldPayments.AnyAsync(x=>x.RequestId==request)||await db.FamilyPayments.AnyAsync(x=>x.RequestId==request)||await db.Expenses.AnyAsync(x=>x.RequestId==request)||await db.TreasuryEntries.AnyAsync(x=>x.RequestId==request))throw new InvalidOperationException("Pagesa është regjistruar tashmë.");
        if(target!=0&&await db.PendingBankPayments.AnyAsync(x=>x.Kind==kind&&x.TargetId==target&&x.ReviewedAt==null))throw new InvalidOperationException("Ky dokument ka pagesë bankare në pritje. Verifikojeni para një kërkese tjetër.");
        int? student=null;string description;
        switch(model){
            case IssuePaymentModel m:
                var sale=await db.Sales.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==m.SaleId&&x.RequestId!=null)??throw new InvalidOperationException("Dalja nuk u gjet.");
                if(sale.Status=="Cancelled"||amount>sale.Due)throw new InvalidOperationException("Kontrolloni borxhin e rekuizitave.");
                student=sale.StudentId;description=sale.InvoiceNumber;break;
            case PersonalPaymentModel m:
                var charge=await db.PersonalCharges.AsNoTracking().Include(x=>x.PersonalTraining).SingleOrDefaultAsync(x=>x.Id==m.ChargeId)??throw new InvalidOperationException("Detyrimi nuk u gjet.");
                if(charge.IsCancelled||amount>charge.Due)throw new InvalidOperationException("Kontrolloni borxhin e trajnimit personal.");
                student=charge.PersonalTraining!.StudentId;description=charge.Number;break;
            case FieldPaymentModel m:
                var booking=await db.FieldBookings.AsNoTracking().Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==m.BookingId)??throw new InvalidOperationException("Termini nuk u gjet.");
                if(booking.SeriesId.HasValue){
                    var seriesIds=await db.FieldBookings.Where(b=>b.SeriesId==booking.SeriesId).Select(b=>b.Id).ToListAsync();
                    var waiting=await db.PendingBankPayments.Where(p=>p.Kind=="Field"&&p.ReviewedAt==null&&p.TargetId.HasValue&&seriesIds.Contains(p.TargetId.Value)).ToListAsync();
                    if(waiting.Any(p=>m.WholeSeries||JsonSerializer.Deserialize<FieldPaymentModel>(p.Payload)!.WholeSeries))
                        throw new InvalidOperationException("Rezervimi ka pagesë bankare në pritje. Verifikojeni para një kërkese tjetër.");
                }
                if(m.WholeSeries){
                    var bookings=await new FieldService(db).ValidateSeriesPaymentAsync(m);
                    var ids=bookings.Select(b=>b.Id).ToList();
                    if(await db.PendingBankPayments.AnyAsync(p=>p.Kind=="Field"&&p.ReviewedAt==null&&p.TargetId.HasValue&&ids.Contains(p.TargetId.Value)))
                        throw new InvalidOperationException("Një termin ka pagesë bankare në pritje. Verifikojeni para pagesës së plotë.");
                }else if(booking.IsCancelled||booking.Revision!=m.Revision||amount>booking.Due)throw new InvalidOperationException("Termini ose borxhi ka ndryshuar. Rihapni formularin.");
                description=m.WholeSeries ? $"{booking.CustomerName} · Pagesë e plotë e rezervimit" : $"{booking.CustomerName} · {booking.Date:dd/MM/yyyy} · {booking.StartsAt:HH:mm}";break;
            case FamilyPaymentModel m:
                var account=await new BillingService(db).GetFamilyAccountAsync(m.FamilyId)??throw new InvalidOperationException("Familja nuk u gjet.");
                if(account.State!=m.ExpectedState)throw new InvalidOperationException("Borxhi i familjes ka ndryshuar. Rihapni formularin.");
                BillingService.AllocateFamilyPayment(account.Members.Select(x=>(x.Student.Id,x.MonthlyDue)),amount);
                description=account.Family.Name;break;
            case ExpenseCreateModel m:
                var expenseCategory=await db.ExpenseCategories.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==m.CategoryId&&x.IsActive&&x.ParentId==null)??throw new InvalidOperationException("Zgjidhni kategori aktive.");
                if(expenseCategory.ContextType=="Match"&&!m.MatchGameId.HasValue)throw new InvalidOperationException("Zgjidhni ndeshjen zyrtare për këtë shpenzim.");
                if(expenseCategory.ContextType!="Match"&&m.MatchGameId.HasValue)throw new InvalidOperationException("Kjo kategori nuk lidhet me ndeshje.");
                if(m.MatchGameId.HasValue&&!await db.MatchGames.AnyAsync(x=>x.Id==m.MatchGameId&&x.Kind!="Friendly"&&x.Status!="Cancelled"))throw new InvalidOperationException("Zgjidhni një ndeshje zyrtare të vlefshme.");
                description=m.Description;break;
            case TreasuryCreateModel m:description=m.Owner??"Gjendja fillestare e bankës";break;
            default:throw new InvalidOperationException("Lloji i pagesës nuk njihet.");
        }
        var pending=new PendingBankPayment{Kind=kind,TargetId=target==0?null:target,StudentId=student,Description=description,RequestId=request,Amount=amount,Payload=payload,CreatedById=actor};
        db.PendingBankPayments.Add(pending);await db.SaveChangesAsync();if(tx!=null)await tx.CommitAsync();return pending.Id;
    }

    private async Task ConfirmAdditional(PendingBankPayment pending,DateOnly date,string actor)
    {
        switch(pending.Kind){
            case "Sale":
                var sale=JsonSerializer.Deserialize<IssuePaymentModel>(pending.Payload)!;sale.Amount=pending.ConfirmedAmount!.Value;
                pending.PaymentId=await new IssueService(db).PayAsync(sale,actor);break;
            case "Personal":
                var personal=JsonSerializer.Deserialize<PersonalPaymentModel>(pending.Payload)!;personal.Amount=pending.ConfirmedAmount!.Value;
                pending.PaymentId=await new PersonalTrainingService(db).PayAsync(personal,actor);break;
            case "Family":
                var family=JsonSerializer.Deserialize<FamilyPaymentModel>(pending.Payload)!;family.PaymentDate=date;family.Amount=pending.ConfirmedAmount??pending.Amount;
                pending.ResultId=await new BillingService(db).RegisterFlexibleFamilyBankPaymentAsync(family,actor);break;
            case "Field":
                var field=JsonSerializer.Deserialize<FieldPaymentModel>(pending.Payload)!;field.Amount=pending.ConfirmedAmount!.Value;
                pending.ResultId=await new FieldService(db).PayAsync(field,actor);
                foreach(var receipt in await db.FieldPayments.Where(x=>x.Id==pending.ResultId||x.BatchId==pending.RequestId).ToListAsync()) receipt.CreatedAt=BillingClock.UtcDate(date);break;
            case "Expense":
                var expense=JsonSerializer.Deserialize<ExpenseCreateModel>(pending.Payload)!;expense.Date=date;expense.Amount=pending.ConfirmedAmount!.Value;
                pending.ResultId=await new ExpenseService(db).CreateAsync(expense,actor);break;
            case "Opening":case "Withdrawal":
                var treasury=JsonSerializer.Deserialize<TreasuryCreateModel>(pending.Payload)!;treasury.Date=date;treasury.Amount=pending.ConfirmedAmount!.Value;
                pending.ResultId=await new TreasuryService(db).CreateAsync(treasury,actor);break;
            default:throw new InvalidOperationException("Lloji i pagesës nuk njihet.");
        }
        if(pending.PaymentId.HasValue){
            var payment=await db.Payments.SingleAsync(x=>x.Id==pending.PaymentId);payment.PaymentDate=BillingClock.UtcDate(date);
            foreach(var entry in await db.StudentTransactions.Where(x=>x.ReferenceId==payment.Id&&(x.ReferenceType=="SalePayment"||x.ReferenceType=="TrainingPayment")).ToListAsync()){
                entry.TransactionDate=payment.PaymentDate;
                if(entry.ReferenceType=="SalePayment"){entry.Year=date.Year;entry.Month=date.Month;}
            }
        }
    }
}
