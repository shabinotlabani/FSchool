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
public class AdminMatchesController(ApplicationDbContext db):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private MatchService Service=>new(db);

    public async Task<IActionResult> Index(int? teamId,int? year)
    {
        var selectedYear=year??BillingClock.Today.Year;
        if(selectedYear is <1900 or >2200)return BadRequest();
        var teams=await db.TrainingTeams.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();
        if(teamId.HasValue&&!teams.Any(x=>x.Id==teamId))return NotFound();
        var matches=await db.MatchGames.AsNoTracking().Include(x=>x.TrainingTeam).Include(x=>x.League).Include(x=>x.UpdatedBy)
            .Where(x=>x.Kind!="Friendly"&&x.Date.Year==selectedYear&&(!teamId.HasValue||x.TrainingTeamId==teamId))
            .OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Kickoff).ToListAsync();
        return View(new MatchList{Teams=teams,Matches=matches,TeamId=teamId,Year=selectedYear});
    }

    public async Task<IActionResult> Edit(int id=0,int? teamId=null)
    {
        try{
            teamId??=id==0?await db.TrainingTeams.Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>(int?)x.Id).FirstOrDefaultAsync():null;
            if(id==0&&!teamId.HasValue)return RedirectToAction(nameof(Index));
            return View("~/Views/Matches/Edit.cshtml",await Service.Load(Actor,id,teamId??0,true));
        }catch(UnauthorizedAccessException){return NotFound();}
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MatchInput input,string? command=null)
    {
        if(input.Id==0){input.Status="Draft";input.ScoreFor=null;input.ScoreAgainst=null;input.Players=[];input.Events=[];}
        if(!input.LeagueId.HasValue)ModelState.AddModelError(nameof(input.LeagueId),"Zgjidhni ligën ose kupën e ndeshjes zyrtare.");
        try{
            if(input.Id!=0){
                var existing=await Service.Load(Actor,input.Id,input.TeamId,true);
                if(command=="Confirm"){input.Status="Live";input.ScoreFor??=0;input.ScoreAgainst??=0;ModelState.Remove(nameof(input.Status));}
                else if(command=="Finish"){input.Status="Completed";ModelState.Remove(nameof(input.Status));}
                else input.Status=existing.Status;
            }
            if(ModelState.IsValid){
                var id=await Service.Save(Actor,input,true);
                TempData["Success"]=input.Id==0?"Ndeshja zyrtare u krijua.":"Korrigjimet u ruajtën.";
                return RedirectToAction(command=="Finish"?nameof(Summary):nameof(Edit),new{id});
            }
        }catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Ndeshja ndryshoi. Rihapeni para ruajtjes.");}
        try{
            var fresh=await Service.Load(Actor,input.Id,input.TeamId,true);
            input.Leagues=fresh.Leagues;input.DisciplineAlerts=fresh.DisciplineAlerts;input.HasDisciplineRule=fresh.HasDisciplineRule;input.TeamName=fresh.TeamName;input.Sport=fresh.Sport;
            foreach(var player in input.Players){var known=fresh.Players.SingleOrDefault(x=>x.StudentId==player.StudentId);if(known==null)return NotFound();player.Name=known.Name;player.Care=known.Care;}
            return View("~/Views/Matches/Edit.cshtml",input);
        }catch(UnauthorizedAccessException){return NotFound();}
    }

    public async Task<IActionResult> Summary(int id)
    {
        var game=await db.MatchGames.AsNoTracking().Include(x=>x.TrainingTeam).Include(x=>x.Players).SingleOrDefaultAsync(x=>x.Id==id&&x.Kind!="Friendly");
        if(game==null)return NotFound();
        ViewData["AdminMatch"]=true;
        return View("~/Views/Matches/Summary.cshtml",game);
    }
}
