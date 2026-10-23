using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Admin+","+Roles.SuperAdmin)]
public class DevelopmentSettingsController(ApplicationDbContext db):Controller
{
    private SportDevelopmentService Service=>new(db);
    public async Task<IActionResult> Index(string? section=null){
        var sport=await Service.Sport();ViewData["Section"]=section;
        return View(new MetricManagement(sport,(await Service.Fields(sport)).Where(x=>section==null||x.Section==section).ToList()));
    }
    public async Task<IActionResult> Edit(string section="Measurements",string key=""){
        var sport=await Service.Sport();
        if(!SportDevelopmentService.Defaults(sport).Any(s=>s.Key==section))return NotFound();
        var row=(await Service.Fields(sport)).SingleOrDefault(x=>x.Section==section&&x.Key==key);
        if(key.Length>0&&row==null)return NotFound();
        return View(row==null?new MetricInput{Sport=sport,Section=section}:new MetricInput{Id=row.Id,Revision=row.Revision,Sport=sport,Section=row.Section,Key=row.Key,Label=row.Label,Type=row.Type,Min=row.Min,Max=row.Max,Step=row.Step,Choices=row.Choices,Enabled=row.Enabled,SortOrder=row.SortOrder});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MetricInput input){
        if(ModelState.IsValid)try{await Service.Save(input);TempData["Success"]="Fusha u ruajt.";return RedirectToAction(nameof(Index),new{section=input.Section});}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Fusha ka ndryshuar. Rihapeni.");}
        return View(input);
    }
}
