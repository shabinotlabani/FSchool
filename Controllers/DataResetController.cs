using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DataResetController(ApplicationDbContext db, UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, ILogger<DataResetController> logger) : Controller
{
    private async Task<ApplicationUser?> OwnerAsync()
    {
        var user = await users.GetUserAsync(User);
        return user != null && DataResetService.IsOwner(user.UserName) ? user : null;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => await OwnerAsync() == null ? Forbid() : View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string? confirmation, string? password)
    {
        var user = await OwnerAsync();
        if (user == null) return Forbid();
        if (confirmation != "ZERO") ModelState.AddModelError("", "Shkruani ZERO për të konfirmuar fshirjen.");
        if (!ModelState.IsValid) return View();
        var check = await signIn.CheckPasswordSignInAsync(user, password ?? "", lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            ModelState.AddModelError("", check.IsLockedOut ? "Llogaria është bllokuar përkohësisht. Provoni më vonë." : "Fjalëkalimi nuk është i saktë.");
            return View();
        }
        try { await DataResetService.ResetAsync(db); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data reset failed for {UserId}", user.Id);
            ModelState.AddModelError("", "Zerimi nuk u krye. Të dhënat u ruajtën; provoni përsëri.");
            return View();
        }
        logger.LogWarning("Operational data reset by {UserId}", user.Id);
        TempData["ResetSuccess"] = "Të dhënat u zeruan. Konfigurimet dhe llogaritë u ruajtën.";
        return RedirectToAction(nameof(Index));
    }
}
