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
 [Authorize(Roles=Roles.Coach)]
 public async Task<IActionResult> Players(string? search)
 {
  return View(await new _2Korriku.Services.CoachPlayersService(db).Load(User.FindFirstValue(ClaimTypes.NameIdentifier)!,search));
 }
 public async Task<IActionResult> Index(string? search,string? coachRole)
 {
  if(User.IsInRole(Roles.Coach))
  {
   var id=User.FindFirstValue(ClaimTypes.NameIdentifier)!;
   var assigned=new _2Korriku.Services.CoachChatService(db).Assigned(id).Select(x=>x.Id);
   ViewData["UnreadMessages"]=await db.CoachMessages.CountAsync(x=>x.CoachId==id&&assigned.Contains(x.StudentId)&&x.FromParent&&x.ReadAt==null);
   if(coachRole==null)return View("RoleChoice");
   if(coachRole is not ("head" or "assistant"))return BadRequest();
   ViewData["CoachRole"]=coachRole;
   var teams=await db.TrainingTeams.AsNoTracking().Where(t=>t.IsActive && (coachRole=="head"?t.CoachId:t.AssistantCoachId)==id)
    .Include(t=>t.Coach).Include(t=>t.AssistantCoach).Include(t=>t.FootballField).Include(t=>t.Sessions).OrderBy(t=>t.Name).ToListAsync();
   return View("Portal",_2Korriku.Services.CoachSchedule.Order(teams,DateTime.UtcNow));
  }
  ViewData["Search"]=search;
  var coaches=(await users.GetUsersInRoleAsync(Roles.Coach)).OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToList();
  if(!string.IsNullOrWhiteSpace(search)) coaches=coaches.Where(x=>($"{x.FirstName} {x.LastName} {x.Email} {x.PhoneNumber}").Contains(search.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
  var ids=coaches.Select(x=>x.Id).ToList();
  return View(new CoachDirectoryModel { Coaches=coaches, Assignments=await db.CoachTeams.AsNoTracking().Include(x=>x.TrainingTeam).Where(x=>ids.Contains(x.UserId)).ToListAsync() });
 }
 [Authorize(Roles=Roles.Coach)]
 public async Task<IActionResult> Personal()
 {
  var id=User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  var today=_2Korriku.Services.BillingClock.Today;
  var list=await db.PersonalTrainings.AsNoTracking().Include(x=>x.Student).Include(x=>x.Sessions)
   .Where(x=>x.Student!.IsActive&&x.CoachId==id&&!x.IsCancelled&&(x.Mode=="Session"?x.StartsOn>=today:!x.EndsOn.HasValue||x.EndsOn>=today))
   .OrderBy(x=>x.StartsOn).ThenBy(x=>x.Student!.FirstName).ToListAsync();
  return View(list);
 }
 public async Task<IActionResult> Team(int id,string? search)
 {
  if(User.IsInRole(Roles.Coach) && !await db.CoachTeams.AnyAsync(x=>x.UserId==User.FindFirstValue(ClaimTypes.NameIdentifier)&&x.TrainingTeamId==id)) return NotFound();
  var team=await db.TrainingTeams.AsNoTracking().Include(t=>t.Coach).Include(t=>t.AssistantCoach).Include(t=>t.FootballField).Include(t=>t.Sessions).Include(t=>t.Students.Where(s=>s.IsActive)).SingleOrDefaultAsync(t=>t.Id==id);
  if(team==null)return NotFound();
  ViewData["Search"]=search;
  if(!string.IsNullOrWhiteSpace(search))team.Students=team.Students.Where(s=>s.FullName.Contains(search.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
  return View(team);
 }
}
