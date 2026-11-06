using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ClubEventService(ApplicationDbContext db)
{
    private async Task<T> Write<T>(Func<Task<T>> action)
    {
        await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        var result=await action();
        await db.SaveChangesAsync();
        if(tx!=null)await tx.CommitAsync();
        return result;
    }
    public static DateTime Utc(DateTime local)=>TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local,DateTimeKind.Unspecified),BillingClock.Zone);
    public static DateTime Local(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc,DateTimeKind.Utc),BillingClock.Zone);
    public static string Snapshot(ClubEvent e)=>JsonSerializer.Serialize(new{e.Version,e.Name,e.Description,e.StartsAt,e.EndsAt,e.Location,e.Price,e.Terms,e.Transport,e.Instructions,e.RequiresConsent,e.Deadline,e.CancellationPolicy,e.Organizer,e.Contact});
    private void Audit(ClubEvent e,string actor,string action)=>db.EventAudits.Add(new(){Event=e,Actor=actor,Action=action,Snapshot=Snapshot(e)});
    private void Entry(EventInvitation i,string kind,decimal debit,decimal credit,string? actor=null,int? reference=null)
    {
        db.StudentTransactions.Add(new(){StudentId=i.StudentId,TransactionType="Event"+kind,Description=(i.Event.Name.Length>120?i.Event.Name[..120]:i.Event.Name)+" · "+kind,
            Debit=debit,Credit=credit,ReferenceType="Event",ReferenceId=reference??i.Id,Year=BillingClock.Today.Year,Month=BillingClock.Today.Month,CreatedByUserId=actor});
    }
    private void SetCharge(EventInvitation i,decimal amount,string? actor=null)
    {
        var old=i.Amount-i.Waived;
        i.Amount=amount;i.Waived=0;
        if(amount>old)Entry(i,"Charge",amount-old,0,actor);
        if(amount<old)Entry(i,"Release",0,old-amount,actor);
    }
    private void Release(EventInvitation i,string? actor=null)
    {
        var remaining=i.Amount-i.Waived;if(remaining<=0)return;
        i.Waived=i.Amount;Entry(i,"Release",0,remaining,actor);
    }
    public Task<int> Category(int id,string name,string kind,bool active)=>Write(async()=>{
        name=name.Trim();if(name.Length is 0 or >100||kind is not("Sport" or "Social"))throw new ValidationException("Shkruani kategori të vlefshme.");
        if(await db.EventCategories.AnyAsync(x=>x.Id!=id&&x.Name.ToLower()==name.ToLower()))throw new ValidationException("Kategoria ekziston.");
        var row=id==0?new EventCategory():await db.EventCategories.SingleAsync(x=>x.Id==id);
        row.Name=name;row.Kind=kind;row.IsActive=active;if(id==0)db.Add(row);await db.SaveChangesAsync();return row.Id;
    });
    public Task<int> SeedCategories()=>Write(async()=>{
        if(await db.EventCategories.AnyAsync())return 0;
        string[] sport=["Turne dhe gara","Kampe sportive","Ndeshje miqësore","Seleksionime","Aktivitete sportive"];
        string[] social=["Ekskursione dhe udhëtime","Festa dhe ditëlindje","Kinema, teatër dhe muze","Aktivitete humanitare","Punëtori edukative","Takime me prindër","Piknikë dhe aktivitete familjare","Ceremoni dhe gala","Aktivitete me sponsorë","Lojëra dhe argëtim","Aktivitete ekologjike"];
        db.EventCategories.AddRange(sport.Select(x=>new EventCategory{Name=x,Kind="Sport"}).Concat(social.Select(x=>new EventCategory{Name=x,Kind="Social"})));return 1;
    });
    public Task<int> Save(EventEdit m,IFormFile? photo,string actor)=>Write(async()=>{
        Validator.ValidateObject(m,new(m),true);
        if(!await db.EventCategories.AnyAsync(x=>x.Id==m.CategoryId&&x.IsActive))throw new ValidationException("Zgjidhni kategori aktive.");
        var e=m.Id==0?new ClubEvent{CreatedBy=actor}:await db.ClubEvents.Include(x=>x.Invitations).SingleAsync(x=>x.Id==m.Id);
        if(m.Id!=0&&(e.Revision!=m.Revision||e.Status is "Cancelled" or "Completed"))throw new ValidationException("Eventi ka ndryshuar ose është mbyllur. Rihapeni.");
        var before=Snapshot(e);
        e.Name=m.Name.Trim();e.Description=m.Description.Trim();e.CategoryId=m.CategoryId;e.StartsAt=Utc(m.StartsAt);e.EndsAt=Utc(m.EndsAt);e.Deadline=Utc(m.Deadline);
        e.Location=m.Location.Trim();e.Capacity=m.Capacity;e.Price=m.IsPaid?m.Price:0;e.RequiresConsent=m.RequiresConsent;e.Terms=(m.Terms??"").Trim();e.Transport=(m.Transport??"").Trim();e.Instructions=(m.Instructions??"").Trim();e.Organizer=(m.Organizer??"").Trim();e.Contact=(m.Contact??"").Trim();e.CancellationPolicy=m.CancellationPolicy;
        if(e.StartsAt<=DateTime.UtcNow||e.Deadline<=DateTime.UtcNow)throw new ValidationException("Data dhe afati duhet të jenë në të ardhmen.");
        if(m.RemovePhoto){e.Photo=null;e.PhotoType=null;}
        if(photo!=null){
            if(photo.Length is <30 or >204800)throw new ValidationException("Zgjidhni fotografinë JPG, PNG ose WebP për eventin.");
            using var stream=new MemoryStream();await photo.CopyToAsync(stream);var bytes=stream.ToArray();StudentPhotoService.Validate(bytes);e.Photo=bytes;e.PhotoType="image/webp";
        }
        if(m.Id==0)db.ClubEvents.Add(e);
        await db.SaveChangesAsync();
        if(m.Id!=0&&before!=Snapshot(e)&&e.Status=="Published"){
            e.Version++;
            foreach(var i in e.Invitations){i.Event=e;Release(i,actor);i.Status="Pending";i.WithdrawalRequested=false;i.Attended=null;}
            await Notify(e,e.Invitations.Select(x=>x.StudentId).ToList(),"Kushtet e eventit ndryshuan","Kontrolloni eventin dhe jepni konfirmim të ri.");
        }
        var ids=m.StudentIds.Distinct().ToHashSet();
        if(ids.Count!=await db.Students.CountAsync(x=>ids.Contains(x.Id)&&x.IsActive)||m.TeamIds.Distinct().Count()!=await db.TrainingTeams.CountAsync(x=>m.TeamIds.Contains(x.Id)))throw new ValidationException("Përzgjedhja përmban lojtarë ose ekipe të pavlefshme.");
        var players=await db.Students.Where(x=>x.IsActive&&(m.AllPlayers||ids.Contains(x.Id)||m.TeamIds.Contains(x.TrainingTeamId??0)||((m.MinAge.HasValue||m.MaxAge.HasValue)&&!m.AllPlayers&&m.TeamIds.Count==0&&m.StudentIds.Count==0))).ToListAsync();
        var today=BillingClock.Today;
        players=players.Where(x=>{var age=today.Year-x.DateOfBirth.Year-(today<x.DateOfBirth.AddYears(today.Year-x.DateOfBirth.Year)?1:0);return (!m.MinAge.HasValue||age>=m.MinAge)&&(!m.MaxAge.HasValue||age<=m.MaxAge);}).ToList();
        var added=new List<int>();
        foreach(var player in players.Where(x=>!e.Invitations.Any(i=>i.StudentId==x.Id))){e.Invitations.Add(new(){StudentId=player.Id});added.Add(player.Id);}
        if(e.Invitations.Count==0)throw new ValidationException("Zgjidhni së paku një lojtar për eventin.");
        if(e.Invitations.Count(x=>x.Status=="Accepted")>e.Capacity)throw new ValidationException("Kapaciteti është më i vogël se pjesëmarrjet e konfirmuara.");
        e.Revision=Guid.NewGuid();Audit(e,actor,m.Id==0?"Krijim":"Ndryshim");
        if(e.Status=="Published"&&added.Count>0)await Notify(e,added,"Ftesë për event",e.RequiresConsent?"Kërkohet leja juaj prindërore.":"Konfirmoni pjesëmarrjen në portal.");
        return e.Id;
    });
    public Task<int> State(int id,Guid revision,string action,string actor)=>Write(async()=>{
        var e=await db.ClubEvents.Include(x=>x.Invitations).SingleAsync(x=>x.Id==id);
        if(e.Revision!=revision)throw new ValidationException("Eventi ka ndryshuar. Rihapeni.");
        if(action=="Publish"&&e.Status=="Draft"){
            if(e.Deadline<=DateTime.UtcNow||e.Invitations.Count==0)throw new ValidationException("Kontrolloni afatin dhe lojtarët e ftuar.");
            e.Status="Published";await Notify(e,e.Invitations.Select(x=>x.StudentId).ToList(),"Event i ri",e.RequiresConsent?"Kërkohet autorizimi juaj prindëror.":"Konfirmoni pjesëmarrjen në portal.");
        }else if(action=="Cancel"&&e.Status is "Draft" or "Published"){
            e.Status="Cancelled";e.Version++;foreach(var i in e.Invitations){if(e.CancellationPolicy=="Refund")Release(i,actor);i.Status="Cancelled";i.WithdrawalRequested=false;}
            await Notify(e,e.Invitations.Select(x=>x.StudentId).ToList(),"Eventi u anulua","Kontrolloni gjendjen dhe politikën e anulimit në portal.");
        }else if(action=="Complete"&&e.Status=="Published"&&e.EndsAt<=DateTime.UtcNow)e.Status="Completed";
        else throw new ValidationException("Ky veprim nuk lejohet për gjendjen aktuale të eventit.");
        e.Revision=Guid.NewGuid();Audit(e,actor,action);return id;
    });
    public Task<int> Respond(string token,int invitationId,int version,string decision,string guardian,bool terms)=>Write(async()=>{
        var access=await new ParentAccessService(db).Resolve(token)??throw new ValidationException("Lidhja nuk është aktive.");
        var ids=access.Students.Select(x=>x.Id).ToList();
        var i=await db.EventInvitations.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.Id==invitationId&&ids.Contains(x.StudentId)&&x.Event.Status!="Draft")??throw new ValidationException("Ftesa nuk u gjet.");
        var e=i.Event;
        guardian=guardian.Trim();if(guardian.Length is 0 or >150)throw new ValidationException("Shkruani emrin e prindit / kujdestarit.");
        if(version!=e.Version)throw new ValidationException("Kushtet kanë ndryshuar. Lexojini përsëri.");
        if(decision=="Withdraw"){
            if(i.Status!="Accepted"||i.WithdrawalRequested||e.Status!="Published")throw new ValidationException("Nuk ka pjesëmarrje aktive për tërheqje.");
            i.WithdrawalRequested=true;
        }else{
            if(e.Status!="Published"||DateTime.UtcNow>e.Deadline)throw new ValidationException("Afati për konfirmim ka përfunduar.");
            if(decision is not("Accepted" or "Declined"))throw new ValidationException("Zgjidhni një përgjigje të vlefshme.");
            if(i.Status=="Accepted"&&decision!="Accepted")throw new ValidationException("Kërkoni tërheqjen e pëlqimit për këtë pjesëmarrje.");
            if(decision=="Accepted"&&!terms)throw new ValidationException("Lexoni dhe pranoni kushtet para konfirmimit.");
            if(i.Status==decision&&i.AcceptedVersion==version)return i.ClubEventId;
            if(decision=="Accepted"&&await db.EventInvitations.CountAsync(x=>x.ClubEventId==e.Id&&x.Status=="Accepted")>=e.Capacity)throw new ValidationException("Kapaciteti i eventit është plotësuar.");
            i.Status=decision;i.AcceptedVersion=version;i.WithdrawalRequested=false;
            if(decision=="Accepted")SetCharge(i,e.Price);else Release(i);
        }
        db.EventConsents.Add(new(){Invitation=i,Version=version,Decision=decision,Guardian=guardian,TokenHash=ParentPushService.Hash(token),TermsAccepted=terms,Snapshot=Snapshot(e)});
        await Notify(e,[i.StudentId],decision=="Accepted"?"Pjesëmarrja u konfirmua":decision=="Withdraw"?"Kërkesa për tërheqje u pranua":"Pjesëmarrja u refuzua",decision=="Accepted"&&i.Due>0?$"Borxhi për eventin: {i.Due:N2}.":"Përgjigjja u ruajt në historikun e eventit.");
        return e.Id;
    });
    public Task<int> ResolveWithdrawal(int id,bool approve,string reason,string actor)=>Write(async()=>{
        var i=await db.EventInvitations.Include(x=>x.Event).SingleAsync(x=>x.Id==id);
        if(!i.WithdrawalRequested||string.IsNullOrWhiteSpace(reason)||reason.Length>200)throw new ValidationException("Shkruani arsyen për kërkesën aktive.");
        i.WithdrawalRequested=false;
        if(approve){i.Status="Cancelled";if(i.Event.CancellationPolicy=="Refund")Release(i,actor);}
        Audit(i.Event,actor,$"Tërheqje #{i.Id}: {(approve?"Pranuar":"Refuzuar")} · {reason}");
        await Notify(i.Event,[i.StudentId],"Kërkesa për tërheqje u shqyrtua",approve?"Pjesëmarrja u anulua sipas politikës së eventit.":"Kërkesa u refuzua. Kontaktoni klubin.");return i.ClubEventId;
    });
    public Task<int> Attendance(int id,bool attended,string actor)=>Write(async()=>{
        var i=await db.EventInvitations.Include(x=>x.Event).SingleAsync(x=>x.Id==id);
        if(i.Status!="Accepted"||i.AcceptedVersion!=i.Event.Version||i.Event.Status is not("Published" or "Completed")||i.Event.StartsAt>DateTime.UtcNow)throw new ValidationException("Prezenca regjistrohet në ditën e eventit për lojtarë të konfirmuar.");
        i.Attended=attended;Audit(i.Event,actor,$"Prezenca #{id}: {attended}");return i.ClubEventId;
    });
    public Task<int> CancelParticipation(int id,string reason,string actor)=>Write(async()=>{
        var i=await db.EventInvitations.Include(x=>x.Event).SingleAsync(x=>x.Id==id);
        if(i.Event.Status!="Published"||i.Status=="Cancelled"||string.IsNullOrWhiteSpace(reason)||reason.Length>200)throw new ValidationException("Zgjidhni pjesëmarrje aktive dhe shkruani arsyen e anulimit.");
        if(i.Event.CancellationPolicy=="Refund")Release(i,actor);
        i.Status="Cancelled";i.WithdrawalRequested=false;
        Audit(i.Event,actor,$"Anulim pjesëmarrjeje #{id} · {reason.Trim()}");
        await Notify(i.Event,[i.StudentId],"Pjesëmarrja u anulua","Kontrolloni gjendjen sipas politikës së anulimit në portal.");return i.ClubEventId;
    });
    public Task<int> Pay(int id,decimal amount,string method,Guid request,string actor)=>Write(async()=>{
        var existing=await db.Payments.SingleOrDefaultAsync(x=>x.RequestId==request);
        if(existing!=null){if(existing.EventInvitationId!=id||existing.Amount!=amount||existing.PaymentMethod!=method||existing.CreatedByUserId!=actor)throw new ValidationException("Formulari është përdorur.");return existing.Id;}
        var i=await db.EventInvitations.Include(x=>x.Event).SingleAsync(x=>x.Id==id);
        if(request==Guid.Empty||amount<=0||amount>i.Due||decimal.Round(amount,2)!=amount||method is not("Cash" or "Bank"))throw new ValidationException("Shkruani pagesë të vlefshme deri në borxhin e mbetur.");
        if(method=="Bank"&&!await BankVerificationService.IsConfirmedAmount(db,request,"Event",amount,actor))throw new ValidationException("Pagesa bankare duhet të verifikohet fillimisht.");
        var p=new Payment{StudentId=i.StudentId,EventInvitationId=i.Id,RequestId=request,Amount=amount,PaymentMethod=method,CreatedByUserId=actor};db.Payments.Add(p);i.Paid+=amount;
        await db.SaveChangesAsync();Entry(i,"Payment",0,amount,actor,p.Id);await Notify(i.Event,[i.StudentId],"Pagesa e eventit u regjistrua",$"U pranuan {amount:N2}. Borxhi i mbetur: {i.Due:N2}.");return p.Id;
    });
    internal async Task<int> CancelPayment(Payment p,string reason,string actor)
    {
        var i=await db.EventInvitations.Include(x=>x.Event).SingleAsync(x=>x.Id==p.EventInvitationId);
        if(i.Paid<p.Amount)throw new ValidationException("Pagesa është rimbursuar ose kredituar dhe nuk mund të anulohet.");
        p.IsCancelled=true;p.CancellationReason=reason;i.Paid-=p.Amount;Entry(i,"PaymentReversal",p.Amount,0,actor,p.Id);
        await Notify(i.Event,[i.StudentId],"Pagesa e eventit u anulua",$"Gjendja e përditësuar: {i.Due:N2}.");await db.SaveChangesAsync();return p.StudentId;
    }
    public Task<int> Settle(int id,decimal amount,string kind,string method,Guid request,string reason,string actor,DateOnly? settlementDate=null)=>Write(async()=>{
        var old=await db.EventSettlements.SingleOrDefaultAsync(x=>x.RequestId==request);
        if(old!=null){if(old.EventInvitationId!=id||old.Amount!=amount||old.Kind!=kind||old.Actor!=actor||old.Reason!=reason.Trim())throw new ValidationException("Formulari është përdorur.");return await db.EventInvitations.Where(x=>x.Id==id).Select(x=>x.ClubEventId).SingleAsync();}
        var i=await db.EventInvitations.Include(x=>x.Event).Include(x=>x.Student).SingleAsync(x=>x.Id==id);
        if(request==Guid.Empty||amount<=0||amount>i.Refundable||decimal.Round(amount,2)!=amount||kind is not("Refund" or "Credit")||string.IsNullOrWhiteSpace(reason)||reason.Length>200)throw new ValidationException("Kontrolloni shumën e kthyeshme dhe arsyen.");
        var settlement=new EventSettlement{EventInvitationId=id,RequestId=request,Amount=amount,Kind=kind,Reason=reason.Trim(),Actor=actor};
        if(kind=="Refund"){
            if(method is not("Cash" or "Bank"))throw new ValidationException("Zgjidhni mënyrën e rimbursimit.");
            if(method=="Bank"&&!await BankVerificationService.IsConfirmedAmount(db,request,"EventRefund",amount,actor))throw new ValidationException("Rimbursimi bankar duhet të verifikohet fillimisht.");
            var category=await db.ExpenseCategories.FirstOrDefaultAsync(x=>x.Name=="Rimbursime eventesh"&&x.ParentId==null);
            if(category==null){category=new(){Name="Rimbursime eventesh"};db.Add(category);await db.SaveChangesAsync();}
            settlement.ExpenseId=await new ExpenseService(db).CreateAsync(new(){RequestId=request,CategoryId=category.Id,Amount=amount,Method=method,Date=settlementDate??BillingClock.Today,Description="Rimbursim eventi #"+i.ClubEventId,Recipient=i.Student.FullName,Notes=reason},actor);
        }else{
            // Internal transfer: release event money into the existing monthly balance; no cash receipt is fabricated.
            db.StudentTransactions.Add(new(){StudentId=i.StudentId,TransactionType="Payment",Description="Kreditim nga eventi #"+i.ClubEventId,Credit=amount,ReferenceType="EventCredit",ReferenceId=i.Id,Year=BillingClock.Today.Year,Month=BillingClock.Today.Month,CreatedByUserId=actor});
        }
        i.Paid-=amount;Entry(i,kind,amount,0,actor);db.Add(settlement);Audit(i.Event,actor,$"{kind} #{id}: {amount:N2} · {reason}");
        await Notify(i.Event,[i.StudentId],kind=="Credit"?"Kreditimi u regjistrua":"Rimbursimi u regjistrua",$"Shuma: {amount:N2}. Kontrolloni historikun financiar në portal.");return i.ClubEventId;
    });
    public Task<int> Link(int eventId,int sourceId,bool sponsor,string actor)=>Write(async()=>{
        var e=await db.ClubEvents.SingleAsync(x=>x.Id==eventId);
        if(sponsor){
            if(!await db.SponsorshipReceipts.AnyAsync(x=>x.Id==sourceId&&x.CancelledAt==null)||await db.EventSponsorLinks.AnyAsync(x=>x.SponsorshipReceiptId==sourceId))throw new ValidationException("Arkëtimi i sponsorit nuk është i vlefshëm ose është lidhur tashmë.");
            db.EventSponsorLinks.Add(new(){ClubEventId=eventId,SponsorshipReceiptId=sourceId});
        }else{
            if(!await db.Expenses.AnyAsync(x=>x.Id==sourceId&&x.CancelledAt==null)||await db.EventExpenseLinks.AnyAsync(x=>x.ExpenseId==sourceId)||await db.EventSettlements.AnyAsync(x=>x.ExpenseId==sourceId))throw new ValidationException("Shpenzimi nuk është i vlefshëm ose është lidhur tashmë.");
            db.EventExpenseLinks.Add(new(){ClubEventId=eventId,ExpenseId=sourceId});
        }
        Audit(e,actor,$"Lidhje {(sponsor?"sponsori":"shpenzimi")} #{sourceId}");return eventId;
    });
    public Task<int> Expense(int eventId,ExpenseCreateModel m,string actor)=>Write(async()=>{
        var e=await db.ClubEvents.SingleAsync(x=>x.Id==eventId);
        m.EventId=eventId;
        if(m.Method=="Bank"){await new BankVerificationService(db).QueueExpense(m,actor);return eventId;}
        var id=await new ExpenseService(db).CreateAsync(m,actor);
        var old=await db.EventExpenseLinks.SingleOrDefaultAsync(x=>x.ExpenseId==id);
        if(old!=null){if(old.ClubEventId!=eventId)throw new ValidationException("Shpenzimi i përket një eventi tjetër.");return eventId;}
        db.EventExpenseLinks.Add(new(){ClubEventId=eventId,ExpenseId=id});Audit(e,actor,"Shpenzim #"+id);return eventId;
    });
    public async Task Notify(ClubEvent e,List<int> students,string title,string body,string? key=null)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
        if(!await db.ParentPushSettings.AnyAsync())return;
        var request=key==null?Guid.NewGuid():new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{db.CurrentClubId}:{key}"))[..16]);
        if(await db.ParentPushMessages.AnyAsync(x=>x.RequestId==request))return;
        var subs=await db.ParentPushSubscriptions.Include(x=>x.Student).Where(x=>x.Enabled&&students.Contains(x.StudentId)&&x.Student.IsActive).ToListAsync();
        var rows=new List<ParentPushDelivery>();var push=new ParentPushService(db);
        foreach(var sub in subs){var token=await push.AccessToken(sub);if(token!=null&&sub.TokenHash==ParentPushService.Hash(token))rows.Add(new(){SubscriptionId=sub.Id});}
        if(rows.Count==0)return;
        var text=e.Name+" · "+body;
        var notificationKind=key?.EndsWith(":deadline")==true?"EventDeadline":key?.EndsWith(":start")==true?"EventStart":"Event";
        db.ParentPushMessages.Add(new(){RequestId=request,AutomaticKind=notificationKind,SourceKey=$"E:{e.Id}",ScheduleHash=e.Version.ToString(),Title=title,Body=text.Length>400?text[..397]+"...":text,Audience="Event #"+e.Id,CreatedBy="System",ExpiresAt=DateTime.UtcNow.AddDays(1),Deliveries=rows});
        await db.SaveChangesAsync();
    }
    public Task<int> Reminders(DateTime now)=>Write(async()=>{
        var events=await db.ClubEvents.Include(x=>x.Invitations).Where(x=>x.Status=="Published"&&x.EndsAt>now).ToListAsync();
        foreach(var e in events){
            if(e.Deadline>now&&e.Deadline<=now.AddDays(1))await Notify(e,e.Invitations.Where(x=>x.Status=="Pending").Select(x=>x.StudentId).ToList(),"Afati i konfirmimit po afrohet","Përgjigjuni në portal para afatit.",$"event:{e.Id}:{e.Version}:deadline");
            if(e.StartsAt>now&&e.StartsAt<=now.AddDays(1))await Notify(e,e.Invitations.Where(x=>x.Status=="Accepted").Select(x=>x.StudentId).ToList(),"Eventi fillon së shpejti",$"Fillon më {Local(e.StartsAt):dd/MM/yyyy HH:mm}.",$"event:{e.Id}:{e.Version}:start");
        }
        return events.Count;
    });
}
