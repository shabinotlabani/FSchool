using _2Korriku.Data;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

public class HomeController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly ApplicationDbContext _context;

    public HomeController(DashboardService dashboardService, ApplicationDbContext context)
    {
        _dashboardService = dashboardService;
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        if(User.IsInRole(_2Korriku.Models.Roles.Doctor)||User.IsInRole(_2Korriku.Models.Roles.Nutritionist))return RedirectToAction("Index","PlayerCare");
        if (User.IsInRole(_2Korriku.Models.Roles.FieldOperator)) return RedirectToAction("Index", "Fields");
        if (User.IsInRole(_2Korriku.Models.Roles.Coach)) return RedirectToAction("Index", "Coaches");
        var stats = await _dashboardService.GetStatsAsync();
        var latestStudents = _context.Students
            .OrderByDescending(x => x.RegistrationDate)
            .Take(5)
            .ToList();

        var model = new DashboardViewModel
        {
            Stats = stats,
            RecentStudents = latestStudents,
            StudentsWithoutTeam = await _context.Students.AsNoTracking().Where(s => s.TrainingTeamId == null)
                .OrderByDescending(s => s.IsActive).ThenBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync()
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}

public class DashboardViewModel
{
    public DashboardStats Stats { get; set; } = new();
    public List<_2Korriku.Models.Student> RecentStudents { get; set; } = new();
    public List<_2Korriku.Models.Student> StudentsWithoutTeam { get; set; } = new();
}
