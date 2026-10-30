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
public class AdminMatchesController(ApplicationDbContext db,GeminiFixtureService fixtureAi):Controller
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

    public async Task<IActionResult> Propose(int? teamId=null,int? leagueId=null)
    {
        var model=new MatchFixtureProposalModel{TeamId=teamId??0,LeagueId=leagueId??0};await ProposalChoices(model);return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Propose(MatchFixtureProposalModel input)
    {
        try{var model=await fixtureAi.Propose(input.TeamId,input.LeagueId,input.From,HttpContext.RequestAborted);await ProposalChoices(model);return View(model);}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);await ProposalChoices(input);return View(input);}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportProposals(MatchFixtureProposalModel input)
    {
        input.Selected??=[];input.Fixtures??=[];
        var league=await db.Leagues.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.LeagueId&&x.IsActive&&x.Teams.Any(t=>t.TrainingTeamId==input.TeamId));if(league==null)return NotFound();
        var selected=input.Fixtures.Where(x=>input.Selected.Contains(x.Index)).GroupBy(x=>x.Index).Select(x=>x.First()).ToList();
        if(selected.Count==0){ModelState.AddModelError("","Zgjidhni së paku një ndeshje.");await ProposalChoices(input);return View("Propose",input);}
        if(selected.Any(x=>x.Date<BillingClock.Today||x.Date<league.StartsOn||x.Date>league.EndsOn||string.IsNullOrWhiteSpace(x.Opponent)||x.Opponent.Trim().Length>150||!GeminiFixtureService.OfficialSource(x.SourceUrl,league.Organizer))){ModelState.AddModelError("","Një propozim nuk është më i vlefshëm ose nuk ka burim zyrtar.");await ProposalChoices(input);return View("Propose",input);}
        var added=0;
        foreach(var row in selected.OrderBy(x=>x.Date).ThenBy(x=>x.Kickoff)){
            if(await db.MatchGames.AnyAsync(x=>x.TrainingTeamId==input.TeamId&&x.LeagueId==input.LeagueId&&x.Date==row.Date&&x.Opponent.ToUpper()==row.Opponent.Trim().ToUpper()))continue;
            await Service.Save(Actor,new(){TeamId=input.TeamId,LeagueId=input.LeagueId,Opponent=row.Opponent.Trim(),Date=row.Date,Kickoff=row.Kickoff,Home=row.Home,Venue=row.Venue,CompetitionStage=row.Stage,Status="Draft",Players=[],Events=[]},true);added++;
        }
        TempData["Success"]=$"U shtuan {added} ndeshje si propozime.";return RedirectToAction(nameof(Index),new{teamId=input.TeamId,year=input.From.Year});
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
    private async Task ProposalChoices(MatchFixtureProposalModel model)
    {
        model.Teams=await db.TrainingTeams.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();
        model.Leagues=await db.Leagues.AsNoTracking().Where(x=>x.IsActive&&(model.TeamId==0||x.Teams.Any(t=>t.TrainingTeamId==model.TeamId))).OrderBy(x=>x.Name).ToListAsync();
        model.TeamName=model.Teams.FirstOrDefault(x=>x.Id==model.TeamId)?.Name??model.TeamName;model.LeagueName=model.Leagues.FirstOrDefault(x=>x.Id==model.LeagueId)?.Name??model.LeagueName;
    }
}
