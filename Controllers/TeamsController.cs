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
public class TeamsController(ApplicationDbContext db,TeamService teams):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(string? search,bool includeInactive=false)
    {
        ViewData["Search"]=search;ViewData["IncludeInactive"]=includeInactive;
        var rows=await teams.OptionsAsync(includeInactive:includeInactive);
        if(!string.IsNullOrWhiteSpace(search))rows=rows.Where(t=>t.Team.Name.Contains(search.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
        return View(rows);
    }
    public async Task<IActionResult> Details(int id)
    {
        var team=await db.TrainingTeams.AsNoTracking().Include(t=>t.FootballField).Include(t=>t.Sessions).Include(t=>t.Students).SingleOrDefaultAsync(t=>t.Id==id);if(team==null)return NotFound();
        return View(new TeamDetailsModel{Team=team,History=await db.TeamChanges.AsNoTracking().Include(c=>c.Actor).Where(c=>c.TrainingTeamId==id).OrderByDescending(c=>c.Id).ToListAsync(),MembershipHistory=await db.StudentTeamChanges.AsNoTracking().Include(c=>c.Student).Include(c=>c.Actor).Where(c=>c.FromTeamId==id||c.ToTeamId==id).OrderByDescending(c=>c.Id).ToListAsync()});
    }
    private async Task FieldChoices(TeamEditModel model) => model.Fields = await db.FootballFields.AsNoTracking().Where(f => f.IsActive || f.Id == model.FootballFieldId).OrderBy(f => f.Name).ToListAsync();
    [HttpGet,Authorize(Roles=Roles.Admin)]public async Task<IActionResult> Create(){var model=new TeamEditModel();await FieldChoices(model);return View("Edit",model);}
    [HttpGet,Authorize(Roles=Roles.Admin)]public async Task<IActionResult> Edit(int id)
    {
        var team=await db.TrainingTeams.AsNoTracking().Include(t=>t.Sessions).SingleOrDefaultAsync(t=>t.Id==id);if(team==null)return NotFound();
        var model = new TeamEditModel{FootballFieldId=team.FootballFieldId,Id=id,Revision=team.Revision,Name=team.Name,MinAge=team.MinAge,MaxAge=team.MaxAge,Capacity=team.Capacity,IsActive=team.IsActive,Location=team.Location,Notes=team.Notes,Sessions=team.Sessions.OrderBy(s=>s.Day).ThenBy(s=>s.StartsAt).Select(s=>new TrainingSessionInput{Day=s.Day,StartsAt=s.StartsAt,EndsAt=s.EndsAt}).ToList()};await FieldChoices(model);return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]public async Task<IActionResult> Save(TeamEditModel model)
    {
        if(ModelState.IsValid)try{var id=await teams.SaveAsync(model,Actor);TempData["Success"]="Ekipi dhe orari u ruajtën.";return RedirectToAction(nameof(Details),new{id});}catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        if(model.Sessions==null||model.Sessions.Count==0)model.Sessions=[new()];
        await FieldChoices(model);return View("Edit",model);
    }
    [HttpGet,Authorize(Roles=Roles.Write),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]public async Task<IActionResult> Suggestions(DateOnly? dateOfBirth,int? excludingStudentId)
    {
        if(!ModelState.IsValid||!dateOfBirth.HasValue||dateOfBirth==DateOnly.MinValue||dateOfBirth>BillingClock.Today)return BadRequest("Datëlindja nuk është e vlefshme.");
        var options=await teams.OptionsAsync(dateOfBirth,excludingStudentId:excludingStudentId);
        return Json(new{age=TeamService.Age(dateOfBirth.Value),teams=options.Select(o=>new{id=o.Team.Id,name=o.Team.Name,minAge=o.Team.MinAge,maxAge=o.Team.MaxAge,capacity=o.Team.Capacity,enrolled=o.Enrolled,freePlaces=o.FreePlaces,location=o.Team.FootballField?.Name ?? o.Team.Location,sessions=o.Team.Sessions.OrderBy(s=>s.Day).ThenBy(s=>s.StartsAt).Select(s=>new{day=TeamService.DayName(s.Day),start=s.StartsAt.ToString("HH:mm"),end=s.EndsAt.ToString("HH:mm")})})});
    }
    [HttpGet,Authorize(Roles=Roles.Write)]public async Task<IActionResult> Assign(int id)
    {
        var student=await db.Students.AsNoTracking().Include(s=>s.TrainingTeam).SingleOrDefaultAsync(s=>s.Id==id);if(student==null)return NotFound();
        return View(new ChangeStudentTeamModel{StudentId=id,TeamId=student.TrainingTeamId,ExpectedTeamId=student.TrainingTeamId,Student=student,Options=await teams.OptionsAsync(student.DateOfBirth,excludingStudentId:id)});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)]public async Task<IActionResult> Assign(ChangeStudentTeamModel model)
    {
        model.Student=await db.Students.AsNoTracking().Include(s=>s.TrainingTeam).SingleOrDefaultAsync(s=>s.Id==model.StudentId);if(model.Student==null)return NotFound();
        if(ModelState.IsValid)try{await teams.ChangeStudentAsync(model,Actor);TempData["Success"]="Caktimi i ekipit u ruajt në historik.";return RedirectToAction("Account","Payments",new{id=model.StudentId});}catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        model.Options=await teams.OptionsAsync(model.Student.DateOfBirth,excludingStudentId:model.StudentId);return View(model);
    }
}
