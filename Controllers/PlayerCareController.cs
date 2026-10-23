using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Doctor+","+Roles.Nutritionist)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class PlayerCareController(ApplicationDbContext db):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private PlayerCareService Service=>new(db);
    public async Task<IActionResult> Index()
    {
        string kind;try{kind=await Service.Kind(Actor);}catch(UnauthorizedAccessException){return Forbid();}ViewData["Kind"]=kind;
        var assigned=await Service.Assignments(Actor,kind).AsNoTracking().Include(x=>x.Student.TrainingTeam).ToListAsync();
        var ids=assigned.Select(x=>x.Id).ToList();
        var entries=await db.CareEntries.AsNoTracking().Where(x=>ids.Contains(x.AssignmentId)).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        return View(assigned.Select(x=>new CareRosterRow(x.Student,entries.FirstOrDefault(e=>e.AssignmentId==x.Id))).OrderBy(x=>x.Latest?.ReviewDate??DateOnly.MaxValue).ThenBy(x=>x.Player.FirstName).ToList());
    }
    public async Task<IActionResult> Player(int id)
    {
        try{var a=await Service.Assigned(id,Actor);return View(new CarePage(a.Student,a.Kind,await db.CareEntries.AsNoTracking().Where(x=>x.AssignmentId==a.Id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync(),
            await db.DevelopmentRecords.AsNoTracking().Where(x=>x.StudentId==id&&x.Kind=="Measurements").OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync()));}
        catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpGet]
    public async Task<IActionResult> Visit(int studentId)
    {
        try{var a=await Service.Assigned(studentId,Actor);ViewData["Player"]=a.Student;ViewData["Kind"]=a.Kind;
            var last=await db.CareEntries.AsNoTracking().Where(x=>x.AssignmentId==a.Id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).FirstOrDefaultAsync();
            return View(new CareInput{StudentId=studentId,TrainingStatus=last?.TrainingStatus,CoachInstructions=last?.CoachInstructions});}
        catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Visit(CareInput input)
    {
        try{var a=await Service.Assigned(input.StudentId,Actor);ViewData["Player"]=a.Student;ViewData["Kind"]=a.Kind;
            if(ModelState.IsValid){await Service.Save(input,Actor);return RedirectToAction(nameof(Player),new{id=input.StudentId});}}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException ex){ModelState.AddModelError("",ex.Message);}
        return View(input);
    }
}
