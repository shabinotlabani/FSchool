using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Admin)]
public class UsersController(UserManager<ApplicationUser> users, UserAdministrationService administration, _2Korriku.Data.ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search, string? role)
    {
        ViewData["Search"] = search; ViewData["Role"] = role;
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            foreach(var term in search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                query = query.Where(u => EF.Functions.ILike(u.Email!, pattern) || EF.Functions.ILike((u.FirstName ?? "") + " " + (u.LastName ?? ""), pattern));
            }
        var list = new List<UserListModel>();
        foreach(var user in await query.OrderBy(u=>u.FirstName).ThenBy(u=>u.Email).ToListAsync())
        {
            var assigned = await users.GetRolesAsync(user);
            if (!string.IsNullOrEmpty(role) && !assigned.Contains(role)) continue;
            list.Add(new() { User = user, Role = string.Join(", ", assigned) });
        }
        return View(list);
    }
    [HttpGet] public async Task<IActionResult> Create(string? role) => View("Edit", new UserEditModel { Role = role == Roles.Coach ? Roles.Coach : Roles.Operator, Teams = await db.TrainingTeams.OrderBy(t=>t.Name).ToListAsync() });
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id); if (user == null) return NotFound();
        var roles = await users.GetRolesAsync(user);
        return View(new UserEditModel { Teams = await db.TrainingTeams.OrderBy(t=>t.Name).ToListAsync(), TeamIds = await db.CoachTeams.Where(x=>x.UserId==id).Select(x=>x.TrainingTeamId).ToListAsync(), Phone = user.PhoneNumber, Specialization = user.Specialization, Id = id, FirstName = user.FirstName ?? "", LastName = user.LastName ?? "", Email = user.Email ?? "", Role = Roles.All.FirstOrDefault(roles.Contains) ?? Roles.Viewer, IsActive = !UserAdministrationService.IsDisabled(user) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UserEditModel model)
    {
        if (ModelState.IsValid)
            try
            {
                await administration.SaveAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                TempData["Success"] = "Shfrytëzuesi u ruajt. Ndryshimet e llogarisë kërkojnë hyrje të re.";
                return model.Role == Roles.Coach ? RedirectToAction("Index", "Coaches") : RedirectToAction(nameof(Index));
            }
            catch(Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.Password = model.ConfirmPassword = null;
        ModelState.SetModelValue(nameof(model.Password), null, null);
        ModelState.SetModelValue(nameof(model.ConfirmPassword), null, null);
        model.Teams = await db.TrainingTeams.OrderBy(t=>t.Name).ToListAsync();
        return View("Edit", model);
    }
}
