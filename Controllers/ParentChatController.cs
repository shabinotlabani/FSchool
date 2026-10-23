using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _2Korriku.Controllers;

[AllowAnonymous, Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parent-portal")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class ParentChatController(ApplicationDbContext db) : Controller
{
    private CoachChatService Chat => new(db);
    public override void OnActionExecuting(ActionExecutingContext context) {
        Response.Headers["Referrer-Policy"]="no-referrer";
        Response.Headers["X-Robots-Tag"]="noindex, nofollow, noarchive";
        Response.Headers["Content-Security-Policy"]="default-src 'none'; style-src 'self'; img-src 'self'; script-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        base.OnActionExecuting(context);
    }
    [HttpGet("p/{token}/chat")]
    public async Task<IActionResult> Thread(string token,int studentId,string? coachId=null,long? before=null) {
        var access=await new ParentAccessService(db).Resolve(token);
        if(access==null||!access.Students.Any(x=>x.Id==studentId))return NotFound();
        var coaches=await Chat.Recipients(studentId);
        coachId??=coaches.FirstOrDefault()?.Id;
        if(coachId==null)return View("Unavailable",studentId);
        try{return View("~/Views/Shared/Chat.cshtml",await Chat.Load(studentId,coachId,token,before));}
        catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost("p/{token}/chat/send"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string token,ChatInput input) {
        try{
            if(ModelState.IsValid){await Chat.Send(input,input.CoachId,token);return RedirectToAction(nameof(Thread),new{token,studentId=input.StudentId,coachId=input.CoachId});}
        }catch(UnauthorizedAccessException){return NotFound();}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        try{var model=await Chat.Load(input.StudentId,input.CoachId,token);model.Input=input;return View("~/Views/Shared/Chat.cshtml",model);}
        catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpGet("p/{token}/chat/messages")]
    public async Task<IActionResult> Messages(string token,int studentId,string coachId) {
        try{return Json(ChatPayload.Messages(await Chat.Load(studentId,coachId,token)));}
        catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost("p/{token}/chat/read"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(string token,int studentId,string coachId,long through) {
        try{await Chat.Read(studentId,coachId,token,through);return NoContent();}
        catch(UnauthorizedAccessException){return NotFound();}
    }
}
