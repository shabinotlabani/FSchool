using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Coach)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class CoachMenusController(ApplicationDbContext db):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private CoachMenuService Service=>new(db);
    public async Task<IActionResult> Index(int studentId)
    {
        try{return View(await Service.Load(studentId,Actor));}catch(UnauthorizedAccessException){return NotFound();}
    }
    public async Task<IActionResult> Status(int studentId)
    {
        try{var page=await Service.Load(studentId,Actor);return Json(new{status=page.Proposal?.MenuStatus});}catch(UnauthorizedAccessException){return NotFound();}
    }
    [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting("nutrition-ai")]
    public async Task<IActionResult> Propose(int studentId,string? instructions)
    {
        try{await Service.Request(studentId,Actor,instructions);return RedirectToAction(nameof(Index),new{studentId});}
        catch(UnauthorizedAccessException){return NotFound();}catch(ValidationException ex){TempData["MenuError"]=ex.Message;return RedirectToAction(nameof(Index),new{studentId});}
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id,int studentId,bool reviewed)
    {
        try{await Service.Approve(id,Actor,reviewed);TempData["MenuSuccess"]="Menuja u aprovua dhe u publikua te prindi.";return RedirectToAction(nameof(Index),new{studentId});}
        catch(UnauthorizedAccessException){return NotFound();}catch(ValidationException ex){TempData["MenuError"]=ex.Message;return RedirectToAction(nameof(Index),new{studentId});}
    }
}
