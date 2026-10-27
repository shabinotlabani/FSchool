using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.SuperAdmin)]
public class SponsorshipsController(ApplicationDbContext db) : Controller
{
    private SponsorshipService Service => new(db);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> SponsorReport(int? id)
    {
        await using var snapshot=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var model=new SponsorshipSponsorReport{Id=id,Contracts=await db.SponsorshipContracts.AsNoTracking().Include(x=>x.Plan).OrderByDescending(x=>x.Plan.Year).ThenBy(x=>x.Sponsor).ThenBy(x=>x.Reference).ToListAsync()};
        if(id.HasValue){
            var contract=await FindContract(id.Value);if(contract==null)return NotFound();
            model.Report=new SponsorshipContractDetails{
                Contract=contract,
                Receipts=await db.SponsorshipReceipts.AsNoTracking().Where(x=>x.ContractId==id&&x.CancelledAt==null).OrderBy(x=>x.Date).ThenBy(x=>x.Id).ToListAsync(),
                Spending=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Beneficiary).Include(x=>x.Allocation.PlanLine).Where(x=>x.Allocation.ContractId==id&&x.CancelledAt==null).OrderBy(x=>x.Date).ThenBy(x=>x.Id).ToListAsync()
            };
        }
        return View(model);
    }
    public async Task<IActionResult> Salaries(int? year)
    {
        var y=year??BillingClock.Today.Year;
        if(y<2020||y>2100)return BadRequest();
        return View(new SponsorshipInvoiceList {
            PlayerContracts=await db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Where(c=>c.Status!="Draft"&&c.StartsOn.Year<=y&&(c.EndedOn??c.EndsOn).Year>=y).ToListAsync(),
            Year=y,Years=await db.SponsorshipPlans.OrderByDescending(x=>x.Year).Select(x=>x.Year).ToListAsync(),
            Invoices=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Beneficiary).Include(x=>x.Allocation.Contract)
                .Where(x=>x.Allocation.Contract.Plan.Year==y&&x.SalaryMonth!=null&&x.CancelledAt==null)
                .OrderBy(x=>x.Beneficiary.Name).ThenBy(x=>x.SalaryMonth).ToListAsync()
        });
    }
    public async Task<IActionResult> Coverage(int? year)
    {
        await using var snapshot=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var y=year??BillingClock.Today.Year;
        var years=await db.SponsorshipPlans.OrderByDescending(x=>x.Year).Select(x=>x.Year).ToListAsync();
        var lines=await db.SponsorshipPlanLines.AsNoTracking().Where(x=>x.Plan.Year==y).OrderBy(x=>x.Id)
            .Select(x=>new SponsorshipCoverageLine {
                Category=x.Category,Declared=x.Budget,
                Covered=db.SponsorshipAllocations.Where(a=>a.PlanLineId==x.Id).Sum(a=>(decimal?)a.Amount)??0
            }).ToListAsync();
        return View(new SponsorshipCoverageReport{Year=y,Years=years,Lines=lines});
    }
    public async Task<IActionResult> Invoices(int? year,string? search)
    {
        var y=year??BillingClock.Today.Year;
        var query=db.SponsorshipSpending.AsNoTracking().Include(x=>x.Beneficiary).Include(x=>x.Allocation.Contract).Include(x=>x.Allocation.PlanLine).Where(x=>x.Allocation.Contract.Plan.Year==y);
        if(!string.IsNullOrWhiteSpace(search)) {
            var term=search.Trim().ToLower();query=query.Where(x=>x.DocumentReference.ToLower().Contains(term)||x.Description.ToLower().Contains(term)||x.Beneficiary.Name.ToLower().Contains(term)||x.Allocation.Contract.Sponsor.ToLower().Contains(term));
        }
        return View(new SponsorshipInvoiceList{Year=y,Search=search,Years=await db.SponsorshipPlans.OrderByDescending(x=>x.Year).Select(x=>x.Year).ToListAsync(),Invoices=await query.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync()});
    }
    private async Task InvoiceChoices(SponsorshipInvoiceInput m)
    {
        m.Categories=await db.SponsorshipPlanLines.AsNoTracking().Include(x=>x.Plan).OrderByDescending(x=>x.Plan.Year).ThenBy(x=>x.Category).ToListAsync();
        m.Beneficiaries=await db.SponsorshipBeneficiaries.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();
        if(m.PlanLineId>0&&m.Amount>0)try {m.Suggestions=await Service.SuggestBudgets(m.PlanLineId,m.Date,m.Amount);}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);}
    }
    [HttpGet] public async Task<IActionResult> Invoice(int? beneficiaryId,string? salaryMonth)
    {
        var m=new SponsorshipInvoiceInput{BeneficiaryId=beneficiaryId??0};
        if(beneficiaryId.HasValue&&TariffService.TryMonth(salaryMonth,out var month)){
            var contracts=await db.PlayerContracts.AsNoTracking().Include(c=>c.Beneficiary).Where(c=>c.BeneficiaryId==beneficiaryId&&c.Status!="Draft"&&c.StartsOn<month.AddMonths(1)&&(c.EndedOn??c.EndsOn)>=month).ToListAsync();
            if(contracts.Count>0){m.SalaryMonth=salaryMonth;m.SalaryRole=contracts[0].SalaryRole;m.Amount=PlayerContractService.MonthPayroll(contracts,month).Net;m.Description="Paga neto "+salaryMonth;m.DocumentReference="PAGA-"+beneficiaryId+"-"+salaryMonth;m.PlanLineId=await db.SponsorshipPlanLines.Where(l=>l.IsSalary&&l.Plan.Year==month.Year).Select(l=>l.Id).FirstOrDefaultAsync();}
        }
        await InvoiceChoices(m);return View(m);
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Invoice(SponsorshipInvoiceInput m,string? intent)
    {
        if(intent=="suggest") {
            ModelState.Clear();await InvoiceChoices(m);
            m.AllocationId=m.Suggestions.FirstOrDefault(x=>x.CanCover)?.AllocationId??0;
            return View(m);
        }
        if(ModelState.IsValid)try {var id=await Service.RegisterInvoice(m,Actor);TempData["Success"]="Fatura u regjistrua.";return RedirectToAction(nameof(InvoiceDetails),new{id});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        await InvoiceChoices(m);return View(m);
    }
    [HttpGet] public async Task<IActionResult> BudgetSuggestions(int planLineId,DateOnly date,decimal amount)
    {
        Response.Headers.CacheControl="no-store";
        if(!ModelState.IsValid)return BadRequest(new{message="Kontrolloni datën dhe shumën."});
        try{return Json(await Service.SuggestBudgets(planLineId,date,amount));}
        catch(ValidationException e){return BadRequest(new{message=e.Message});}
    }
    public async Task<IActionResult> InvoiceDetails(int id)
    {
        var invoice=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Beneficiary).Include(x=>x.Allocation.Contract).Include(x=>x.Allocation.PlanLine).SingleOrDefaultAsync(x=>x.Id==id);
        return invoice==null?NotFound():View(invoice);
    }
    public async Task<IActionResult> Index(int? year)
    {
        await using var snapshot=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var y=year??BillingClock.Today.Year;
        return View(new SponsorshipDashboard {
            Year=y,Years=await db.SponsorshipPlans.OrderByDescending(x=>x.Year).Select(x=>x.Year).ToListAsync(),
            Plan=await db.SponsorshipPlans.AsNoTracking().Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Year==y),
            Contracts=await db.SponsorshipContracts.AsNoTracking().Include(x=>x.Allocations).Where(x=>x.Plan.Year==y).OrderBy(x=>x.Sponsor).ToListAsync(),
            Receipts=await db.SponsorshipReceipts.AsNoTracking().Where(x=>x.Contract.Plan.Year==y).ToListAsync(),
            Spending=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Allocation).Where(x=>x.Allocation.Contract.Plan.Year==y).ToListAsync()
        });
    }
    [HttpGet] public async Task<IActionResult> Plan(int id=0)
    {
        if(id==0)return View(new SponsorshipPlanInput());
        var p=await FindPlan(id);if(p==null)return NotFound();
        return View(new SponsorshipPlanInput{Id=p.Id,Revision=p.Revision,Year=p.Year,Title=p.Title,Reference=p.Reference,Lines=p.Lines.OrderBy(x=>x.Id).Select(x=>new SponsorshipLineInput{Id=x.Id,Category=x.Category,Activities=x.Activities,IsSalary=x.IsSalary,Budget=x.Budget}).ToList(),History=await db.SponsorshipAudits.AsNoTracking().Where(x=>x.Entity=="Plan"&&x.EntityId==p.Id).OrderByDescending(x=>x.At).ToListAsync()});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Plan(SponsorshipPlanInput m)
    {
        if(ModelState.IsValid)try {await Service.CreatePlan(m,Actor);TempData["Success"]="Plani vjetor u regjistrua.";return RedirectToAction(nameof(Index),new{year=m.Year});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        return View(m);
    }
    private Task<SponsorshipPlan?> FindPlan(int id) => db.SponsorshipPlans.AsNoTracking().Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==id);
    [HttpGet] public async Task<IActionResult> Contract(int planId,int id=0)
    {
        if(id!=0) {
            var c=await FindContract(id);if(c==null)return NotFound();var plan=await FindPlan(c.PlanId);
            return View(new SponsorshipContractInput{Id=c.Id,Revision=c.Revision,PlanId=c.PlanId,Plan=plan,Sponsor=c.Sponsor,SponsorNumber=c.SponsorNumber,Reference=c.Reference,CertificateNumber=c.CertificateNumber,CertificateDate=c.CertificateDate,From=c.From,To=c.To,Amount=c.Amount,Allocations=plan!.Lines.OrderBy(x=>x.Id).Select(x=>new SponsorshipAllocationInput{PlanLineId=x.Id,Amount=c.Allocations.SingleOrDefault(a=>a.PlanLineId==x.Id)?.Amount??0}).ToList()});
        }
        var p=await FindPlan(planId);if(p==null)return NotFound();
        return View(new SponsorshipContractInput{PlanId=p.Id,Plan=p,Allocations=p.Lines.OrderBy(x=>x.Id).Select(x=>new SponsorshipAllocationInput{PlanLineId=x.Id}).ToList()});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Contract(SponsorshipContractInput m)
    {
        if(ModelState.IsValid)try {var id=await Service.CreateContract(m,Actor);TempData["Success"]="Kontrata dhe buxhetet u regjistruan.";return RedirectToAction(nameof(Details),new{id});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        m.Plan=await FindPlan(m.PlanId);if(m.Plan==null)return NotFound();return View(m);
    }
    private Task<SponsorshipContract?> FindContract(int id) => db.SponsorshipContracts.AsNoTracking().Include(x=>x.Plan).Include(x=>x.Allocations).ThenInclude(x=>x.PlanLine).SingleOrDefaultAsync(x=>x.Id==id);
    public async Task<IActionResult> Details(int id)
    {
        await using var snapshot=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var c=await FindContract(id);if(c==null)return NotFound();
        var receipts=await db.SponsorshipReceipts.AsNoTracking().Where(x=>x.ContractId==id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        var spending=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Beneficiary).Include(x=>x.Allocation.PlanLine).Where(x=>x.Allocation.ContractId==id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        var rids=receipts.Select(x=>x.Id).ToArray();var sids=spending.Select(x=>x.Id).ToArray();
        return View(new SponsorshipContractDetails{Contract=c,Receipts=receipts,Spending=spending,History=await db.SponsorshipAudits.AsNoTracking().Where(x=>(x.Entity=="Contract"&&x.EntityId==id)||(x.Entity=="Receipt"&&rids.Contains(x.EntityId))||(x.Entity=="Spend"&&sids.Contains(x.EntityId))).OrderByDescending(x=>x.At).ToListAsync()});
    }
    [HttpGet] public async Task<IActionResult> Receive(int id)
    {
        var c=await FindContract(id);return c==null?NotFound():View(new SponsorshipReceiptInput{ContractId=id,Contract=c});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Receive(SponsorshipReceiptInput m)
    {
        if(ModelState.IsValid)try {var id=await Service.Receive(m,Actor);TempData["Success"]="Pranimi në bankë u regjistrua.";return RedirectToAction(nameof(Details),new{id});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        m.Contract=await FindContract(m.ContractId);if(m.Contract==null)return NotFound();return View(m);
    }
    private async Task SpendChoices(SponsorshipSpendInput m) {
        m.Contract=await FindContract(m.ContractId);m.Beneficiaries=await db.SponsorshipBeneficiaries.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();
    }
    [HttpGet] public async Task<IActionResult> Spend(int id,int? beneficiaryId)
    {
        var m=new SponsorshipSpendInput{ContractId=id,BeneficiaryId=beneficiaryId??0};await SpendChoices(m);return m.Contract==null?NotFound():View(m);
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Spend(SponsorshipSpendInput m)
    {
        if(ModelState.IsValid)try {var id=await Service.Spend(m,Actor);TempData["Success"]="Pagesa u regjistrua dhe buxheti u përditësua.";return RedirectToAction(nameof(Details),new{id});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        await SpendChoices(m);if(m.Contract==null)return NotFound();return View(m);
    }
    [HttpGet] public async Task<IActionResult> Beneficiaries(int? contractId,bool invoice=false)
    {
        ViewData["Invoice"]=invoice;
        ViewData["ContractId"]=contractId;ViewData["Beneficiaries"]=await db.SponsorshipBeneficiaries.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();return View(new SponsorshipBeneficiaryInput());
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Beneficiaries(SponsorshipBeneficiaryInput m,int? contractId,bool invoice=false)
    {
        if(ModelState.IsValid)try {
            var id=await Service.CreateBeneficiary(m,Actor);TempData["Success"]="Përfituesi u regjistrua.";
            if(invoice)return RedirectToAction(nameof(Invoice),new{beneficiaryId=id});
            return contractId.HasValue?RedirectToAction(nameof(Spend),new{id=contractId,beneficiaryId=id}):RedirectToAction(nameof(Beneficiaries));
        } catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        ViewData["Invoice"]=invoice;ViewData["ContractId"]=contractId;ViewData["Beneficiaries"]=await db.SponsorshipBeneficiaries.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();return View(m);
    }
    [HttpGet] public async Task<IActionResult> Cancel(int id,string kind)
    {
        if(kind!="Receipt"&&kind!="Spend")return BadRequest();
        if(kind=="Receipt") {
            var r=await db.SponsorshipReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(r==null)return NotFound();
            ViewData["ContractId"]=r.ContractId;ViewData["Amount"]=r.Amount;
        } else {
            var s=await db.SponsorshipSpending.AsNoTracking().Include(x=>x.Allocation).SingleOrDefaultAsync(x=>x.Id==id);if(s==null)return NotFound();
            ViewData["ContractId"]=s.Allocation.ContractId;ViewData["Amount"]=s.Amount;
        }
        return View(new SponsorshipCancelInput{Id=id,Kind=kind});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Cancel(SponsorshipCancelInput m)
    {
        if(ModelState.IsValid)try {var id=await Service.Cancel(m,Actor);TempData["Success"]="Anulimi u ruajt me arsye. Regjistrimi mbetet në historik.";return RedirectToAction(nameof(Details),new{id});}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        return View(m);
    }
}
