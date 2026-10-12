using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Admin)]
public class PresentationController(ApplicationDbContext db,IServiceProvider services):Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()=>View(await db.DemoImports.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==PresentationDemo.ImportId));
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Import()
    {
        await PresentationDemo.SeedAsync(services);
        return RedirectToAction(nameof(Index));
    }
}
