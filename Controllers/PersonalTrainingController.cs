using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Read)]
public class PersonalTrainingController(ApplicationDbContext db):Controller
{
    private PersonalTrainingService Service=>new(db);
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(string? month,string? search,bool unpaidOnly=false,int? studentId=null)
    {
        var date=TariffService.CurrentMonth;
        if(month!=null&&!DateOnly.TryParseExact(month,"yyyy-MM",out date))return BadRequest();
        var list=await db.PersonalTrainings.AsNoTracking().Include(x=>x.Student).Include(x=>x.Sessions).Include(x=>x.Charges).Where(x=>!studentId.HasValue||x.StudentId==studentId).OrderByDescending(x=>x.Id).ToListAsync();
        if(!string.IsNullOrWhiteSpace(search))foreach(var term in search.Split(' ',StringSplitOptions.RemoveEmptyEntries))list=list.Where(x=>x.Student!.FullName.Contains(term,StringComparison.OrdinalIgnoreCase)||x.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||(x.Student.ParentPhone?.Contains(term)??false)).ToList();
        var charges=list.SelectMany(x=>x.Charges).Where(x=>x.Period.Year==date.Year&&x.Period.Month==date.Month&&(!unpaidOnly||x.Due>0)).OrderBy(x=>x.PersonalTraining!.Student!.FullName).ToList();
        ViewBag.StudentId=studentId;
        return View(new PersonalTrainingIndexModel{Month=date.ToString("yyyy-MM"),Search=search,UnpaidOnly=unpaidOnly,Trainings=list,Charges=charges});
    }
    [Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Tariffs()=>View(await db.PersonalTariffs.AsNoTracking().OrderBy(x=>x.Name).ToListAsync());
    [HttpGet,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Tariff(int id=0){
        if(id==0)return View(new PersonalTariffModel());
        var t=await db.PersonalTariffs.FindAsync(id);if(t==null)return NotFound();
        return View(new PersonalTariffModel{History=await db.PersonalTrainingAudits.AsNoTracking().Include(x=>x.Actor).Where(x=>x.PersonalTariffId==id).OrderByDescending(x=>x.Id).ToListAsync(),Id=t.Id,Revision=t.Revision,Name=t.Name,Mode=t.Mode,Amount=t.Amount,IsActive=t.IsActive,Notes=t.Notes});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Tariff(PersonalTariffModel m){
        if(ModelState.IsValid)try{await Service.SaveTariffAsync(m,Actor);TempData["Success"]="Tarifa u ruajt. Trajnimet ekzistuese ruajnë çmimin e tyre.";return RedirectToAction(nameof(Tariffs));}catch(Exception ex)when(ex is ValidationException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        m.History=await db.PersonalTrainingAudits.AsNoTracking().Include(x=>x.Actor).Where(x=>x.PersonalTariffId==m.Id).OrderByDescending(x=>x.Id).ToListAsync();
        return View(m);
    }
    private async Task Choices(PersonalTrainingCreateModel m){
        m.Students=await db.Students.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.LastName).ToListAsync();
        m.Tariffs=await db.PersonalTariffs.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();
    }
    [HttpGet,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Create(int studentId=0){var m=new PersonalTrainingCreateModel{StudentId=studentId};await Choices(m);return View(m);}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Create(PersonalTrainingCreateModel m){
        if(ModelState.IsValid)try{var id=await Service.CreateAsync(m,Actor);TempData["Success"]="Trajnimi u regjistrua. Ekipi bazë mbetet i njëjtë.";return RedirectToAction(nameof(Details),new{id});}catch(Exception ex)when(ex is ValidationException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        await Choices(m);return View(m);
    }
    public async Task<IActionResult> Details(int id){
        var t=await db.PersonalTrainings.AsNoTracking().Include(x=>x.Student).ThenInclude(x=>x!.TrainingTeam).ThenInclude(x=>x!.Sessions).Include(x=>x.Sessions).Include(x=>x.Charges).ThenInclude(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==id);
        if(t==null)return NotFound();
        return View(new PersonalTrainingDetailsModel{Training=t,History=await db.PersonalTrainingAudits.AsNoTracking().Include(x=>x.Actor).Where(x=>x.PersonalTrainingId==id).OrderByDescending(x=>x.Id).ToListAsync()});
    }
    private Task<PersonalCharge?> Charge(int id)=>db.PersonalCharges.AsNoTracking().Include(x=>x.PersonalTraining).ThenInclude(x=>x!.Student).SingleOrDefaultAsync(x=>x.Id==id);
    [HttpGet,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Pay(int id){
        var c=await Charge(id);if(c==null)return NotFound();
        return View(new PersonalPaymentModel{ChargeId=id,Charge=c,Amount=c.Due});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Pay(PersonalPaymentModel m){
        m.Charge=await Charge(m.ChargeId);if(m.Charge==null)return NotFound();
        if(ModelState.IsValid)try{var id=await Service.PayAsync(m,Actor);return RedirectToAction("Receipt","Payments",new{id});}catch(Exception ex)when(ex is ValidationException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        return View(m);
    }
    public async Task<IActionResult> Print(int id){
        var c=await Charge(id);if(c==null)return NotFound();
        return View("~/Views/Payments/Document.cshtml",new PaymentDocumentModel{ServiceDescription=c.Description,Number=c.Number,Date=BillingClock.Today,Student=c.PersonalTraining!.Student!,Description=c.Period.ToString("MM.yyyy"),Amount=c.Amount,Exempted=c.Waived,Paid=c.Paid,Due=c.Due,IsCancelled=c.IsCancelled,PreparedBy=User.Identity?.Name??"Administrata",Notes=string.Join(" · ",new[]{c.Calculation,c.WaiverReason,c.WaiverNotes}.Where(x=>!string.IsNullOrWhiteSpace(x)))});
    }
    [HttpGet,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Action(int id,string operation){
        if(operation is not ("Stop" or "Cancel" or "Waive" or "Restore"))return BadRequest();
        var m=new PersonalActionModel{Id=id,EndMonth=TariffService.CurrentMonth.ToString("yyyy-MM")};
        if(operation is "Waive" or "Restore"){m.Charge=await Charge(id);if(m.Charge==null)return NotFound();}
        else {m.Training=await db.PersonalTrainings.AsNoTracking().Include(x=>x.Student).SingleOrDefaultAsync(x=>x.Id==id);if(m.Training==null)return NotFound();m.Revision=m.Training.Revision;}
        ViewBag.Operation=operation;return View(m);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Action(PersonalActionModel m,string operation){
        if(operation is not ("Stop" or "Cancel" or "Waive" or "Restore"))return BadRequest();
        if(ModelState.IsValid)try{var id=await Service.ActAsync(operation,m,Actor);TempData["Success"]="Veprimi u ruajt me historik.";return RedirectToAction(nameof(Details),new{id});}catch(Exception ex)when(ex is ValidationException or InvalidOperationException){ModelState.AddModelError("",ex.Message);}
        if(operation is "Waive" or "Restore")m.Charge=await Charge(m.Id);
        else m.Training=await db.PersonalTrainings.AsNoTracking().Include(x=>x.Student).SingleOrDefaultAsync(x=>x.Id==m.Id);
        ViewBag.Operation=operation;return View(m);
    }
}
