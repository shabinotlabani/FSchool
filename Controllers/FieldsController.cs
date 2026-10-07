using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Read)]
public class FieldsController(ApplicationDbContext db) : Controller
{
    private FieldService Service => new(db);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(DateOnly? from, int? fieldId, bool includeCancelled = false)
    {
        var date = from ?? BillingClock.Today;
        if (!ModelState.IsValid || date.Year < 2000 || date.Year > 2099 || fieldId <= 0) return BadRequest();
        return View(await Service.ScheduleAsync(date, fieldId, includeCancelled));
    }
    public async Task<IActionResult> Manage() => View(await db.FootballFields.AsNoTracking().OrderBy(f => f.Name).ToListAsync());

    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0) return View(new FieldEditModel());
        var f = await db.FootballFields.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id);
        if (f == null) return NotFound();
        return View(new FieldEditModel { Id = id, Revision = f.Revision, Name = f.Name, Location = f.Location, Notes = f.Notes, IsActive = f.IsActive });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(FieldEditModel model)
    {
        if (ModelState.IsValid) try
        {
            await Service.SaveFieldAsync(model); TempData["Success"] = "Fusha u ruajt."; return RedirectToAction(nameof(Manage));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        return View(model);
    }
    private async Task Choices(FieldBookingEditModel model) => model.Fields = await db.FootballFields.AsNoTracking()
        .Where(f => f.IsActive || f.Id == model.FootballFieldId).OrderBy(f => f.Name).ToListAsync();

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Booking(int id = 0, int? fieldId = null, DateOnly? date = null)
    {
        if (!ModelState.IsValid) return BadRequest();
        var model = new FieldBookingEditModel { FootballFieldId = fieldId ?? 0, Date = date ?? BillingClock.Today };
        if (id != 0)
        {
            var b = await db.FieldBookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == id);
            if (b == null) return NotFound();
            if (b.IsCancelled) return RedirectToAction(nameof(Details), new { id });
            model = new FieldBookingEditModel { Id = id, Price = b.Price, RequestId = b.RequestId, Revision = b.Revision, FootballFieldId = b.FootballFieldId,
                Date = b.Date, StartsAt = b.StartsAt, EndsAt = b.EndsAt, CustomerName = b.CustomerName, Phone = b.Phone, Notes = b.Notes };
        }
        await Choices(model); return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Booking(FieldBookingEditModel model)
    {
        if (ModelState.IsValid) try
        {
            var id = await Service.SaveBookingAsync(model, Actor); TempData["Success"] = model.Weeks > 1 ? $"U rezervuan {model.Weeks} termine javore. Çmimi dhe pagesat mbahen veçmas për çdo termin." : "Termini privat u ruajt.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        await Choices(model); return View(model);
    }
    private Task<FieldBooking?> GetBooking(int id) => db.FieldBookings.AsNoTracking().Include(b => b.Payments).Include(b => b.FootballField)
        .Include(b => b.Changes).ThenInclude(c => c.Actor).SingleOrDefaultAsync(b => b.Id == id);
    public async Task<IActionResult> Details(int id)
    {
        var booking = await GetBooking(id);
        if (booking == null) return NotFound();
        if (booking.SeriesId.HasValue)
            ViewData["SeriesBookings"] = await db.FieldBookings.AsNoTracking().Include(b => b.FootballField).Include(b => b.Payments)
                .Where(b => b.SeriesId == booking.SeriesId).OrderBy(b => b.Date).ThenBy(b => b.StartsAt).ToListAsync();
        return View(booking);
    }
    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Pay(int id)
    {
        var booking = await GetBooking(id);
        if (booking == null) return NotFound();
        if (booking.IsCancelled || booking.Due <= 0) return RedirectToAction(nameof(Details), new { id });
        return View(new FieldPaymentModel { BookingId = id, Revision = booking.Revision, Amount = booking.Due, Booking = booking });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Pay(FieldPaymentModel model)
    {
        if (ModelState.IsValid) try
        {
            var id = await Service.PayAsync(model, Actor); return RedirectToAction(nameof(Receipt), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.Booking = await GetBooking(model.BookingId); return model.Booking == null ? NotFound() : View(model);
    }
    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await db.FieldPayments.AsNoTracking().Include(p => p.Actor).SingleOrDefaultAsync(p => p.Id == id);
        return payment == null ? NotFound() : View(payment);
    }
    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> CancelPayment(int id)
    {
        var payment = await db.FieldPayments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        return payment == null ? NotFound() : View(new FieldPaymentCancelModel { Id = id, Payment = payment });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> CancelPayment(FieldPaymentCancelModel model)
    {
        if (ModelState.IsValid) try
        {
            var id = await Service.CancelPaymentAsync(model, Actor); TempData["Success"] = "Pagesa u anulua. Raporti financiar u përditësua.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.Payment = await db.FieldPayments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == model.Id);
        return model.Payment == null ? NotFound() : View(model);
    }
    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await GetBooking(id);
        if (booking == null) return NotFound();
        if (booking.IsCancelled) return RedirectToAction(nameof(Details), new { id });
        return View(new FieldBookingCancelModel { Id = id, Revision = booking.Revision, Booking = booking });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Cancel(FieldBookingCancelModel model)
    {
        if (ModelState.IsValid) try
        {
            await Service.CancelAsync(model, Actor); TempData["Success"] = "Termini u anulua dhe orari u lirua.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.Booking = await GetBooking(model.Id); return model.Booking == null ? NotFound() : View(model);
    }
}
