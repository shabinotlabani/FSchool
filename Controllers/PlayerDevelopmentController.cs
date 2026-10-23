using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Coach)]
public class PlayerDevelopmentController(ApplicationDbContext db) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private PlayerDevelopmentService Service => new(db);
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Position(int studentId,string playerRole,string? primaryPosition,string? secondaryPosition)
    {
        var catalog=await new SportDevelopmentService(db).Catalog();
        if(!catalog.Roles.Contains(playerRole))return BadRequest();
        try {
            await Service.Player(studentId,Actor);
            if(playerRole!="Portier"&&!catalog.Positions.Contains(primaryPosition))throw new ValidationException("Zgjidh pozitën kryesore.");
            await Service.SaveRecord(new DevelopmentRecordInput{Sport=catalog.Sport,StudentId=studentId,Kind="Profile",Values=new(){["PlayerRole"]=playerRole,
                ["PrimaryPosition"]=playerRole=="Portier"?"Pa caktuar":primaryPosition,
                ["SecondaryPosition"]=playerRole=="Portier"?"Asnjë":string.IsNullOrWhiteSpace(secondaryPosition)?"Asnjë":secondaryPosition}},Actor);
            return RedirectToAction(nameof(Player),new{id=studentId});
        }catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException ex){TempData["PositionError"]=ex.Message;return RedirectToAction(nameof(Player),new{id=studentId});}
    }
    public async Task<IActionResult> Index()
    {
        var players=await Service.Players(Actor).AsNoTracking().Include(x=>x.TrainingTeam).OrderBy(x=>x.TrainingTeam!.Name).ThenBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToListAsync();
        var ids=players.Select(x=>x.Id).ToList();
        var latest=await db.PlayerEvaluations.AsNoTracking().Where(x=>ids.Contains(x.StudentId) && !db.PlayerEvaluations.Any(y=>y.StudentId==x.StudentId && (y.Date>x.Date || y.Date==x.Date && y.Id>x.Id))).ToListAsync();
        return View(new DevelopmentRoster(players,latest));
    }
    public async Task<IActionResult> Player(int id,string section="Overview",int? year=null)
    {
        if(section is not ("Overview" or "Assessment" or "Progress") && DevelopmentSections.Find(section)==null)return BadRequest();
        try {
            var player=await Service.Player(id,Actor);
            var catalog=await new SportDevelopmentService(db).Catalog();
            ViewData["IndividualMenu"]=await db.NutritionMenus.AnyAsync(x=>x.Scope==id);
            if(section=="Progress"){
                ViewData["MedicalHistory"]=await db.CareEntries.AsNoTracking().Where(x=>x.Assignment.StudentId==id&&x.Assignment.Kind==Roles.Doctor&&x.Date<=BillingClock.Today)
                    .OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).Select(x=>new CareNotice(id,x.TrainingStatus!,x.CoachInstructions,x.Date,x.ReviewDate)).ToListAsync();
                ViewData["ReportedCases"]=await db.CareReferrals.AsNoTracking().Where(x=>x.StudentId==id&&x.CoachId==Actor&&x.Kind==Roles.Doctor).OrderByDescending(x=>x.CreatedAt).ToListAsync();
            }
            var evaluations=await db.PlayerEvaluations.AsNoTracking().Include(x=>x.Coach).Where(x=>x.StudentId==id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
            var records=await db.DevelopmentRecords.AsNoTracking().Include(x=>x.Coach).Where(x=>x.StudentId==id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
            return View(section=="Overview"?"Overview":section=="Progress"?"Progress":"Player",new DevelopmentHistory(player,evaluations.Where(x=>!year.HasValue||x.Date.Year==year).ToList(),Actor){
                Catalog=catalog, Records=records.Where(x=>!year.HasValue||x.Date.Year==year).ToList(),Year=year,Section=section,
                Years=evaluations.Select(x=>x.Date.Year).Concat(records.Select(x=>x.Date.Year)).Distinct().OrderDescending().ToList()});
        }
        catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpGet]
    public async Task<IActionResult> Record(int studentId,string kind="Profile",int id=0)
    {
        if(DevelopmentSections.Find(kind)==null)return BadRequest();
        try {
            ViewData["Player"]=await Service.Player(studentId,Actor);
            ViewData["Section"]=(await new SportDevelopmentService(db).Catalog()).Find(kind);
            if(id==0)return ViewData["Section"]==null?BadRequest():View(new DevelopmentRecordInput{Sport=await new SportDevelopmentService(db).Sport(),StudentId=studentId,Kind=kind});
            var row=await db.DevelopmentRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.StudentId==studentId&&x.CoachId==Actor&&x.Kind==kind);
            if(row==null)return NotFound();
            ViewData["Section"]=row.Schema;
            return View(new DevelopmentRecordInput{Sport=row.Sport,Id=id,StudentId=studentId,Kind=kind,Date=row.Date,Revision=row.Revision,Notes=row.Notes,Values=row.Values.ToDictionary(x=>x.Key,x=>kind=="Profile"&&x.Key=="PlayerRole"?DevelopmentSections.RoleLabel(x.Value):(string?)x.Value)});
        }catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(DevelopmentRecordInput input)
    {
        if(DevelopmentSections.Find(input.Kind)==null)return BadRequest();
        try {
            ViewData["Player"]=await Service.Player(input.StudentId,Actor);
            var stored=input.Id==0?null:await db.DevelopmentRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.Id&&x.StudentId==input.StudentId&&x.CoachId==Actor&&x.Kind==input.Kind);
            if(input.Id!=0&&stored==null)return NotFound();
            ViewData["Section"]=stored?.Schema??(await new SportDevelopmentService(db).Catalog()).Find(input.Kind);
            if(ViewData["Section"]==null)return BadRequest();
            if(ModelState.IsValid){await Service.SaveRecord(input,Actor);return RedirectToAction(nameof(Player),new{id=input.StudentId});}
        }catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException ex){ModelState.AddModelError("",ex.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Regjistrimi është ndryshuar. Rihapeni para ruajtjes.");}
        return View(input);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int studentId,int id=0)
    {
        try {
            ViewData["Player"]=await Service.Player(studentId,Actor);
            if(id==0)return View(new EvaluationInput{StudentId=studentId});
            var row=await db.PlayerEvaluations.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && x.StudentId==studentId && x.CoachId==Actor);
            if(row==null)return NotFound();
            return View(new EvaluationInput{Id=row.Id,StudentId=row.StudentId,Revision=row.Revision,Date=row.Date,Technique=row.Technique,Tactics=row.Tactics,Fitness=row.Fitness,Discipline=row.Discipline,Teamwork=row.Teamwork,Strengths=row.Strengths,Improvements=row.Improvements,Goals=row.Goals,ReviewDate=row.ReviewDate});
        } catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EvaluationInput input)
    {
        try {
            ViewData["Player"]=await Service.Player(input.StudentId,Actor);
            if(ModelState.IsValid){await Service.Save(input,Actor);return RedirectToAction(nameof(Player),new{id=input.StudentId});}
        } catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException ex){ModelState.AddModelError("",ex.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Vlerësimi është ndryshuar. Rihapeni para se të ruani.");}
        return View(input);
    }
}
