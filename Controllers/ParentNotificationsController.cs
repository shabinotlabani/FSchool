using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Admin)]
public class ParentNotificationsController(ApplicationDbContext db):Controller
{
    private async Task<ParentAutomationInput> AutomationSettings(){
        var s=await db.ParentPushSettings.AsNoTracking().SingleOrDefaultAsync();
        return s==null?new():new(){TrainingReminders=s.TrainingReminders,ScheduleNotifications=s.ScheduleNotifications,PaymentReminders=s.PaymentReminders,PaymentDay=s.PaymentDay,PaymentHour=s.PaymentHour};
    }
    private async Task<ParentNotificationsView> Load(ParentNotificationInput? input=null)=>new(){
        Automation=await AutomationSettings(),
        Configured=await db.ParentPushSettings.AnyAsync(),
        Devices=await db.ParentPushSubscriptions.CountAsync(x=>x.Enabled&&x.Student.IsActive),
        Teams=await db.TrainingTeams.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync(),
        Students=await db.Students.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToListAsync(),
        Messages=await db.ParentPushMessages.AsNoTracking().Include(x=>x.Deliveries).OrderByDescending(x=>x.Id).Take(50).ToListAsync(),Input=input??new()};
    [HttpGet]public async Task<IActionResult> Index()=>View(await Load());
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Automation([Bind(Prefix="Automation")]ParentAutomationInput input)
    {
        if(ModelState.IsValid){
            await using var tx=await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2020)");
            var settings=await db.ParentPushSettings.SingleOrDefaultAsync();
            if(settings==null)return BadRequest();
            settings.TrainingReminders=input.TrainingReminders;settings.ScheduleNotifications=input.ScheduleNotifications;
            settings.PaymentReminders=input.PaymentReminders;settings.PaymentDay=input.PaymentDay;settings.PaymentHour=input.PaymentHour;
            await db.SaveChangesAsync();await tx.CommitAsync();
            TempData["Success"]="Kujtesat automatike u ruajtën.";return RedirectToAction(nameof(Index));
        }
        var model=await Load();model.Automation=input;return View("Index",model);
    }
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Configure(string subject)
    {
        try{await new ParentPushService(db).Configure(subject??"");TempData["Success"]="Njoftimet u aktivizuan.";return RedirectToAction(nameof(Index));}
        catch(ValidationException e){ModelState.AddModelError("",e.Message);return View("Index",await Load());}
    }
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Send([Bind(Prefix="Input")]ParentNotificationInput input)
    {
        if(ModelState.IsValid)try{
            var count=await new ParentPushService(db).Queue(input,User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            TempData["Success"]=$"Njoftimi u vendos në radhë për {count} pajisje.";return RedirectToAction(nameof(Index));
        }catch(ValidationException e){ModelState.AddModelError("",e.Message);}
        return View("Index",await Load(input));
    }
}
