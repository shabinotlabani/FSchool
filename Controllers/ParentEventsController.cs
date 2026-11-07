using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;
[AllowAnonymous,Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parent-portal")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[ParentModule("Events")]
public class ParentEventsController(ApplicationDbContext db):Controller
{
    [HttpGet("p/{token}/events")]
    public async Task<IActionResult> Index(string token,int? studentId,bool panel=false)
    {
        Response.Headers["Referrer-Policy"]="no-referrer";Response.Headers["X-Robots-Tag"]="noindex, nofollow, noarchive";
        Response.Headers["Content-Security-Policy"]="default-src 'none'; style-src 'self'; img-src 'self'; script-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        var access=await new ParentAccessService(db).Resolve(token);if(access==null)return NotFound();
        var player=studentId.HasValue?access.Students.FirstOrDefault(x=>x.Id==studentId):access.Students.FirstOrDefault();if(player==null)return NotFound();
        var rows=await db.EventInvitations.AsNoTracking().Include(x=>x.Event).Include(x=>x.Consents).Where(x=>x.StudentId==player.Id&&x.Event.Status!="Draft").OrderByDescending(x=>x.Event.StartsAt).ToListAsync();
        ViewData["Members"]=access.Students;ViewData["Payments"]=await db.Payments.AsNoTracking().Where(x=>x.StudentId==player.Id&&x.EventInvitationId!=null).OrderByDescending(x=>x.Id).ToListAsync();
        ViewData["Settlements"]=await db.EventSettlements.AsNoTracking().Where(x=>x.Invitation.StudentId==player.Id).ToListAsync();
        var model=new ParentEvents(token,player.Id,player.FullName,rows);
        return panel?PartialView("_Panel",model):View(model);
    }
    [HttpPost("p/{token}/events/respond"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(string token,int invitationId,int version,string decision,string guardian,bool terms,bool panel=false)
    {
        var access=await new ParentAccessService(db).Resolve(token);if(access==null)return NotFound();
        var ids=access.Students.Select(x=>x.Id).ToList();var studentId=await db.EventInvitations.Where(x=>x.Id==invitationId&&ids.Contains(x.StudentId)).Select(x=>(int?)x.StudentId).SingleOrDefaultAsync();
        if(studentId==null)return NotFound();
        try{await new ClubEventService(db).Respond(token,invitationId,version,decision,guardian??"",terms);TempData["Success"]="Përgjigjja juaj u ruajt.";}
        catch(Exception e)when(e is ValidationException or InvalidOperationException){TempData["Error"]=e.Message;}
        return RedirectToAction(nameof(Index),new{token,studentId,panel});
    }
    [HttpGet("p/{token}/events/{id:int}/photo")]
    public async Task<IActionResult> Photo(string token,int id)
    {
        var access=await new ParentAccessService(db).Resolve(token);if(access==null)return NotFound();var ids=access.Students.Select(x=>x.Id).ToList();
        if(!await db.EventInvitations.AnyAsync(x=>x.ClubEventId==id&&ids.Contains(x.StudentId)&&x.Event.Status!="Draft"))return NotFound();
        var e=await db.ClubEvents.Where(x=>x.Id==id).Select(x=>new{x.Photo,x.PhotoType}).SingleAsync();Response.Headers["X-Content-Type-Options"]="nosniff";
        return e.Photo==null?NotFound():File(e.Photo,e.PhotoType??"image/webp");
    }
}
