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
    public async Task<IActionResult> Index(string? sport,string? gender,string? season,string? organizer,string? ageCategory,string? scope)
    {
        var query=db.Leagues.AsNoTracking().Include(l=>l.Teams).ThenInclude(t=>t.TrainingTeam).AsQueryable();
        if(!string.IsNullOrWhiteSpace(sport))query=query.Where(x=>x.Sport==sport);
        if(!string.IsNullOrWhiteSpace(gender))query=query.Where(x=>x.Gender==gender);
        if(!string.IsNullOrWhiteSpace(season))query=query.Where(x=>x.Season==season);
        if(!string.IsNullOrWhiteSpace(organizer))query=query.Where(x=>x.Organizer==organizer);
        if(!string.IsNullOrWhiteSpace(ageCategory))query=query.Where(x=>x.AgeCategory==ageCategory);
        if(!string.IsNullOrWhiteSpace(scope))query=query.Where(x=>x.CompetitionScope==scope);
        return View(new CompetitionListModel{Sport=sport,Gender=gender,Season=season,Organizer=organizer,AgeCategory=ageCategory,Scope=scope,Rows=await query.OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync()});
    }
    public async Task<IActionResult> Edit(int id=0)
    {
        var row=id==0?null:await db.Leagues.AsNoTracking().Include(l=>l.Teams).SingleOrDefaultAsync(l=>l.Id==id);
        if(id!=0&&row==null)return NotFound();
        var input=row==null?new LeagueInput():new LeagueInput{Id=row.Id,Revision=row.Revision,Name=row.Name,Kind=row.Kind,Country=row.Country,Organizer=row.Organizer,Sport=row.Sport,Gender=row.Gender,Season=row.Season,AgeCategory=row.AgeCategory,CompetitionScope=row.CompetitionScope,CompetitionType=row.CompetitionType,Division=row.Division,Group=row.Group,Stage=row.Stage,IsOfficial=row.IsOfficial,IsVerified=row.IsVerified,StartsOn=row.StartsOn,EndsOn=row.EndsOn,IsActive=row.IsActive,RulesConfirmed=row.RulesConfirmed,YellowThreshold=row.YellowThreshold==0?null:row.YellowThreshold,YellowBan=row.YellowBan==0?null:row.YellowBan,RedBan=row.RedBan,SecondYellowBan=row.SecondYellowBan,CountDismissalYellows=row.CountDismissalYellows,TeamIds=row.Teams.Select(t=>t.TrainingTeamId).ToList(),TeamStatuses=row.Teams.ToDictionary(t=>t.TrainingTeamId,t=>t.Status),TeamDeadlines=row.Teams.ToDictionary(t=>t.TrainingTeamId,t=>t.SquadDeadline)};
        await Choices(input);return View(input);
    }
    public async Task<IActionResult> Catalog(string? season=null)
    {
        var current=LeagueService.CurrentSeason();var model=new CompetitionCatalogModel{Season=current.Name,StartsOn=current.StartsOn,EndsOn=current.EndsOn};
        await CatalogChoices(model);return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Catalog(CompetitionCatalogModel input)
    {
        if(ModelState.IsValid)try{var added=await new LeagueService(db).AddFromCatalog(input,User.FindFirstValue(ClaimTypes.NameIdentifier)!);TempData["Success"]=$"U shtuan {added} gara për sezonin {input.Season}.";return RedirectToAction(nameof(Index),new{season=input.Season});}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        await CatalogChoices(input);return View(input);
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
    public async Task<IActionResult> Squad(int id,int teamId,string listType="Main")
    {
        var registration=await db.LeagueTeams.AsNoTracking().Include(x=>x.League).Include(x=>x.TrainingTeam).SingleOrDefaultAsync(x=>x.LeagueId==id&&x.TrainingTeamId==teamId);
        if(registration==null)return NotFound();
        var players=await db.Students.AsNoTracking().Include(x=>x.TrainingTeam).Where(x=>x.IsActive).OrderBy(x=>x.TrainingTeam!.Name).ThenBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToListAsync();
        var selected=await db.CompetitionPlayerRegistrations.AsNoTracking().Where(x=>x.LeagueTeamId==registration.Id&&x.ListType==listType&&x.Status=="Eligible").Select(x=>x.StudentId).ToListAsync();
        return View(new CompetitionSquadModel{LeagueId=id,TeamId=teamId,CompetitionName=registration.League.Name,TeamName=registration.TrainingTeam.Name,ListType=listType,Deadline=registration.SquadDeadline,Players=players,StudentIds=selected});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Squad(CompetitionSquadModel input)
    {
        input.StudentIds??=[];
        if(input.ListType is not("Main" or "A" or "B")){ModelState.AddModelError("","Zgjidhni listën e vlefshme.");return await Squad(input.LeagueId,input.TeamId);}
        var registration=await db.LeagueTeams.Include(x=>x.League).SingleOrDefaultAsync(x=>x.LeagueId==input.LeagueId&&x.TrainingTeamId==input.TeamId);
        if(registration==null)return NotFound();
        if(registration.SquadDeadline.HasValue&&registration.SquadDeadline<BillingClock.Today){ModelState.AddModelError("","Afati i listës ka përfunduar.");return await Squad(input.LeagueId,input.TeamId,input.ListType);}
        var ids=input.StudentIds.Distinct().ToList();
        if(await db.Students.CountAsync(x=>ids.Contains(x.Id)&&x.IsActive)!=ids.Count){ModelState.AddModelError("","Lista përmban lojtar joaktiv ose jashtë klubit.");return await Squad(input.LeagueId,input.TeamId,input.ListType);}
        var rows=await db.CompetitionPlayerRegistrations.Where(x=>x.LeagueTeamId==registration.Id&&x.ListType==input.ListType).ToListAsync();
        foreach(var row in rows)row.Status=ids.Contains(row.StudentId)?"Eligible":"Removed";
        foreach(var id in ids.Where(id=>!rows.Any(x=>x.StudentId==id)))db.CompetitionPlayerRegistrations.Add(new(){LeagueTeamId=registration.Id,StudentId=id,ListType=input.ListType,Status="Eligible",RegisteredOn=BillingClock.Today,EligibleFrom=registration.League.StartsOn,EligibleUntil=registration.League.EndsOn});
        await db.SaveChangesAsync();TempData["Success"]="Lista e lojtarëve u ruajt.";return RedirectToAction(nameof(Squad),new{id=input.LeagueId,teamId=input.TeamId,listType=input.ListType});
    }
    private async Task Choices(LeagueInput input)=>input.Teams=await db.TrainingTeams.AsNoTracking().OrderBy(t=>t.Name).ToListAsync();
    private async Task CatalogChoices(CompetitionCatalogModel input)
    {
        input.Items=LeagueService.Catalog.ToList();
        var existing=await db.Leagues.AsNoTracking().Where(x=>x.Season==input.Season).Select(x=>x.Name).ToListAsync();
        input.ExistingKeys=LeagueService.Catalog.Where(x=>existing.Contains(x.Name)).Select(x=>x.Key).ToHashSet();
    }
}
