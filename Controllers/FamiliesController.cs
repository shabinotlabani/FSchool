using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Read)]
public class FamiliesController(ApplicationDbContext db):Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query=db.PlayerFamilies.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search))query=query.Where(f=>f.Name.ToLower().Contains(search.Trim().ToLower()) || f.Phone!=null && f.Phone.Contains(search.Trim()));
        ViewData["Search"]=search;
        return View(await query.OrderBy(f=>f.Name).ToListAsync());
    }
    [ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Prices(string month,int planId=2)
    {
        if(!TariffService.TryMonth(month,out var date)||date.Year<2000||date.Year>9998)return BadRequest();
        var options=await new TariffService(db).OptionsAsync(date,includeInactive:true);var family=options.SingleOrDefault(p=>p.Id==planId && p.IsFamily);if(family==null)return NotFound();
        return Json(new{first=family.Amount,additional=family.FamilyAdditionalAmount,limit=family.FamilyFirstCount,singleStandard=family.SingleUsesStandard,standard=options.Single(p=>p.Id==1).Amount});
    }
    public async Task<IActionResult> Details(int id,string? month)
    {
        var date=TariffService.CurrentMonth;
        if(month!=null && (!TariffService.TryMonth(month,out date)||date.Year<2000||date.Year>9998))return BadRequest("Muaji nuk është i vlefshëm.");
        var model=await new FamilyService(db).DetailsAsync(id,date);
        return model==null?NotFound():View(model);
    }
    [Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Edit(int? id,string? month)
    {
        var date=TariffService.NextMonth;
        if(month!=null && (!TariffService.TryMonth(month,out date)||date<TariffService.NextMonth||date.Year>9998))return BadRequest("Zgjidhni një muaj të ardhshëm.");
        var model=new FamilyEditModel{EffectiveMonth=date.ToString("yyyy-MM")};
        if(id.HasValue)
        {
            var family=await db.PlayerFamilies.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==id);if(family==null)return NotFound();
            model.FeePlanId=await new FamilyService(db).PlanIdAsync(family.Id,date);model.Id=family.Id;model.Name=family.Name;model.Phone=family.Phone;model.Revision=family.Revision;
            model.StudentIds=(await new FamilyService(db).MembersAsync(family.Id,date)).Select(a=>a.StudentId).ToList();
        }
        await Choices(model);return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Save(FamilyEditModel model)
    {
        if(ModelState.IsValid)try
        {
            var id=await new FamilyService(db).SaveAsync(model,User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            TempData["Success"]="Familja dhe paketa u ruajtën. Faturat e lëshuara nuk ndryshojnë.";
            return RedirectToAction(nameof(Details),new{id,month=model.EffectiveMonth});
        }
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        await Choices(model);return View("Edit",model);
    }
    private async Task Choices(FamilyEditModel model)
    {
        model.Students=await db.Students.AsNoTracking().Where(s=>s.IsActive||model.StudentIds.Contains(s.Id)).OrderBy(s=>s.LastName).ThenBy(s=>s.FirstName).ToListAsync();
        var month=TariffService.TryMonth(model.EffectiveMonth,out var date)?date:TariffService.NextMonth;
        var options=await new TariffService(db).OptionsAsync(month,includeInactive:true);
        var existing=model.Id==0?0:await new FamilyService(db).PlanIdAsync(model.Id,month);
        model.Plans=options.Where(p=>p.IsFamily && (p.IsActive || p.Id==existing)).ToList();
        if(!model.Plans.Any(p=>p.Id==model.FeePlanId))model.FeePlanId=model.Plans.FirstOrDefault()?.Id??0;
        model.FamilyPrice=model.Plans.SingleOrDefault(p=>p.Id==model.FeePlanId);model.StandardPrice=options.Single(p=>p.Id==1).Amount;
    }
}
