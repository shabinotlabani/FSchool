using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Admin),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class LeaguesController(ApplicationDbContext db):Controller
{
    public async Task<IActionResult> Index()=>View(await db.Leagues.AsNoTracking().Include(l=>l.Teams).ThenInclude(t=>t.TrainingTeam).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync());
    public async Task<IActionResult> Edit(int id=0)
    {
        var row=id==0?null:await db.Leagues.AsNoTracking().Include(l=>l.Teams).SingleOrDefaultAsync(l=>l.Id==id);
        if(id!=0&&row==null)return NotFound();
        var input=row==null?new LeagueInput():new LeagueInput{Id=row.Id,Revision=row.Revision,Name=row.Name,Kind=row.Kind,StartsOn=row.StartsOn,EndsOn=row.EndsOn,IsActive=row.IsActive,RulesConfirmed=row.RulesConfirmed,YellowThreshold=row.YellowThreshold==0?null:row.YellowThreshold,YellowBan=row.YellowBan==0?null:row.YellowBan,RedBan=row.RedBan,SecondYellowBan=row.SecondYellowBan,CountDismissalYellows=row.CountDismissalYellows,TeamIds=row.Teams.Select(t=>t.TrainingTeamId).ToList()};
        await Choices(input);return View(input);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeagueInput input)
    {
        if(ModelState.IsValid)try{await new LeagueService(db).Save(input,User.FindFirstValue(ClaimTypes.NameIdentifier)!);TempData["Success"]="Liga u ruajt.";return RedirectToAction(nameof(Index));}
        catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Liga ndryshoi. Rihapeni para ruajtjes.");}
        await Choices(input);return View(input);
    }
    private async Task Choices(LeagueInput input)=>input.Teams=await db.TrainingTeams.AsNoTracking().OrderBy(t=>t.Name).ToListAsync();
}
