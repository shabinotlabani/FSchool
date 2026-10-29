using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Coach), ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class MatchesController(ApplicationDbContext db) : Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private MatchService Service=>new(db);
    public async Task<IActionResult> Matrix(int? teamId,int? year,string? kind,int? leagueId,string? competitionFilter)
    {
        try{
            teamId??=await Service.Teams(Actor).OrderBy(t=>t.Name).Select(t=>(int?)t.Id).FirstOrDefaultAsync();
            if(teamId==null)return RedirectToAction(nameof(Index));
            var report=await Service.Report(Actor,teamId,year??BillingClock.Today.Year,string.IsNullOrEmpty(kind)?null:kind,null,leagueId,competitionFilter);
            var students=await db.Students.AsNoTracking().Where(s=>s.TrainingTeamId==teamId&&s.IsActive).ToListAsync();
            var rows=report.Totals.ToDictionary(p=>p.StudentId);
            foreach(var s in students)rows.TryAdd(s.Id,new(s.Id,s.FullName,0,0,0,0,0,0));
            return View(new MatchMatrix{Report=report,Players=rows.Values.OrderBy(p=>p.Name).ToList()});
        }catch(UnauthorizedAccessException){return NotFound();}catch(ValidationException){return BadRequest();}
    }
    public async Task<IActionResult> LeagueRules(int id,int teamId)
    {
        if(!await Service.Teams(Actor).AnyAsync(t=>t.Id==teamId))return NotFound();
        var league=await db.Leagues.AsNoTracking().SingleOrDefaultAsync(l=>l.Id==id&&l.Teams.Any(t=>t.TrainingTeamId==teamId));
        if(league==null)return NotFound();
        ViewData["TeamId"]=teamId;return View(league);
    }
    public async Task<IActionResult> Discipline(int teamId,string kind="Official",DateOnly? date=null)
    {
        var team=await Service.Teams(Actor).SingleOrDefaultAsync(t=>t.Id==teamId);
        if(team==null||kind is not("Official" or "Cup" or "Friendly"))return NotFound();
        var day=date??BillingClock.Today;
        var rule=await db.DisciplineRules.AsNoTracking().SingleOrDefaultAsync(r=>r.TrainingTeamId==teamId&&r.Kind==kind&&r.StartsOn<=day&&r.EndsOn>=day);
        var start=new DateOnly(day.Month>=7?day.Year:day.Year-1,7,1);
        return View(rule==null?new DisciplineInput{TeamId=teamId,TeamName=team.Name,Kind=kind,StartsOn=start,EndsOn=start.AddYears(1).AddDays(-1)}:new DisciplineInput{Id=rule.Id,Revision=rule.Revision,TeamId=teamId,TeamName=team.Name,Kind=kind,StartsOn=rule.StartsOn,EndsOn=rule.EndsOn,YellowThreshold=rule.YellowThreshold,YellowBan=rule.YellowBan,RedBan=rule.RedBan,SecondYellowBan=rule.SecondYellowBan,CountDismissalYellows=rule.CountDismissalYellows});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Discipline(DisciplineInput input)
    {
        var team=await Service.Teams(Actor).SingleOrDefaultAsync(t=>t.Id==input.TeamId);
        if(team==null)return NotFound();
        input.TeamName=team.Name;
        if(input.Kind is not("Official" or "Cup" or "Friendly")||input.StartsOn.Year<1900||input.EndsOn.Year>2200||input.EndsOn<input.StartsOn)ModelState.AddModelError("","Kontrolloni garën dhe sezonin.");
        if(!ModelState.IsValid)return View(input);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        if(!await Service.Teams(Actor).AnyAsync(t=>t.Id==input.TeamId))return NotFound();
        var row=input.Id==0?new DisciplineRule{TrainingTeamId=input.TeamId}:await db.DisciplineRules.SingleOrDefaultAsync(r=>r.Id==input.Id&&r.TrainingTeamId==input.TeamId);
        if(row==null)return NotFound();
        if(input.Id!=0&&row.Revision!=input.Revision){ModelState.AddModelError("","Rregulli ndryshoi. Rihapeni.");return View(input);}
        if(await db.DisciplineRules.AnyAsync(r=>r.Id!=input.Id&&r.TrainingTeamId==input.TeamId&&r.Kind==input.Kind&&r.StartsOn<=input.EndsOn&&r.EndsOn>=input.StartsOn)){ModelState.AddModelError("","Ky sezon mbivendoset me një rregull tjetër.");return View(input);}
        row.Kind=input.Kind;row.StartsOn=input.StartsOn;row.EndsOn=input.EndsOn;row.YellowThreshold=input.YellowThreshold;row.YellowBan=input.YellowBan;row.RedBan=0;row.SecondYellowBan=input.SecondYellowBan;row.CountDismissalYellows=input.CountDismissalYellows;row.UpdatedById=Actor;row.UpdatedAt=DateTime.UtcNow;row.Revision=Guid.NewGuid();
        if(input.Id==0)db.DisciplineRules.Add(row);
        await db.SaveChangesAsync();await tx.CommitAsync();
        return RedirectToAction(nameof(Index),new{teamId=input.TeamId,year=input.StartsOn.Year});
    }
    public async Task<IActionResult> Index(int? teamId,int? year,string? kind)
    {
        try{return View(await Service.List(Actor,teamId,year??BillingClock.Today.Year,string.IsNullOrEmpty(kind)?null:kind));}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException){return BadRequest();}
    }
    public async Task<IActionResult> Edit(int id=0,int? teamId=null)
    {
        try{
            if(id==0&&!teamId.HasValue)teamId=await Service.Teams(Actor).OrderBy(t=>t.Name).Select(t=>(int?)t.Id).FirstOrDefaultAsync();
            if(id==0&&!teamId.HasValue)return RedirectToAction(nameof(Index));
            return View(await Service.Load(Actor,id,teamId??0));
        }catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> CallUp(int id,int studentId,string? reason)
    {
        try{await Service.CallUp(Actor,id,studentId,reason);TempData["Success"]="Lojtari u shtua për këtë ndeshje.";return RedirectToAction(nameof(Edit),new{id});}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException e){TempData["Error"]=e.Message;return RedirectToAction(nameof(Edit),new{id});}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MatchInput input,string? command=null)
    {
        if(input.Id==0){input.Status="Draft";input.ScoreFor=null;input.ScoreAgainst=null;input.Players=[];input.Events=[];}
        try{
            if(input.Id!=0){
                var existing=await Service.Load(Actor,input.Id,input.TeamId);
                if(existing.Sport=="Football"){
                    if(command=="Confirm"&&existing.Status!="Draft")throw new ValidationException("Formacioni është konfirmuar tashmë.");
                    if(command=="Finish"&&existing.Status is not ("Live" or "Completed"))throw new ValidationException("Konfirmoni formacionin para përfundimit të ndeshjes.");
                    input.Status=command=="Confirm"&&existing.Status=="Draft"?"Live":command=="Finish"&&existing.Status is "Live" or "Completed"?"Completed":existing.Status;
                    ModelState.Remove(nameof(input.Status));
                    if(command=="Confirm"){input.ScoreFor=0;input.ScoreAgainst=0;}
                }
            }
            if(ModelState.IsValid){var id=await Service.Save(Actor,input);TempData["Success"]=command=="Confirm"?"Formacioni u konfirmua. Tani regjistro ngjarjet.":input.Id==0?"Ndeshja u krijua.":"Ndeshja u ruajt.";return RedirectToAction(command=="Finish"?nameof(Summary):nameof(Edit),new{id});}
        }catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Ndeshja ndryshoi. Rihapeni para ruajtjes.");}
        try{
            var fresh=await Service.Load(Actor,input.Id,input.TeamId);
            input.Status=fresh.Status;ModelState.Remove(nameof(input.Status));
            input.Leagues=fresh.Leagues;input.DisciplineAlerts=fresh.DisciplineAlerts;input.HasDisciplineRule=fresh.HasDisciplineRule;input.TeamName=fresh.TeamName;input.Sport=fresh.Sport;input.CallUpCandidates=fresh.CallUpCandidates;
            if(input.Players==null)return BadRequest();
            foreach(var p in input.Players){var known=fresh.Players.SingleOrDefault(x=>x.StudentId==p.StudentId);if(known==null)return NotFound();p.Name=known.Name;p.Care=known.Care;}
            return View(input);
        }catch(UnauthorizedAccessException){return NotFound();}
    }
    public async Task<IActionResult> Summary(int id)
    {
        var teams=Service.Teams(Actor).Select(t=>t.Id);
        var game=await db.MatchGames.AsNoTracking().Include(g=>g.TrainingTeam).Include(g=>g.Players).SingleOrDefaultAsync(g=>g.Id==id&&teams.Contains(g.TrainingTeamId));
        return game==null?NotFound():View(game);
    }
    public async Task<IActionResult> Report(int? teamId,int? year,string? kind,int? studentId,int? leagueId,string? competitionFilter)
    {
        try{return View(await Service.Report(Actor,teamId,year??BillingClock.Today.Year,string.IsNullOrEmpty(kind)?null:kind,studentId,leagueId,competitionFilter));}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException){return BadRequest();}
    }
}
