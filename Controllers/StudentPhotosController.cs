using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.Write+","+Roles.Coach),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class StudentPhotosController(ApplicationDbContext db):Controller
{
 async Task<Student?> Player(int id){if(User.IsInRole(Roles.Admin)||User.IsInRole(Roles.Operator))return await db.Students.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==id);try{return await new PlayerDevelopmentService(db).Player(id,User.FindFirstValue(ClaimTypes.NameIdentifier)!);}catch(UnauthorizedAccessException){return null;}}
 public async Task<IActionResult> Edit(int id){var player=await Player(id);if(player==null)return NotFound();ViewData["HasPhoto"]=await db.Set<StudentPhoto>().AnyAsync(p=>p.StudentId==id);return View(player);}
 public async Task<IActionResult> Image(int id){if(await Player(id)==null)return NotFound();var bytes=await db.Set<StudentPhoto>().Where(p=>p.StudentId==id).Select(p=>p.Data).SingleOrDefaultAsync();Response.Headers["X-Content-Type-Options"]="nosniff";return bytes==null?NotFound():File(bytes,"image/webp");}
 [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(262144)]
 public async Task<IActionResult> Save(int id,IFormFile? photo,bool remove=false){if(await Player(id)==null)return NotFound();try{await new StudentPhotoService(db).Save(id,photo,remove);return Json(new{success=true});}catch(ValidationException ex){return BadRequest(new{message=ex.Message});}}
}
