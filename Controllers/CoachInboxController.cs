using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Coach), ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public class CoachInboxController(ApplicationDbContext db) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private CoachChatService Chat => new(db);
    public async Task<IActionResult> Index(int page = 1) {
        try { ViewData["Page"] = Math.Clamp(page,1,10000); return View(await Chat.Inbox(Actor,page)); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
    public async Task<IActionResult> Thread(int studentId, long? before = null, string? returnUrl = null) {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index","CoachInbox");
        try { return View("~/Views/Shared/Chat.cshtml", await Chat.Load(studentId,Actor,null,before)); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(ChatInput input, string? returnUrl = null) {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index","CoachInbox");
        try {
            if (ModelState.IsValid) { await Chat.Send(input,Actor,null); return RedirectToAction(nameof(Thread),new{studentId=input.StudentId,returnUrl=ViewData["ReturnUrl"]}); }
        } catch (UnauthorizedAccessException) { return NotFound(); }
        catch (ValidationException e) { ModelState.AddModelError("",e.Message); }
        try { var model=await Chat.Load(input.StudentId,Actor,null); model.Input=input; return View("~/Views/Shared/Chat.cshtml",model); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
    public async Task<IActionResult> Messages(int studentId) {
        try { var model=await Chat.Load(studentId,Actor,null); return Json(ChatPayload.Messages(model)); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int studentId,long through) {
        try { await Chat.Read(studentId,Actor,null,through); return NoContent(); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
    public async Task<IActionResult> Deactivate(int studentId, string? returnUrl = null) {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Players","Coaches");
        try { var student=await Chat.Authorize(studentId,Actor,null); if(!student.IsActive)return NotFound();
            return View(new CoachDeactivateInput{StudentId=student.Id,Name=student.FullName,Revision=student.Revision,TeamId=student.TrainingTeamId!.Value}); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
    [HttpPost,ValidateAntiForgeryToken,ActionName("Deactivate")]
    public async Task<IActionResult> DeactivatePost(CoachDeactivateInput input, string? returnUrl = null) {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Players","Coaches");
        try {
            if(ModelState.IsValid){ var team=await new StudentWorkflowService(db).DeactivateByCoach(input,Actor);
                TempData["Success"]="Lojtari u pasivizua.";return LocalRedirect((string)ViewData["ReturnUrl"]!); }
        } catch(UnauthorizedAccessException){return NotFound();}
        catch(Exception e) when(e is ValidationException or InvalidOperationException){ModelState.AddModelError("",e.Message);}
        try { var student=await Chat.Authorize(input.StudentId,Actor,null);input.Name=student.FullName;input.TeamId=student.TrainingTeamId!.Value;return View("Deactivate",input); }
        catch(UnauthorizedAccessException){return NotFound();}
    }
}

internal static class ChatPayload
{
    public static object Messages(ChatView model) => new { messages=model.Messages.Select(m=>new {
        id=m.Id,body=m.Body,mine=m.FromParent==model.Parent,
        date=BillingClock.LocalTime(m.CreatedAt).ToString("dd/MM/yyyy HH:mm"),read=m.ReadAt!=null }),
        older=model.HasOlder };
}
