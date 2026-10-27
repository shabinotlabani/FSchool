using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

// Restricted sponsorship funds have their own ledger. They must never become
// available to general school expenses or be recorded twice in the general cashbox.
public class SponsorshipService(ApplicationDbContext db)
{
    private static string Key(string value) => string.Concat(value.Trim().ToUpperInvariant().Where(c=>!char.IsWhiteSpace(c)));
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static void Validate(object input) => Validator.ValidateObject(input,new(input),true);
    private static void Money(decimal amount) { if(amount < 0 || decimal.Round(amount,2)!=amount) throw new ValidationException("Shuma duhet të ketë deri në dy shifra dhjetore."); }
    private static void Date(DateOnly date, SponsorshipContract c)
    {
        if(date<c.From || date>c.To || date>BillingClock.Today) throw new ValidationException("Data duhet të jetë brenda kontratës dhe jo pas ditës së sotme.");
    }
    private async Task<T> Write<T>(string actor, Func<Task<T>> action)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        // Role edits and sponsorship writes cannot race. All sponsor budgets share
        // a lock because annual limits and salary uniqueness span contracts.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261017, 2010)");
        var allowed = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            join u in db.Users on ur.UserId equals u.Id
            where ur.UserId==actor && r.Name==Roles.SuperAdmin && (u.LockoutEnd==null || u.LockoutEnd<=DateTimeOffset.UtcNow)
            select ur).AnyAsync();
        if(!allowed) throw new InvalidOperationException("Vetëm Super Admin ka qasje te sponsorizimet.");
        var result = await action();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return result;
    }
    private void Audit(string actor,string entity,int id,string reason,object? before,object after) => db.SponsorshipAudits.Add(new(){ActorId=actor,Entity=entity,EntityId=id,Reason=reason,Before=JsonSerializer.Serialize(before),After=JsonSerializer.Serialize(after)});

    public async Task<List<SponsorshipBudgetSuggestion>> SuggestBudgets(int planLineId,DateOnly date,decimal amount)
    {
        if(date.Year<2020 || date>BillingClock.Today || amount<=0 || amount>999999999.99m)
            throw new ValidationException("Plotësoni kategorinë, datën dhe shumën.");
        Money(amount);
        await using var snapshot=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead):null;
        var line=await db.SponsorshipPlanLines.AsNoTracking().Include(x=>x.Plan).SingleOrDefaultAsync(x=>x.Id==planLineId);
        if(line==null)throw new ValidationException("Zgjidhni kategorinë e faturës.");
        var allocations=await db.SponsorshipAllocations.AsNoTracking().Include(x=>x.Contract)
            .Where(x=>x.PlanLineId==planLineId&&x.Contract.From<=date&&x.Contract.To>=date).ToListAsync();
        var ids=allocations.Select(x=>x.ContractId).ToArray();
        var receipts=await db.SponsorshipReceipts.AsNoTracking().Where(x=>ids.Contains(x.ContractId)).ToListAsync();
        var spending=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Allocation).Where(x=>ids.Contains(x.Allocation.ContractId)).ToListAsync();
        var annualAvailable=line.Budget-await db.SponsorshipSpending.Where(x=>x.Allocation.PlanLineId==planLineId&&x.CancelledAt==null).SumAsync(x=>x.Amount);
        var result=new List<SponsorshipBudgetSuggestion>();
        foreach(var a in allocations) {
            var entries=new List<(DateOnly Date,decimal Amount)>();
            foreach(var r in receipts.Where(x=>x.ContractId==a.ContractId)) {
                entries.Add((r.Date,r.Amount));if(r.CancelledAt.HasValue)entries.Add((BillingClock.LocalDate(r.CancelledAt.Value),-r.Amount));
            }
            foreach(var s in spending.Where(x=>x.Allocation.ContractId==a.ContractId)) {
                entries.Add((s.Date,-s.Amount));if(s.CancelledAt.HasValue)entries.Add((BillingClock.LocalDate(s.CancelledAt.Value),s.Amount));
            }
            var balance=entries.Where(x=>x.Date<=date).Sum(x=>x.Amount);var cash=balance;
            foreach(var day in entries.Where(x=>x.Date>date).GroupBy(x=>x.Date).OrderBy(x=>x.Key)) {balance+=day.Sum(x=>x.Amount);cash=Math.Min(cash,balance);}
            var category=a.Amount-spending.Where(x=>x.AllocationId==a.Id&&x.CancelledAt==null).Sum(x=>x.Amount);
            var available=Math.Max(0,Math.Min(annualAvailable,Math.Min(category,cash)));
            result.Add(new(a.Id,a.ContractId,a.Contract.Sponsor,a.Contract.Reference,line.Category,a.Contract.To,available,Math.Min(amount,available),available>=amount));
        }
        return result.OrderByDescending(x=>x.CanCover).ThenBy(x=>x.Expires).ThenByDescending(x=>x.Available).ThenBy(x=>x.AllocationId).ToList();
    }

    public async Task<int> RegisterInvoice(SponsorshipInvoiceInput m,string actor)
    {
        Validate(m);
        var allocation=await db.SponsorshipAllocations.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==m.AllocationId&&x.PlanLineId==m.PlanLineId)
            ??throw new ValidationException("Zgjidhni buxhetin e kategorisë së faturës.");
        // Spend revalidates the selected budget under the global financial lock.
        await Spend(new(){RequestId=m.RequestId,ContractId=allocation.ContractId,AllocationId=allocation.Id,BeneficiaryId=m.BeneficiaryId,
            Date=m.Date,InvoiceDate=m.InvoiceDate,SalaryMonth=m.SalaryMonth,SalaryRole=m.SalaryRole,Amount=m.Amount,Description=m.Description,DocumentReference=m.DocumentReference,BankReference=m.BankReference},actor);
        return await db.SponsorshipSpending.Where(x=>x.RequestId==m.RequestId).Select(x=>x.Id).SingleAsync();
    }

    public Task<int> CreatePlan(SponsorshipPlanInput m,string actor) => Write(actor,async()=>{
        Validate(m);
        foreach(var line in m.Lines) { Validate(line); Money(line.Budget); }
        if(m.Lines.Select(x=>Key(x.Category)).Distinct().Count()!=m.Lines.Count) throw new ValidationException("Kategoritë nuk duhet të përsëriten.");
        if(await db.SponsorshipPlans.AnyAsync(x=>x.Year==m.Year&&x.Id!=m.Id)) throw new InvalidOperationException("Plani për këtë vit ekziston.");
        var plan=m.Id==0?new SponsorshipPlan{Year=m.Year}:await db.SponsorshipPlans.Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==m.Id)??throw new ValidationException("Plani nuk u gjet.");
        object? before=null;
        if(m.Id!=0) {
            if(plan.Revision!=m.Revision)throw new ValidationException("Plani ka ndryshuar. Rihapni formularin.");
            if(string.IsNullOrWhiteSpace(m.Reason))throw new ValidationException("Shkruani arsyen dhe referencën e ndryshimit të deklarimit.");
            if(plan.Year!=m.Year)throw new ValidationException("Viti i planit nuk ndryshohet.");
            before=new{plan.Title,plan.Reference,Lines=plan.Lines.Select(x=>new{x.Id,x.Category,x.Activities,x.IsSalary,x.Budget}).ToArray()};
        }
        if(m.Lines.Where(x=>x.Id!=0).Select(x=>x.Id).Distinct().Count()!=m.Lines.Count(x=>x.Id!=0)||m.Lines.Any(x=>x.Id!=0&&!plan.Lines.Any(l=>l.Id==x.Id)))throw new ValidationException("Kategoritë nuk i përkasin këtij plani.");
        foreach(var line in plan.Lines.ToList()) {
            var input=m.Lines.SingleOrDefault(x=>x.Id==line.Id);
            var allocated=await db.SponsorshipAllocations.Where(x=>x.PlanLineId==line.Id).SumAsync(x=>x.Amount);
            if(input==null){if(allocated>0)throw new ValidationException("Kategoria është përdorur në kontrata dhe nuk mund të hiqet.");db.SponsorshipPlanLines.Remove(line);continue;}
            if(input.Budget<allocated)throw new ValidationException("Buxheti i kategorisë nuk mund të jetë më i ulët se kontratat ekzistuese.");
            if(allocated>0&&(input.IsSalary!=line.IsSalary||input.Category.Trim()!=line.Category))throw new ValidationException("Emri dhe lloji i kategorisë së përdorur në kontrata nuk ndryshojnë. Shtoni kategori të re për qëllim tjetër.");
            line.Category=input.Category.Trim();line.Activities=input.Activities?.Trim()??"";line.IsSalary=input.IsSalary;line.Budget=input.Budget;
        }
        foreach(var line in m.Lines.Where(x=>x.Id==0))plan.Lines.Add(new(){Category=line.Category.Trim(),Activities=line.Activities?.Trim()??"",IsSalary=line.IsSalary,Budget=line.Budget});
        plan.Title=m.Title.Trim();plan.Reference=m.Reference.Trim();plan.Revision=Guid.NewGuid();
        if(m.Id==0)db.SponsorshipPlans.Add(plan);await db.SaveChangesAsync();
        Audit(actor,"Plan",plan.Id,m.Id==0?"Regjistrim i planit vjetor":m.Reason!.Trim(),before,new{m.Year,m.Title,m.Reference,m.Lines});return plan.Id;
    });
    public Task<int> CreateContract(SponsorshipContractInput m,string actor) => Write(actor,async()=>{
        Validate(m);Money(m.Amount);
        var plan=await db.SponsorshipPlans.Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==m.PlanId) ?? throw new ValidationException("Zgjidhni planin vjetor.");
        if(!m.From.HasValue||!m.To.HasValue||m.From.Value.Year<2020||m.To.Value.Year>2200||m.From.Value>m.To.Value) throw new ValidationException("Zgjidhni saktë datën e fillimit dhe datën e përfundimit të kontratës.");
        var from=m.From.Value;var to=m.To.Value;
        foreach(var a in m.Allocations) { Validate(a); Money(a.Amount); }
        if(m.Allocations.Select(x=>x.PlanLineId).Distinct().Count()!=m.Allocations.Count || m.Allocations.Any(x=>!plan.Lines.Any(l=>l.Id==x.PlanLineId))) throw new ValidationException("Zgjidhni kategori të ndryshme nga ky plan.");
        var allocations=m.Allocations.Where(x=>x.Amount>0).ToList();
        if(allocations.Sum(x=>x.Amount)!=m.Amount) throw new ValidationException("Shuma e kategorive duhet të jetë e barabartë me shumën e kontratës.");
        foreach(var a in allocations) {
            var reserved=await db.SponsorshipAllocations.Where(x=>x.PlanLineId==a.PlanLineId&&x.ContractId!=m.Id).SumAsync(x=>x.Amount);
            if(reserved+a.Amount>plan.Lines.Single(x=>x.Id==a.PlanLineId).Budget) throw new ValidationException("Kontratat së bashku tejkalojnë planin vjetor për kategorinë: "+plan.Lines.Single(x=>x.Id==a.PlanLineId).Category);
        }
        var sponsor=Key(m.SponsorNumber); var reference=Key(m.Reference);
        if(await db.SponsorshipContracts.AnyAsync(x=>x.Id!=m.Id&&x.SponsorNumber==sponsor&&x.Reference==reference)) throw new ValidationException("Kjo kontratë e sponsorit është regjistruar.");
        var c=m.Id==0?new SponsorshipContract{PlanId=plan.Id}:await db.SponsorshipContracts.Include(x=>x.Allocations).SingleOrDefaultAsync(x=>x.Id==m.Id)??throw new ValidationException("Kontrata nuk u gjet.");
        object? before=null;
        if(m.Id!=0) {
            if(c.Revision!=m.Revision)throw new ValidationException("Kontrata ka ndryshuar. Rihapni formularin.");
            if(string.IsNullOrWhiteSpace(m.Reason))throw new ValidationException("Shkruani arsyen dhe referencën e ndryshimit të kontratës.");
            if(c.PlanId!=plan.Id)throw new ValidationException("Plani i kontratës nuk ndryshohet.");
            var receipts=await db.SponsorshipReceipts.AsNoTracking().Where(x=>x.ContractId==c.Id).ToListAsync();
            var spending=await db.SponsorshipSpending.AsNoTracking().Where(x=>x.Allocation.ContractId==c.Id).ToListAsync();
            if((receipts.Count>0||spending.Count>0)&&c.SponsorNumber!=sponsor)throw new ValidationException("Sponsori i një kontrate me veprime financiare nuk ndryshohet.");
            if(receipts.Where(x=>x.CancelledAt==null).Sum(x=>x.Amount)>m.Amount)throw new ValidationException("Kontrata nuk mund të ulet nën shumën e pranuar.");
            if(receipts.Any(x=>x.Date<from||x.Date>to)||spending.Any(x=>x.Date<from||x.Date>to||x.SalaryMonth.HasValue&&(x.SalaryMonth.Value>to||x.SalaryMonth.Value.AddMonths(1).AddDays(-1)<from)))throw new ValidationException("Periudha e re përjashton veprime të regjistruara.");
            before=new{c.Sponsor,c.SponsorNumber,c.Reference,c.CertificateNumber,c.CertificateDate,c.From,c.To,c.Amount,Allocations=c.Allocations.Select(x=>new{x.PlanLineId,x.Amount}).ToArray()};
            foreach(var a in c.Allocations.ToList()) {
                var input=allocations.SingleOrDefault(x=>x.PlanLineId==a.PlanLineId);
                if(input==null){if(spending.Any(x=>x.AllocationId==a.Id))throw new ValidationException("Kategoria me pagesa të regjistruara nuk mund të hiqet.");db.SponsorshipAllocations.Remove(a);continue;}
                if(spending.Where(x=>x.AllocationId==a.Id&&x.CancelledAt==null).Sum(x=>x.Amount)>input.Amount)throw new ValidationException("Buxheti i kategorisë nuk mund të ulet nën shpenzimet e kryera.");
                a.Amount=input.Amount;
            }
        }
        foreach(var a in allocations.Where(x=>!c.Allocations.Any(v=>v.PlanLineId==x.PlanLineId)))c.Allocations.Add(new(){PlanLineId=a.PlanLineId,Amount=a.Amount});
        c.CertificateNumber=string.IsNullOrWhiteSpace(m.CertificateNumber)?null:m.CertificateNumber.Trim();c.CertificateDate=m.CertificateDate;c.Sponsor=m.Sponsor.Trim();c.SponsorNumber=sponsor;c.Reference=reference;c.From=from;c.To=to;c.Amount=m.Amount;c.Revision=Guid.NewGuid();
        if(m.Id==0)db.SponsorshipContracts.Add(c);await db.SaveChangesAsync();Audit(actor,"Contract",c.Id,m.Id==0?"Regjistrim kontrate":m.Reason!.Trim(),before,new{m.PlanId,m.Sponsor,m.SponsorNumber,m.Reference,m.CertificateNumber,m.CertificateDate,m.From,m.To,m.Amount,m.Allocations});return c.Id;
    });
    public Task<int> CreateBeneficiary(SponsorshipBeneficiaryInput m,string actor) => Write(actor,async()=>{
        Validate(m);var identifier=Key(m.Identifier);
        if(await db.SponsorshipBeneficiaries.AnyAsync(x=>x.Identifier==identifier)) throw new ValidationException("Ky përfitues ekziston. Përdorni regjistrimin ekzistues.");
        // Accidental duplicate people with a newly invented code also require review.
        var names=await db.SponsorshipBeneficiaries.Where(x=>x.IsPerson==m.IsPerson).Select(x=>x.Name).ToListAsync();
        if(names.Any(x=>Key(x)==Key(m.Name))) throw new ValidationException("Ky emër ekziston. Për persona me të njëjtin emër, shtoni një dallues në emërtim dhe përdorni kodin unik të personit.");
        var b=new SponsorshipBeneficiary{Name=m.Name.Trim(),Identifier=identifier,IsPerson=m.IsPerson};db.SponsorshipBeneficiaries.Add(b);await db.SaveChangesAsync();Audit(actor,"Beneficiary",b.Id,"Regjistrim përfituesi",null,m);return b.Id;
    });
    public Task<int> Receive(SponsorshipReceiptInput m,string actor) => Write(actor,async()=>{
        Validate(m);Money(m.Amount);if(m.RequestId==Guid.Empty)throw new ValidationException("Rihapni formularin.");
        var hash=Hash(new{m.ContractId,m.Date,m.Amount,BankReference=Key(m.BankReference)});
        var existing=await db.SponsorshipReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==m.RequestId);
        if(existing!=null) { if(existing.PayloadHash!=hash)throw new ValidationException("Ky formular është përdorur. Rihapeni.");return existing.ContractId; }
        var c=await db.SponsorshipContracts.SingleOrDefaultAsync(x=>x.Id==m.ContractId)??throw new ValidationException("Kontrata nuk u gjet.");Date(m.Date,c);
        if(await db.SponsorshipReceipts.Where(x=>x.ContractId==c.Id&&x.CancelledAt==null).SumAsync(x=>x.Amount)+m.Amount>c.Amount) throw new ValidationException("Pranimi tejkalon shumën e kontratës.");
        var bank=Key(m.BankReference);
        if(await db.SponsorshipReceipts.AnyAsync(x=>x.BankReference==bank&&x.CancelledAt==null)) throw new ValidationException("Kjo referencë bankare është regjistruar.");
        var r=new SponsorshipReceipt{RequestId=m.RequestId,PayloadHash=hash,ContractId=c.Id,Date=m.Date,Amount=m.Amount,BankReference=bank};
        db.SponsorshipReceipts.Add(r);await db.SaveChangesAsync();Audit(actor,"Receipt",r.Id,"Pranim në bankë",null,new{r.ContractId,r.Date,r.Amount,r.BankReference});return c.Id;
    });
    public Task<int> Spend(SponsorshipSpendInput m,string actor) => Write(actor,async()=>{
        Validate(m);Money(m.Amount);if(m.RequestId==Guid.Empty)throw new ValidationException("Rihapni formularin.");
        var hash=m.InvoiceDate.HasValue
            ?Hash(new{m.ContractId,m.AllocationId,m.BeneficiaryId,m.Date,m.InvoiceDate,m.SalaryMonth,m.SalaryRole,m.Amount,Description=m.Description.Trim(),DocumentReference=Key(m.DocumentReference),BankReference=Key(m.BankReference)})
            :Hash(new{m.ContractId,m.AllocationId,m.BeneficiaryId,m.Date,m.SalaryMonth,m.SalaryRole,m.Amount,Description=m.Description.Trim(),DocumentReference=Key(m.DocumentReference),BankReference=Key(m.BankReference)});
        var existing=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Allocation).SingleOrDefaultAsync(x=>x.RequestId==m.RequestId);
        if(existing!=null) { if(existing.PayloadHash!=hash)throw new ValidationException("Ky formular është përdorur. Rihapeni.");return existing.Allocation.ContractId; }
        var a=await db.SponsorshipAllocations.Include(x=>x.Contract).Include(x=>x.PlanLine).SingleOrDefaultAsync(x=>x.Id==m.AllocationId&&x.ContractId==m.ContractId)??throw new ValidationException("Kategoria nuk lejohet nga kjo kontratë.");
        Date(m.Date,a.Contract);
        var beneficiary=await db.SponsorshipBeneficiaries.FindAsync(m.BeneficiaryId)??throw new ValidationException("Zgjidhni përfituesin.");
        DateOnly? month=null;
        if(a.PlanLine.IsSalary) {
            if(m.SalaryRole is not ("Player" or "Staff"))throw new ValidationException("Zgjidhni Lojtar ose Staf për pagën.");
            if(!beneficiary.IsPerson || !DateOnly.TryParseExact(m.SalaryMonth+"-01","yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed)) throw new ValidationException("Për pagë zgjidhni personin dhe muajin e pagës.");
            month=parsed;
            if(beneficiary.StaffCategory!=null){
                var expectedRole=beneficiary.StaffCategory=="ContractPlayer"?"Player":"Staff";
                if(m.SalaryRole!=expectedRole)throw new ValidationException(expectedRole=="Player"?"Përfituesi është lojtar me kontratë.":"Përfituesi është anëtar i stafit.");
                if(!await db.PlayerContracts.AnyAsync(c=>c.BeneficiaryId==beneficiary.Id&&c.StudentId==null&&c.Status!="Draft"&&c.StartsOn<parsed.AddMonths(1)&&(c.EndedOn??c.EndsOn)>=parsed))throw new ValidationException("Personi nuk ka kontratë aktive për këtë muaj page.");
            }
            if(beneficiary.StudentId.HasValue){
                if(m.SalaryRole!="Player")throw new ValidationException("Përfituesi është lojtar me kontratë.");
                if(!await new PlayerContractService(db).Covering(beneficiary.StudentId.Value,parsed).AnyAsync())throw new ValidationException("Lojtari nuk ka kontratë aktive për këtë muaj page.");
            }
            if(parsed>a.Contract.To || parsed.AddMonths(1).AddDays(-1)<a.Contract.From || parsed>m.Date) throw new ValidationException("Muaji i pagës duhet të jetë brenda periudhës së kontratës dhe jo pas pagesës.");
            var duplicate=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Allocation.Contract).FirstOrDefaultAsync(x=>x.BeneficiaryId==m.BeneficiaryId&&x.SalaryMonth==month&&x.CancelledAt==null);
            if(duplicate!=null) throw new ValidationException($"Paga e {parsed:MM.yyyy} për {beneficiary.Name} është regjistruar nga {duplicate.Allocation.Contract.Sponsor}. Nuk lejohet pagesë e dytë.");
        } else if(!string.IsNullOrWhiteSpace(m.SalaryMonth)) throw new ValidationException("Për pagat përdorni një kategori të shënuar si pagë në planin vjetor.");
        var document=Key(m.DocumentReference);var bank=Key(m.BankReference);
        if(await db.SponsorshipSpending.AnyAsync(x=>x.CancelledAt==null&&x.BeneficiaryId==m.BeneficiaryId&&x.DocumentReference==document))
            throw new ValidationException("Një faturë me këtë numër është regjistruar tashmë për këtë furnitor.");
        if(await db.SponsorshipSpending.AnyAsync(x=>x.CancelledAt==null&&x.BankReference==bank))
            throw new ValidationException("Kjo pagesë bankare është regjistruar më parë.");
        if(await db.SponsorshipSpending.Where(x=>x.AllocationId==a.Id&&x.CancelledAt==null).SumAsync(x=>x.Amount)+m.Amount>a.Amount) throw new ValidationException("Shpenzimi tejkalon buxhetin e kësaj kategorie në kontratë.");
        if(await db.SponsorshipSpending.Where(x=>x.Allocation.PlanLineId==a.PlanLineId&&x.CancelledAt==null).SumAsync(x=>x.Amount)+m.Amount>a.PlanLine.Budget) throw new ValidationException("Shpenzimi tejkalon planin vjetor të kategorisë.");
        var r=new SponsorshipSpend{RequestId=m.RequestId,PayloadHash=hash,AllocationId=a.Id,BeneficiaryId=m.BeneficiaryId,Date=m.Date,InvoiceDate=m.InvoiceDate,SalaryMonth=month,SalaryRole=month.HasValue?m.SalaryRole:null,Amount=m.Amount,Description=m.Description.Trim(),DocumentReference=document,BankReference=bank};
        db.SponsorshipSpending.Add(r);await db.SaveChangesAsync();
        await CheckFunds(a.ContractId);
        Audit(actor,"Spend",r.Id,"Pagesë bankare",null,new{m.ContractId,r.AllocationId,r.BeneficiaryId,r.Date,r.InvoiceDate,r.SalaryMonth,r.SalaryRole,r.Amount,r.Description,r.DocumentReference,r.BankReference});return a.ContractId;
    });
    // Check every daily closing balance, including backdated entries and reversals.
    private async Task CheckFunds(int contractId)
    {
        var entries=new List<(DateOnly Date,decimal Amount)>();
        foreach(var r in await db.SponsorshipReceipts.AsNoTracking().Where(x=>x.ContractId==contractId).ToListAsync()) {
            entries.Add((r.Date,r.Amount));if(r.CancelledAt.HasValue)entries.Add((BillingClock.LocalDate(r.CancelledAt.Value),-r.Amount));
        }
        foreach(var s in await db.SponsorshipSpending.AsNoTracking().Where(x=>x.Allocation.ContractId==contractId).ToListAsync()) {
            entries.Add((s.Date,-s.Amount));if(s.CancelledAt.HasValue)entries.Add((BillingClock.LocalDate(s.CancelledAt.Value),s.Amount));
        }
        decimal balance=0;
        foreach(var day in entries.GroupBy(x=>x.Date).OrderBy(x=>x.Key)) {
            balance+=day.Sum(x=>x.Amount);
            if(balance<0)throw new ValidationException($"Mjetet e pranuara nga ky sponsor nuk mjaftojnë më {day.Key:dd.MM.yyyy}.");
        }
    }
    public Task<int> Cancel(SponsorshipCancelInput m,string actor) => Write(actor,async()=>{
        Validate(m);int contractId;
        if(m.Kind=="Spend") {
            var s=await db.SponsorshipSpending.Include(x=>x.Allocation).SingleOrDefaultAsync(x=>x.Id==m.Id)??throw new ValidationException("Pagesa nuk u gjet.");
            await db.Entry(s).ReloadAsync();contractId=s.Allocation.ContractId;
            if(s.CancelledAt!=null)return contractId;
            s.CancelledAt=DateTime.UtcNow;s.CancellationReason=m.Reason.Trim();
        } else {
            var r=await db.SponsorshipReceipts.SingleOrDefaultAsync(x=>x.Id==m.Id)??throw new ValidationException("Pranimi nuk u gjet.");
            await db.Entry(r).ReloadAsync();contractId=r.ContractId;
            if(r.CancelledAt!=null)return contractId;
            r.CancelledAt=DateTime.UtcNow;r.CancellationReason=m.Reason.Trim();
        }
        await db.SaveChangesAsync();await CheckFunds(contractId);
        Audit(actor,m.Kind,m.Id,m.Reason.Trim(),new{Cancelled=false},new{Cancelled=true,ContractId=contractId});return contractId;
    });
}
