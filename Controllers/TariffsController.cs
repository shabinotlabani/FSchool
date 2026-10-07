using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Admin)]
public class TariffsController(ApplicationDbContext db,TariffService tariffs):Controller
{
    [HttpGet] public IActionResult Create(bool family = false) => View(new CreateTariffModel { IsFamily = family });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTariffModel model)
    {
        if (ModelState.IsValid) try {
            await tariffs.CreateAsync(model, Actor);
            TempData["Success"] = "Tarifa u shtua.";
            return RedirectToAction(nameof(Index));
        } catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        await tariffs.SetActiveAsync(id, active);
        TempData["Success"] = active ? "Tarifa u aktivizua." : "Tarifa u hoq nga përdorimi. Historiku dhe caktimet ekzistuese ruhen.";
        return RedirectToAction(nameof(Index));
    }
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index()=>View(new FeePlanIndexModel{Plans=await tariffs.OptionsAsync(includeInactive:true),History=await db.FeePlanPrices.AsNoTracking().Include(p=>p.FeePlan).Include(p=>p.CreatedByUser).OrderByDescending(p=>p.EffectiveMonth).ThenByDescending(p=>p.Id).ToListAsync()});
    [HttpGet] public async Task<IActionResult> Price(int id)
    {
        var plan=(await tariffs.OptionsAsync(TariffService.NextMonth)).SingleOrDefault(p=>p.Id==id);
        if(plan==null || plan.IsWaiver)return NotFound();
        return View(new FeePriceModel{FeePlanId=id,IsFamily=plan.IsFamily,PlanName=plan.Name,Amount=plan.Amount,FamilyAdditionalAmount=plan.FamilyAdditionalAmount,FamilyFirstCount=plan.FamilyFirstCount,SingleUsesStandard=plan.SingleUsesStandard});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Price(FeePriceModel model)
    {
        model.IsFamily=await db.FeePlans.Where(p=>p.Id==model.FeePlanId).Select(p=>p.IsFamily).FirstOrDefaultAsync();
        model.PlanName=await db.FeePlans.Where(p=>p.Id==model.FeePlanId).Select(p=>p.Name).FirstOrDefaultAsync();
        if(ModelState.IsValid)try{await tariffs.ChangePriceAsync(model,Actor);TempData["Success"]="Çmimi u planifikua. Muajt e gjeneruar nuk ndryshojnë.";return RedirectToAction(nameof(Index));}
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        return View(model);
    }
    [HttpGet] public async Task<IActionResult> Assign(int id)
    {
        var student=await db.Students.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==id);if(student==null)return NotFound();
        var current=await db.StudentFeeAssignments.AsNoTracking().Where(a=>a.StudentId==id && a.EffectiveMonth<=TariffService.NextMonth).OrderByDescending(a=>a.EffectiveMonth).ThenByDescending(a=>a.Id).FirstOrDefaultAsync();
        return View(new AssignFeeModel{StudentId=id,Student=student,FeePlanId=current?.FeePlanId??1,Plans=await tariffs.OptionsAsync(TariffService.NextMonth)});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Assign(AssignFeeModel model)
    {
        model.Student=await db.Students.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==model.StudentId);if(model.Student==null)return NotFound();
        model.Plans=await tariffs.OptionsAsync();
        if(ModelState.IsValid)try{await tariffs.AssignAsync(model,Actor);TempData["Success"]="Kategoria e re u planifikua dhe u ruajt në historik.";return RedirectToAction("Account","Payments",new{id=model.StudentId});}
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        return View(model);
    }
}
