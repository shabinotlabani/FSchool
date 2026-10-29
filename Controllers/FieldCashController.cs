using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.FieldOperator)]
public class FieldCashController(ApplicationDbContext db) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private FieldCashService Service => new(db);

    public async Task<IActionResult> Index() => View(await Service.Load(Actor, User.IsInRole(Roles.Admin)));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.FieldOperator)]
    public async Task<IActionResult> Create()
    {
        try { await Service.Create(Actor); TempData["Success"] = "Dorëzimi u dërgua për konfirmim."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Confirm(int id)
    {
        try { await Service.Confirm(id, Actor); TempData["Success"] = "Pranimi i cash-it u konfirmua."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        try { await Service.Reject(id, Actor, reason); TempData["Success"] = "Dorëzimi u kthye për korrigjim."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
