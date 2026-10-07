using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Read+","+Roles.Coach)]
public class CoachesController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
 public async Task<IActionResult> Index(string? search)
 {
  if(User.IsInRole(Roles.Coach))
  {
   var id=User.FindFirstValue(ClaimTypes.NameIdentifier)!;
   var teams=await db.CoachTeams.AsNoTracking().Where(x=>x.UserId==id)
    .Select(x=>x.TrainingTeam!).Include(t=>t.FootballField).Include(t=>t.Sessions).OrderBy(t=>t.Name).ToListAsync();
   return View("Portal",teams);
  }
  ViewData["Search"]=search;
  var coaches=(await users.GetUsersInRoleAsync(Roles.Coach)).OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToList();
  if(!string.IsNullOrWhiteSpace(search)) coaches=coaches.Where(x=>($"{x.FirstName} {x.LastName} {x.Email} {x.PhoneNumber}").Contains(search.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
  var ids=coaches.Select(x=>x.Id).ToList();
  return View(new CoachDirectoryModel { Coaches=coaches, Assignments=await db.CoachTeams.AsNoTracking().Include(x=>x.TrainingTeam).Where(x=>ids.Contains(x.UserId)).ToListAsync() });
 }
 public async Task<IActionResult> Team(int id,string? search)
 {
  if(User.IsInRole(Roles.Coach) && !await db.CoachTeams.AnyAsync(x=>x.UserId==User.FindFirstValue(ClaimTypes.NameIdentifier)&&x.TrainingTeamId==id)) return NotFound();
  var team=await db.TrainingTeams.AsNoTracking().Include(t=>t.FootballField).Include(t=>t.Sessions).Include(t=>t.Students.Where(s=>s.IsActive)).SingleOrDefaultAsync(t=>t.Id==id);
  if(team==null)return NotFound();
  ViewData["Search"]=search;
  if(!string.IsNullOrWhiteSpace(search))team.Students=team.Students.Where(s=>s.FullName.Contains(search.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
  return View(team);
 }
}
