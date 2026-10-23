using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Nutritionist)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class NutritionMenusController(ApplicationDbContext db,GeminiMenuService gemini):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private NutritionMenuService Service=>new(db);
    private async Task Prepare(int id)
    {
        await Service.Authorize(id,Actor);ViewData["PlayerName"]="Menuja e përbashkët";
        if(id==0)return;
        var player=(await new PlayerCareService(db).Assigned(id,Actor)).Student;ViewData["PlayerName"]=player.FullName;
        var today=BillingClock.Today;var age=today.Year-player.DateOfBirth.Year;if(player.DateOfBirth.AddYears(age)>today)age--;
        var measurements=await db.DevelopmentRecords.AsNoTracking().Where(x=>x.StudentId==id&&x.Kind=="Measurements"&&x.Date<=today).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        var weight=measurements.FirstOrDefault(x=>x.Values.ContainsKey("Weight"));var height=measurements.FirstOrDefault(x=>x.Values.ContainsKey("Height"));
        ViewData["Measurements"]=$"{age} vjeç · "+(weight==null?"Pesha mungon":$"{weight.Values["Weight"]} kg ({weight.Date:dd.MM.yyyy})")+" · "+(height==null?"Gjatësia mungon":$"{height.Values["Height"]} cm ({height.Date:dd.MM.yyyy})");
    }
    public async Task<IActionResult> Edit(int studentId=0,int? referralId=null)
    {
        try{
            await Prepare(studentId);var menu=await Service.Effective(studentId);
            if(studentId>0){
                var referral=await db.CareReferrals.AsNoTracking().Where(x=>x.StudentId==studentId&&x.Kind==Roles.Nutritionist&&x.ClosedAt==null&&(!referralId.HasValue||x.Id==referralId))
                    .OrderByDescending(x=>x.Id).FirstOrDefaultAsync();
                if(referral?.MenuStatus=="Ready"&&referral.MenuDraft!=null){
                    var draft=System.Text.Json.JsonSerializer.Deserialize<MenuInput>(referral.MenuDraft)!;
                    if(draft.Revision==menu.Revision){ViewData["Draft"]=true;ViewData["ReferralReason"]=referral.Reason;return View(draft);}
                    ViewData["AutoStatus"]="Menuja është ndryshuar pas propozimit. Gjeneroni një propozim të ri.";
                }else if(referral!=null&&referral.MenuStatus!="Published")ViewData["AutoStatus"]=CareMenuStatus.Label(referral.MenuStatus)+(referral.MenuError==null?"":" · "+referral.MenuError);
                if(referral!=null&&referral.MenuStatus!="Published")menu.AiInstructions=referral.Reason;
            }
            return View(menu);
        }catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MenuInput input)
    {
        try{await Prepare(input.StudentId);if(ModelState.IsValid){await Service.Save(input,Actor);TempData["Success"]="Menuja u ruajt.";return RedirectToAction(nameof(Edit),new{input.StudentId});}}
        catch(UnauthorizedAccessException){return NotFound();}catch(ValidationException ex){ModelState.AddModelError("",ex.Message);}
        return View(input);
    }
    [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting("nutrition-ai")]
    public async Task<IActionResult> Generate(MenuInput input,CancellationToken ct)
    {
        try{await Prepare(input.StudentId);if(input.AiInstructions?.Length>2000)throw new ValidationException("Udhëzimi është shumë i gjatë.");
            var result=await gemini.Generate(input,Actor,ct);ModelState.Clear();ViewData["Draft"]=true;return View("Edit",result);}
        catch(UnauthorizedAccessException){return NotFound();}catch(ValidationException ex){ModelState.AddModelError("",ex.Message);}
        return View("Edit",input);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(int studentId,Guid revision)
    {
        try{await Service.Save(new(){StudentId=studentId,Revision=revision},Actor,true);return RedirectToAction(nameof(Edit),new{studentId});}
        catch(UnauthorizedAccessException){return NotFound();}catch(ValidationException ex){TempData["Success"]=ex.Message;return RedirectToAction(nameof(Edit),new{studentId});}
    }
}
