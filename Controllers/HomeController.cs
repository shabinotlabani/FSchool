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
    public async Task<IActionResult> Index(int? teamId,int? year)
    {
        if(User.IsInRole(_2Korriku.Models.Roles.Admin)||User.IsInRole(_2Korriku.Models.Roles.SuperAdmin)){
            var today=BillingClock.Today;
            var weekStart=today.AddDays(-(((int)today.DayOfWeek+6)%7));
            var weekEnd=weekStart.AddDays(7);
            var teams=await _context.TrainingTeams.AsNoTracking().Include(t=>t.Coach).OrderBy(t=>t.Name).ToListAsync();
            if(teamId.HasValue&&!teams.Any(t=>t.Id==teamId))return NotFound();
            var matches=await _context.MatchGames.AsNoTracking().Include(g=>g.TrainingTeam)
                .Where(g=>g.Date>=weekStart&&g.Date<weekEnd&&(!teamId.HasValue||g.TrainingTeamId==teamId))
                .OrderBy(g=>g.Date).ThenBy(g=>g.Kickoff).ToListAsync();
            return View("Matches",new _2Korriku.Models.MatchList{Teams=teams,Matches=matches,TeamId=teamId,Year=today.Year});
        }
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

    [Authorize(Roles=_2Korriku.Models.Roles.Admin+","+_2Korriku.Models.Roles.SuperAdmin)]
    [ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> MatchReport(int id)
    {
        var game=await _context.MatchGames.AsNoTracking().Include(g=>g.TrainingTeam).Include(g=>g.Players).SingleOrDefaultAsync(g=>g.Id==id);
        if(game==null)return NotFound();
        ViewData["ReadOnlyMatch"]=true;
        return View("~/Views/Matches/Summary.cshtml",game);
    }
}

public class DashboardViewModel
{
    public DashboardStats Stats { get; set; } = new();
    public List<_2Korriku.Models.Student> RecentStudents { get; set; } = new();
    public List<_2Korriku.Models.Student> StudentsWithoutTeam { get; set; } = new();
}
