using _2Korriku.Data;
using _2Korriku.Services;
using _2Korriku.Models;
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
            if(User.IsInRole(Roles.SuperAdmin))ViewData["ExpiringContracts"]=await _context.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Where(c=>c.Status!="Draft"&&(c.EndedOn??c.EndsOn)>=today&&(c.EndedOn??c.EndsOn)<=today.AddDays(30)).OrderBy(c=>c.EndedOn??c.EndsOn).ToListAsync();
            var weekStart=today.AddDays(-(((int)today.DayOfWeek+6)%7));
            var weekEnd=weekStart.AddDays(7);
            var teams=await _context.TrainingTeams.AsNoTracking().Include(t=>t.Coach).OrderBy(t=>t.Name).ToListAsync();
            if(teamId.HasValue&&!teams.Any(t=>t.Id==teamId))return NotFound();
            var matches=await _context.MatchGames.AsNoTracking().Include(g=>g.TrainingTeam).ThenInclude(t=>t.Coach).Include(g=>g.Players)
                .Where(g=>g.Date>=weekStart&&g.Date<weekEnd&&(!teamId.HasValue||g.TrainingTeamId==teamId))
                .OrderBy(g=>g.Date).ThenBy(g=>g.Kickoff).ToListAsync();
            var students=await _context.Students.AsNoTracking().ToListAsync();
            var studentIds=students.Select(s=>s.Id).ToList();
            var changes=await _context.StudentChanges.AsNoTracking().Where(c=>studentIds.Contains(c.StudentId)).OrderBy(c=>c.CreatedAt).ThenBy(c=>c.Id).ToListAsync();
            var contracts=await _context.PlayerContracts.AsNoTracking().Where(c=>c.StudentId.HasValue&&c.Status!="Draft").ToListAsync();
            var monthNames=new[]{"Jan","Shk","Mar","Pri","Maj","Qer","Kor","Gus","Sht","Tet","Nën","Dhj"};
            var monthly=new List<PlayerMonthlyCount>();
            var lastMonth=today.Month;
            for(var month=1;month<=lastMonth;month++){
                var monthStart=new DateOnly(today.Year,month,1);var monthEnd=monthStart.AddMonths(1);
                var endUtc=BillingClock.UtcDate(monthEnd);
                var count=students.Count(student=>BillingClock.LocalDate(student.RegistrationDate)<monthEnd
                    &&ActiveAt(student,changes,endUtc)
                    &&!contracts.Any(c=>c.StudentId==student.Id&&c.StartsOn<monthEnd&&(c.EndedOn??c.EndsOn)>=monthStart));
                monthly.Add(new(month,monthNames[month-1],count));
            }
            return View("Matches",new _2Korriku.Models.MatchList{Teams=teams,Matches=matches,TeamId=teamId,Year=today.Year,ActivePlayersByMonth=monthly});
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

    private static bool ActiveAt(Student student,List<StudentChange> changes,DateTime endUtc)
    {
        var snapshot=changes.LastOrDefault(c=>c.StudentId==student.Id&&c.CreatedAt<endUtc)?.After;
        if(string.IsNullOrWhiteSpace(snapshot))return true;
        try{using var json=System.Text.Json.JsonDocument.Parse(snapshot);return json.RootElement.TryGetProperty("IsActive",out var active)&&active.GetBoolean();}
        catch(System.Text.Json.JsonException){return student.IsActive;}
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

    [Authorize(Roles=Roles.Admin+","+Roles.SuperAdmin)]
    [ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> MatchPlayer(int matchId,int studentId,int? leagueId)
    {
        var current=await _context.MatchPlayers.AsNoTracking().Include(p=>p.MatchGame).ThenInclude(g=>g.TrainingTeam).SingleOrDefaultAsync(p=>p.MatchGameId==matchId&&p.StudentId==studentId);
        if(current==null)return NotFound();
        var history=await _context.MatchPlayers.AsNoTracking().Include(p=>p.MatchGame).Where(p=>p.StudentId==studentId&&(p.MatchGame.Status=="Completed"||(p.MatchGameId==matchId&&p.MatchGame.Status!="Cancelled"))).ToListAsync();
        var ids=history.Where(p=>p.MatchGame.LeagueId.HasValue).Select(p=>p.MatchGame.LeagueId!.Value).ToList();
        var leagues=await _context.Leagues.AsNoTracking().Where(l=>ids.Contains(l.Id)||l.Teams.Any(t=>t.TrainingTeamId==current.MatchGame.TrainingTeamId)).OrderByDescending(l=>l.StartsOn).ThenBy(l=>l.Name).ToListAsync();
        if(leagueId.HasValue&&!leagues.Any(l=>l.Id==leagueId))return NotFound();
        ViewData["Leagues"]=leagues;ViewData["LeagueId"]=leagueId;
        if(leagueId.HasValue)history=history.Where(p=>p.MatchGame.LeagueId==leagueId).ToList();
        return PartialView("_MatchPlayer",new AdminMatchPlayer(current,history));
    }

    [Authorize(Roles=Roles.Admin+","+Roles.SuperAdmin)]
    [ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> PlayerProgress(int matchId,int studentId)
    {
        if(!await _context.MatchPlayers.AnyAsync(p=>p.MatchGameId==matchId&&p.StudentId==studentId))return NotFound();
        var player=await _context.Students.AsNoTracking().Include(s=>s.TrainingTeam).SingleOrDefaultAsync(s=>s.Id==studentId);
        if(player==null)return NotFound();
        var minutes=await _context.MatchPlayers.AsNoTracking().Where(p=>p.StudentId==studentId&&(p.MatchGame.Status=="Completed"||(p.MatchGameId==matchId&&p.MatchGame.Status!="Cancelled"))&&p.Minutes>0).Select(p=>p.Minutes).ToListAsync();
        ViewData["MatchMinutes"]=new PlayerMatchMinutes(minutes.Sum(),minutes.Count);
        ViewData["AdminMatchId"]=matchId;
        ViewData["IncludesCurrentMatch"]=true;
        ViewData["MedicalHistory"]=await _context.CareEntries.AsNoTracking().Where(x=>x.Assignment.StudentId==studentId&&x.Assignment.Kind==Roles.Doctor&&x.Date<=BillingClock.Today).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).Select(x=>new CareNotice(studentId,x.TrainingStatus!,x.CoachInstructions,x.Date,x.ReviewDate)).ToListAsync();
        ViewData["ReportedCases"]=await _context.CareReferrals.AsNoTracking().Where(x=>x.StudentId==studentId&&x.Kind==Roles.Doctor).OrderByDescending(x=>x.CreatedAt).ToListAsync();
        var evaluations=await _context.PlayerEvaluations.AsNoTracking().Where(x=>x.StudentId==studentId).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        var records=await _context.DevelopmentRecords.AsNoTracking().Where(x=>x.StudentId==studentId).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        return View("~/Views/PlayerDevelopment/Progress.cshtml",new DevelopmentHistory(player,evaluations,""){Catalog=await new SportDevelopmentService(_context).Catalog(),Records=records,Section="Progress"});
    }
}

public class DashboardViewModel
{
    public DashboardStats Stats { get; set; } = new();
    public List<_2Korriku.Models.Student> RecentStudents { get; set; } = new();
    public List<_2Korriku.Models.Student> StudentsWithoutTeam { get; set; } = new();
}
