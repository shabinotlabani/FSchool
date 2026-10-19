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
    public async Task<IActionResult> Index()
    {
        await using var snapshot=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var service=new BankVerificationService(db);
        ViewBag.CanReview=await service.CanReview(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rows=await db.PendingBankPayments.AsNoTracking().Include(x=>x.Student).OrderBy(x=>x.ReviewedAt!=null).ThenByDescending(x=>x.CreatedAt).ToListAsync();
        var balances=new Dictionary<int,BankClientBalance>();var errors=new Dictionary<int,string>();
        foreach(var row in rows.Where(x=>x.ReviewedAt==null&&BankVerificationService.SupportsDistribution(x.Kind))){
            try{balances[row.Id]=await service.ClientBalance(row);}catch(InvalidOperationException e){errors[row.Id]=e.Message;}
        }
        ViewData["ClientBalances"]=balances;ViewData["BalanceErrors"]=errors;
        var proposals=new Dictionary<int,decimal>();
        foreach(var row in rows.Where(x=>x.ReviewedAt==null&&x.Kind=="Field")){
            var payment=System.Text.Json.JsonSerializer.Deserialize<FieldPaymentModel>(row.Payload)!;
            if(payment.WholeSeries)proposals[row.Id]=(await new FieldService(db).SeriesBookingsAsync(payment.BookingId)).Sum(x=>x.Due);
            else{var booking=await db.FieldBookings.AsNoTracking().Include(x=>x.Payments).SingleOrDefaultAsync(x=>x.Id==payment.BookingId);if(booking!=null)proposals[row.Id]=booking.Due;}
        }
        ViewData["ProposedAmounts"]=proposals;
        ViewData["AllocatedRequests"]=(await db.BankPaymentAllocations.Select(x=>x.BankPaymentId).Distinct().ToListAsync()).ToHashSet();
        return View(rows);
    }
    public async Task<IActionResult> Receipt(int id)
    {
        var request=await db.PendingBankPayments.AsNoTracking().Include(x=>x.Student).SingleOrDefaultAsync(x=>x.Id==id);
        if(request==null)return NotFound();
        var rows=await db.BankPaymentAllocations.AsNoTracking().Include(x=>x.Payment).ThenInclude(x=>x.Student).Where(x=>x.BankPaymentId==id).OrderBy(x=>x.Id).ToListAsync();
        if(rows.Count==0)return NotFound();
        return View(new BankReceiptView{Request=request,Allocations=rows});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(BankReviewModel model,bool approve)
    {
        if(approve&&!model.ReceivedAmount.HasValue)
            ModelState.AddModelError(nameof(model.ReceivedAmount),"Shkruani shumën e pranuar realisht në bankë.");
        if(ModelState.IsValid)try{var id=await new BankVerificationService(db).Review(model,User.FindFirstValue(ClaimTypes.NameIdentifier)!,approve);TempData["Success"]=id.HasValue?"Veprimi bankar u konfirmua dhe u regjistrua.":"Kërkesa u refuzua. Gjendja financiare nuk ndryshoi.";return RedirectToAction(nameof(Index));}
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){TempData["Error"]=ex.Message;}
        else TempData["Error"]=ModelState[nameof(model.ReceivedAmount)]?.Errors.FirstOrDefault()?.ErrorMessage??"Kontrolloni shumën, datën dhe arsyen.";
        return RedirectToAction(nameof(Index));
    }
}
