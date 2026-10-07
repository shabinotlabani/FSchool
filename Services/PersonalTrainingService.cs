using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class PersonalTrainingService(ApplicationDbContext db)
{
    private async Task<T> Write<T>(Func<Task<T>> action, CancellationToken ct=default)
    {
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(ct):null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)",ct);
        var result=await action();
        if(tx!=null)await tx.CommitAsync(ct);
        return result;
    }
    private void Audit(PersonalTraining? training,PersonalTariff? tariff,string reason,object snapshot,string? actor)
        =>db.PersonalTrainingAudits.Add(new(){PersonalTraining=training,PersonalTariff=tariff,Reason=reason,Snapshot=JsonSerializer.Serialize(snapshot),ActorId=actor});
    private void Entry(PersonalCharge c,string type,decimal debit,decimal credit,string text,string? actor,int? paymentId=null)
        =>db.StudentTransactions.Add(new(){StudentId=c.PersonalTraining!.StudentId,Year=c.Period.Year,Month=c.Period.Month,TransactionType="Training"+type,Description=text.Length>250?text[..250]:text,Debit=debit,Credit=credit,ReferenceType=paymentId.HasValue?"TrainingPayment":"TrainingCharge",ReferenceId=paymentId??c.Id,CreatedByUserId=actor});
    public Task<int> SaveTariffAsync(PersonalTariffModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        var t=m.Id==0?new PersonalTariff():await db.PersonalTariffs.SingleAsync(x=>x.Id==m.Id);
        if(m.Id!=0)await db.Entry(t).ReloadAsync();
        if(m.Id!=0&&(t.Revision!=m.Revision||t.Mode!=m.Mode))throw new InvalidOperationException("Tarifa ka ndryshuar ose lloji nuk mund të ndryshohet. Rihapni formularin.");
        if(await db.PersonalTariffs.AnyAsync(x=>x.Id!=m.Id&&x.Name.ToLower()==m.Name.Trim().ToLower()))throw new InvalidOperationException("Ky emër tarife ekziston.");
        var before=new{t.Name,t.Amount,t.IsActive,t.Notes};
        t.Name=m.Name.Trim();t.Mode=m.Mode;t.Amount=m.Amount;t.IsActive=m.IsActive;t.Notes=m.Notes?.Trim();t.Revision=Guid.NewGuid();
        if(m.Id==0)db.PersonalTariffs.Add(t);
        Audit(null,t,m.Reason?.Trim()??"Krijim tarife",new{Before=before,After=new{t.Name,t.Amount,t.Mode,t.IsActive,t.Notes}},actor);
        await db.SaveChangesAsync();return t.Id;
    });
    public Task<int> CreateAsync(PersonalTrainingCreateModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        foreach(var s in m.Sessions)Validator.ValidateObject(s,new(s),true);
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{m.StudentId,m.PersonalTariffId,m.ExpectedRate,m.StartsOn,m.Coach,m.Location,m.Notes,Sessions=m.Sessions.Select(s=>new{s.Day,s.StartsAt,s.EndsAt})}))));
        var duplicate=await db.PersonalTrainings.SingleOrDefaultAsync(x=>x.RequestId==m.RequestId);
        if(duplicate!=null){if(duplicate.PayloadHash!=hash||duplicate.CreatedByUserId!=actor)throw new InvalidOperationException("Ky formular është përdorur. Rihapeni.");return duplicate.Id;}
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2005)");
        var player=await db.Students.Include(x=>x.TrainingTeam).ThenInclude(x=>x!.Sessions).SingleOrDefaultAsync(x=>x.Id==m.StudentId&&x.IsActive)??throw new InvalidOperationException("Lojtari nuk është aktiv.");
        if(m.StartsOn<BillingClock.LocalDate(player.RegistrationDate))throw new InvalidOperationException("Trajnimi nuk mund të fillojë para regjistrimit të lojtarit.");
        var tariff=await db.PersonalTariffs.SingleOrDefaultAsync(x=>x.Id==m.PersonalTariffId&&x.IsActive)??throw new InvalidOperationException("Tarifa nuk është aktive.");
        await db.Entry(tariff).ReloadAsync();
        if(!tariff.IsActive)throw new InvalidOperationException("Tarifa është çaktivizuar.");
        if(tariff.Amount!=m.ExpectedRate)throw new InvalidOperationException("Çmimi ka ndryshuar. Zgjidhni përsëri tarifën.");
        if(tariff.Mode=="Session"&&m.Sessions.Count!=1)throw new InvalidOperationException("Për seancë vendosni vetëm një orar.");
        var t=new PersonalTraining{RequestId=m.RequestId,PayloadHash=hash,StudentId=m.StudentId,PersonalTariffId=tariff.Id,Name=tariff.Name,Mode=tariff.Mode,Rate=tariff.Amount,StartsOn=m.StartsOn!.Value,Coach=m.Coach?.Trim(),Location=m.Location?.Trim(),Notes=m.Notes?.Trim(),CreatedByUserId=actor};
        t.Sessions=m.Sessions.Select(s=>new PersonalTrainingSlot{Day=t.Mode=="Session"?Day(t.StartsOn):s.Day,StartsAt=s.StartsAt!.Value,EndsAt=s.EndsAt!.Value}).ToList();
        for(var i=0;i<t.Sessions.Count;i++)
        {
            var s=t.Sessions[i];
            if(t.Sessions.Take(i).Any(x=>x.Day==s.Day&&x.StartsAt<s.EndsAt&&s.StartsAt<x.EndsAt))throw new InvalidOperationException("Oraret personale mbivendosen.");
            if(player.TrainingTeam?.Sessions.Any(x=>x.Day==s.Day&&x.StartsAt<s.EndsAt&&s.StartsAt<x.EndsAt)==true)throw new InvalidOperationException("Orari përplaset me stërvitjen bazë të lojtarit.");
        }
        var others=await db.PersonalTrainings.Include(x=>x.Sessions).Where(x=>x.StudentId==m.StudentId&&!x.IsCancelled).ToListAsync();
        foreach(var other in others)
        foreach(var s in t.Sessions)
        foreach(var o in other.Sessions)
        {
            if(s.Day!=o.Day||s.StartsAt>=o.EndsAt||o.StartsAt>=s.EndsAt)continue;
            var start=t.StartsOn>other.StartsOn?t.StartsOn:other.StartsOn;
            var end=t.Mode=="Session"?t.StartsOn:DateOnly.MaxValue;
            var otherEnd=other.Mode=="Session"?other.StartsOn:other.EndsOn??DateOnly.MaxValue;
            if(otherEnd<end)end=otherEnd;
            var days=(s.Day-Day(start)+7)%7;
            if(end>=start&&end.DayNumber-start.DayNumber>=days)throw new InvalidOperationException("Lojtari ka trajnim personal në këtë orar.");
        }
        db.PersonalTrainings.Add(t);Audit(t,null,"Regjistrim trajnimi",new{t.Name,t.Mode,t.Rate,t.StartsOn,t.Coach,t.Location,Orari=t.Schedule},actor);
        await db.SaveChangesAsync();
        if(t.Mode=="Session")await Charge(t,t.StartsOn,actor);
        else await GenerateAsync(actor);
        return t.Id;
    });
    private static int Day(DateOnly d)=>(int)d.DayOfWeek==0?7:(int)d.DayOfWeek;
    private async Task Charge(PersonalTraining t,DateOnly period,string? actor)
    {
        if(await db.PersonalCharges.AnyAsync(x=>x.PersonalTrainingId==t.Id&&x.Period==period))return;
        var prorated=t.Mode=="Monthly"&&period.Year==t.StartsOn.Year&&period.Month==t.StartsOn.Month&&t.StartsOn.Day>1;
        var c=new PersonalCharge{PersonalTraining=t,Period=period,Description=t.Name+" · "+period.ToString(t.Mode=="Monthly"?"MM.yyyy":"dd.MM.yyyy"),Amount=prorated?MonthlyProration.Calculate(t.Rate,t.StartsOn):t.Rate,Calculation=prorated?$"Tarifa {t.Rate:N2} € × {DateTime.DaysInMonth(period.Year,period.Month)-t.StartsOn.Day+1}/{DateTime.DaysInMonth(period.Year,period.Month)} ditë":null};
        db.PersonalCharges.Add(c);await db.SaveChangesAsync();Entry(c,"Fee",c.Amount,0,c.Description,actor);await db.SaveChangesAsync();
    }
    public Task<int> GenerateAsync(string? actor=null,DateOnly? asOf=null,CancellationToken ct=default)=>Write(async()=>{
        var today=asOf??BillingClock.Today;var count=0;
        var trainings=await db.PersonalTrainings.Where(x=>x.Mode=="Monthly"&&!x.IsCancelled&&x.Student!.IsActive&&x.StartsOn<=today).ToListAsync(ct);
        foreach(var t in trainings)
        for(var month=new DateOnly(t.StartsOn.Year,t.StartsOn.Month,1);month<=new DateOnly(today.Year,today.Month,1);month=month.AddMonths(1))
        {
            if(t.EndsOn<t.StartsOn||t.EndsOn<month)break;
            if(await db.PersonalCharges.AnyAsync(x=>x.PersonalTrainingId==t.Id&&x.Period==month,ct)||!await new BillingService(db).WasActiveInMonthAsync(t.StudentId,month,ct))continue;
            await Charge(t,month,actor);count++;
        }
        return count;
    },ct);
    public Task<int> PayAsync(PersonalPaymentModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        var old=await db.Payments.SingleOrDefaultAsync(x=>x.RequestId==m.RequestId);
        if(old!=null){if(old.PersonalChargeId!=m.ChargeId||old.Amount!=m.Amount||old.PaymentMethod!=m.PaymentMethod||old.CreatedByUserId!=actor||old.Notes!=m.Notes?.Trim())throw new InvalidOperationException("Formulari është përdorur.");return old.Id;}
        var c=await db.PersonalCharges.Include(x=>x.PersonalTraining).SingleAsync(x=>x.Id==m.ChargeId);
        await db.Entry(c).ReloadAsync();
        if(m.Amount>c.Due)throw new InvalidOperationException("Shuma tejkalon borxhin e mbetur.");
        var p=new Payment{PersonalChargeId=c.Id,StudentId=c.PersonalTraining!.StudentId,RequestId=m.RequestId,Amount=m.Amount,PaymentMethod=m.PaymentMethod,Notes=m.Notes?.Trim(),CreatedByUserId=actor};
        db.Payments.Add(p);c.Paid+=m.Amount;await db.SaveChangesAsync();
        Entry(c,"Payment",0,p.Amount,"Arkëtim personal #"+p.Id+" · "+c.Description,actor,p.Id);
        await db.SaveChangesAsync();return p.Id;
    });
    // Called by BillingService under the shared billing transaction lock.
    internal async Task<int> CancelPaymentAsync(Payment p,string reason,string actor)
    {
        var c=await db.PersonalCharges.Include(x=>x.PersonalTraining).SingleAsync(x=>x.Id==p.PersonalChargeId);
        await db.Entry(c).ReloadAsync();
        if(c.Paid<p.Amount)throw new InvalidOperationException("Gjendja e pagesës kërkon kontroll.");
        c.Paid-=p.Amount;p.IsCancelled=true;p.CancellationReason=reason.Trim();
        Entry(c,"PaymentReversal",p.Amount,0,"Anulim arkëtimi #"+p.Id+": "+reason.Trim(),actor,p.Id);
        await db.SaveChangesAsync();return p.StudentId;
    }
    public Task<int> ActAsync(string action,PersonalActionModel m,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        if(action=="Waive"||action=="Restore")
        {
            var c=await db.PersonalCharges.Include(x=>x.PersonalTraining).SingleAsync(x=>x.Id==m.Id);
            await db.Entry(c).ReloadAsync();
            if(c.IsCancelled)throw new InvalidOperationException("Detyrimi është anuluar.");
            if(action=="Waive"){
                if(c.Paid!=0||c.Due<=0||string.IsNullOrWhiteSpace(m.Notes))throw new InvalidOperationException("Lirimi lejohet vetëm pa pagesë. Shkruani arsyen dhe shënimin.");
                c.Waived=c.Amount;c.WaiverReason=m.Reason.Trim();c.WaiverNotes=m.Notes.Trim();Entry(c,"Waiver",0,c.Waived,"Lirim: "+m.Reason,actor);
            }else{
                if(c.Waived<=0)throw new InvalidOperationException("Nuk ka lirim aktiv.");
                Entry(c,"WaiverReversal",c.Waived,0,"Kthim lirimi: "+m.Reason,actor);c.Waived=0;c.WaiverReason=null;c.WaiverNotes=null;
            }
            Audit(c.PersonalTraining,null,m.Reason,new{Action=action,ChargeId=c.Id,m.Notes},actor);await db.SaveChangesAsync();return c.PersonalTrainingId;
        }
        var t=await db.PersonalTrainings.Include(x=>x.Charges).SingleAsync(x=>x.Id==m.Id);
        await db.Entry(t).ReloadAsync();
        if(t.Revision!=m.Revision||t.IsCancelled)throw new InvalidOperationException("Regjistrimi ka ndryshuar. Rihapeni.");
        if(action=="Stop"){
            if(t.Mode!="Monthly"||!DateOnly.TryParseExact(m.EndMonth,"yyyy-MM",out var end)||end<TariffService.CurrentMonth||end>TariffService.CurrentMonth.AddYears(10))throw new InvalidOperationException("Zgjidhni muajin aktual ose një muaj të ardhshëm.");
            if(t.EndsOn.HasValue)throw new InvalidOperationException("Paketa është ndërprerë tashmë.");
            t.EndsOn=end.AddMonths(1).AddDays(-1);
        }else if(action=="Cancel"){
            if(t.Charges.Any(x=>x.Paid>0))throw new InvalidOperationException("Anuloni fillimisht pagesat e lidhura, me arsye.");
            foreach(var c in t.Charges.Where(x=>!x.IsCancelled)){Entry(c,"Cancellation",0,c.Amount-c.Waived,"Anulim trajnimi: "+m.Reason,actor);c.IsCancelled=true;}
            t.IsCancelled=true;
        }else throw new InvalidOperationException("Veprim i panjohur.");
        t.Revision=Guid.NewGuid();Audit(t,null,m.Reason,new{Action=action,t.EndsOn,t.IsCancelled,m.Notes},actor);await db.SaveChangesAsync();return t.Id;
    });
}
