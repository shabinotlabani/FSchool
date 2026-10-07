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
public class TreasuryController(ApplicationDbContext db) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task<List<MoneyMethodBalance>> Balances() => (await new ExpenseService(db).ReportAsync(new() { From = BillingClock.Today, To = BillingClock.Today })).MethodBalances;
    public async Task<IActionResult> Index(MoneyDateFilter filter)
    {
        var model = new TreasuryIndexModel { Filter = filter, Balances = await Balances(),
            Openings = await db.TreasuryEntries.AsNoTracking().Where(x => x.Kind == TreasuryEntry.Opening && x.CancelledAt == null).ToListAsync() };
        if (ModelState.IsValid) model.Entries = await db.TreasuryEntries.AsNoTracking()
            .Where(x => (x.Date >= filter.From && x.Date <= filter.To) || (x.CancelledAt.HasValue && x.CancelledAt >= BillingClock.UtcDate(filter.From!.Value) && x.CancelledAt < BillingClock.UtcDate(filter.To!.Value.AddDays(1))))
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToListAsync();
        return View(model);
    }
    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(string kind = TreasuryEntry.Opening, string method = "Cash")
    {
        if (kind != TreasuryEntry.Opening && kind != TreasuryEntry.Withdrawal) return BadRequest();
        return View(new TreasuryCreateModel { Kind = kind, Method = method == "Bank" ? "Bank" : "Cash", Balances = await Balances() });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(TreasuryCreateModel model)
    {
        if (ModelState.IsValid) try
        {
            var id = await new TreasuryService(db).CreateAsync(model, Actor);
            TempData["Success"] = "Regjistrimi u ruajt.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.Balances = await Balances();
        return View(model);
    }
    private Task<TreasuryEntry?> Find(int id) => db.TreasuryEntries.AsNoTracking().Include(x => x.CreatedBy).Include(x => x.CancelledBy).SingleOrDefaultAsync(x => x.Id == id);
    public async Task<IActionResult> Details(int id) { var entry = await Find(id); return entry == null ? NotFound() : View(entry); }
    public async Task<IActionResult> Print(int id) { var entry = await Find(id); return entry == null ? NotFound() : View(entry); }
    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Cancel(int id) { var entry = await Find(id); return entry == null ? NotFound() : View(new TreasuryCancelModel { Id = id, Entry = entry }); }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Cancel(TreasuryCancelModel model)
    {
        model.Entry = await Find(model.Id);
        if (model.Entry == null) return NotFound();
        if (ModelState.IsValid) try
        {
            await new TreasuryService(db).CancelAsync(model, Actor);
            TempData["Success"] = "Regjistrimi u anulua.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        return View(model);
    }
}
