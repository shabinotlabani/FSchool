using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Admin)]
public class CareStaffController(ApplicationDbContext db):Controller
{
    private IQueryable<ApplicationUser> Staff(string? kind=null)=>db.Users.Where(u=>db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&(kind==null?(r.Name==Roles.Doctor||r.Name==Roles.Nutritionist):r.Name==kind))));
    public async Task<IActionResult> Index(){
        ViewData["PendingRouting"]=await db.CareReferrals.AsNoTracking().Where(x=>x.ClosedAt==null&&x.Student.IsActive&&!db.CareAssignments.Any(a=>a.StudentId==x.StudentId&&a.Kind==x.Kind&&a.Active))
            .Select(x=>new CareRoutingRow(x.StudentId,x.Student.FirstName+" "+x.Student.LastName,x.Kind)).Distinct().ToListAsync();
        return View(await (from user in db.Users.AsNoTracking() join ur in db.UserRoles on user.Id equals ur.UserId join role in db.Roles on ur.RoleId equals role.Id
        where role.Name==Roles.Doctor||role.Name==Roles.Nutritionist orderby user.FirstName
        select new CareStaffRow(user,role.Name!,db.CareAssignments.Count(x=>x.StaffId==user.Id&&x.Active&&x.Kind==role.Name))).ToListAsync());}
    public async Task<IActionResult> Assign(string id)
    {
        var user=await Staff().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(user==null)return NotFound();
        string kind;try{kind=await new PlayerCareService(db).Kind(id);}catch(UnauthorizedAccessException){return NotFound();}
        ViewData["Staff"]=user;ViewData["Kind"]=kind;
        ViewData["Selected"]=await db.CareAssignments.Where(x=>x.StaffId==id&&x.Kind==kind&&x.Active).Select(x=>x.StudentId).ToListAsync();
        return View(await db.Students.AsNoTracking().Where(x=>x.IsActive).Include(x=>x.TrainingTeam).OrderBy(x=>x.FirstName).ThenBy(x=>x.LastName).ToListAsync());
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(string id,List<int> studentIds)
    {
        if(!await Staff().AnyAsync(x=>x.Id==id))return NotFound();
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        string kind;try{kind=await new PlayerCareService(db).Kind(id);}catch(UnauthorizedAccessException){return NotFound();}
        var ids=studentIds.Distinct().ToList();
        if(await db.Students.CountAsync(x=>ids.Contains(x.Id)&&x.IsActive)!=ids.Count)return BadRequest();
        var rows=await db.CareAssignments.Where(x=>x.StaffId==id&&x.Kind==kind).ToListAsync();
        foreach(var row in rows){row.Active=ids.Contains(row.StudentId);row.UpdatedAt=DateTime.UtcNow;row.AssignedBy=User.FindFirstValue(ClaimTypes.NameIdentifier)!;}
        foreach(var studentId in ids.Where(i=>!rows.Any(r=>r.StudentId==i)))db.CareAssignments.Add(new(){StaffId=id,StudentId=studentId,Kind=kind,UpdatedAt=DateTime.UtcNow,AssignedBy=User.FindFirstValue(ClaimTypes.NameIdentifier)!});
        await db.SaveChangesAsync();await tx.CommitAsync();TempData["Success"]="Lojtarët u caktuan.";return RedirectToAction(nameof(Index));
    }
}
