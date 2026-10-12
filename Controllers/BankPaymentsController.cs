using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Write)]
public class BankPaymentsController(ApplicationDbContext db):Controller
{
    public async Task<IActionResult> Index(){ViewBag.CanReview=await new BankVerificationService(db).CanReview(User.FindFirstValue(ClaimTypes.NameIdentifier)!);return View(await db.PendingBankPayments.AsNoTracking().Include(x=>x.Student).OrderBy(x=>x.ReviewedAt!=null).ThenByDescending(x=>x.CreatedAt).ToListAsync());}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(BankReviewModel model,bool approve)
    {
        if(ModelState.IsValid)try{var id=await new BankVerificationService(db).Review(model,User.FindFirstValue(ClaimTypes.NameIdentifier)!,approve);TempData["Success"]=id.HasValue?"Veprimi bankar u konfirmua dhe u regjistrua.":"Kërkesa u refuzua. Gjendja financiare nuk ndryshoi.";return RedirectToAction(nameof(Index));}
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){TempData["Error"]=ex.Message;}
        else TempData["Error"]="Kontrolloni shumën, datën dhe arsyen.";
        return RedirectToAction(nameof(Index));
    }
}
