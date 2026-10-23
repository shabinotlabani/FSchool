using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Coach+","+Roles.Doctor+","+Roles.Nutritionist)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class CareReferralsController(ApplicationDbContext db):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [Authorize(Roles=Roles.Coach)]
    public async Task<IActionResult> Create(int studentId)
    {
        try{ViewData["Player"]=await new PlayerDevelopmentService(db).Player(studentId,Actor);
            ViewData["Cases"]=await db.CareReferrals.AsNoTracking().Where(x=>x.StudentId==studentId&&x.CoachId==Actor).OrderByDescending(x=>x.CreatedAt).ToListAsync();
            return View(new ReferralInput{StudentId=studentId});}catch(UnauthorizedAccessException){return NotFound();}
    }
    [Authorize(Roles=Roles.Coach),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReferralInput input)
    {
        try{
            await using var tx=await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");
            await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
            ViewData["Player"]=await new PlayerDevelopmentService(db).Player(input.StudentId,Actor);
            if(ModelState.IsValid){db.CareReferrals.Add(new(){StudentId=input.StudentId,CoachId=Actor,Kind=input.Kind,Reason=input.Reason.Trim(),CreatedAt=DateTime.UtcNow,MenuForCoach=input.Kind==Roles.Nutritionist,MenuStatus=input.Kind==Roles.Nutritionist?"Pending":null,MenuNextAttempt=input.Kind==Roles.Nutritionist?DateTime.UtcNow:null});await db.SaveChangesAsync();await tx.CommitAsync();TempData["Success"]="Rasti u krijua.";return RedirectToAction(nameof(Create),new{input.StudentId});}
            ViewData["Cases"]=await db.CareReferrals.AsNoTracking().Where(x=>x.StudentId==input.StudentId&&x.CoachId==Actor).OrderByDescending(x=>x.CreatedAt).ToListAsync();return View(input);
        }catch(UnauthorizedAccessException){return NotFound();}
    }
    [Authorize(Roles=Roles.Doctor+","+Roles.Nutritionist)]
    public async Task<IActionResult> Index()
    {
        try{var service=new PlayerCareService(db);var kind=await service.Kind(Actor);var ids=service.Assignments(Actor,kind).Select(x=>x.StudentId);
            return View(await db.CareReferrals.AsNoTracking().Include(x=>x.Student).Where(x=>x.Kind==kind&&ids.Contains(x.StudentId)).OrderBy(x=>x.ClosedAt!=null).ThenByDescending(x=>x.CreatedAt).ToListAsync());}
        catch(UnauthorizedAccessException){return Forbid();}
    }
    [Authorize(Roles=Roles.Doctor+","+Roles.Nutritionist),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2023)");
        var row=await db.CareReferrals.SingleOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();
        try{var a=await new PlayerCareService(db).Assigned(row.StudentId,Actor);if(a.Kind!=row.Kind)return NotFound();
            if(row.ClosedAt==null){row.ClosedAt=DateTime.UtcNow;row.ClosedBy=Actor;await db.SaveChangesAsync();}await tx.CommitAsync();return RedirectToAction(nameof(Index));}
        catch(UnauthorizedAccessException){return NotFound();}
    }
}
