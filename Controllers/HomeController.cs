using _2Korriku.Data;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        if (User.IsInRole(_2Korriku.Models.Roles.Coach)) return RedirectToAction("Index", "Coaches");
        var stats = await _dashboardService.GetStatsAsync();
        var latestStudents = _context.Students
            .OrderByDescending(x => x.RegistrationDate)
            .Take(5)
            .ToList();

        var model = new DashboardViewModel
        {
            Stats = stats,
            RecentStudents = latestStudents
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
}
