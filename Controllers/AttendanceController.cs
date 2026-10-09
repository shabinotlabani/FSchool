using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Admin+","+Roles.Coach)]
public class AttendanceController(ApplicationDbContext db):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<IActionResult> Index(int teamId,string coachRole="head",DateOnly? date=null)
    {
        if(!ModelState.IsValid)return BadRequest();
        try{return View(await new AttendanceService(db).LoadAsync(teamId,coachRole,date??BillingClock.Today,Actor));}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException ex){return BadRequest(ex.Message);}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceModel model)
    {
        if(ModelState.IsValid)try{
            await new AttendanceService(db).SaveAsync(model,Actor);
            TempData["Success"]="Prezenca u ruajt.";
            return RedirectToAction(nameof(Index),new{teamId=model.TeamId,coachRole=model.CoachRole,date=model.Date!.Value.ToString("yyyy-MM-dd")});
        }
        catch(UnauthorizedAccessException){return NotFound();}
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        AttendanceModel current;
        try{current=await new AttendanceService(db).LoadAsync(model.TeamId,model.CoachRole,model.Date??BillingClock.Today,Actor);}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException ex){return BadRequest(ex.Message);}
        // Keep unsaved selections, but preserve the old revision/token so a stale
        // form cannot silently overwrite a colleague's newer attendance list.
        model.TeamName=current.TeamName;model.FieldName=current.FieldName;model.History=current.History;model.SavedAt=current.SavedAt;model.SavedBy=current.SavedBy;
        model.EditableUntil=current.EditableUntil;model.IsLocked=current.IsLocked;
        if(current.IsLocked){ModelState.Clear();ModelState.AddModelError("","Ka kaluar një orë nga ruajtja e parë. Prezenca nuk mund të ndryshohet.");return View("Index",current);}
        model.Players=(model.Players??[]).Where(x=>x!=null).ToList();
        foreach(var row in model.Players){
            var player=current.Players.FirstOrDefault(x=>x.StudentId==row.StudentId);
            row.Name=player?.Name??"Lojtar jashtë listës";
            row.Notes=player?.Notes;
            row.Debt=player?.Debt??0;
        }
        return View("Index",model);
    }
}
