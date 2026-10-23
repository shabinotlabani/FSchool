using System.Security.Cryptography;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Write)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ParentLinksController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        var model=await new ParentAccessService(db).LinkFor(id);
        return model == null ? NotFound() : View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Qr(int id, bool download = false)
    {
        var token = (await new ParentAccessService(db).LinkFor(id))?.Token;
        if (string.IsNullOrEmpty(token)) return NotFound();
        var link = Url.Action("Index", "ParentPortal", new { token }, Request.Scheme);
        if (link == null) return NotFound();
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(12);
        return download ? File(bytes, "image/png", $"Prindi-{id}.png") : File(bytes, "image/png");
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, bool revoke = false)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        var model=await new ParentAccessService(db).LinkFor(id);
        if(model==null)return NotFound();
        var token=revoke?null:Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        if(model.Family!=null){
            var family=await db.PlayerFamilies.SingleAsync(x=>x.Id==model.Family.Id);family.ParentLinkToken=token;
        }else{
            var student=await db.Students.SingleAsync(x=>x.Id==id);student.ParentLinkToken=token;
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return RedirectToAction(nameof(Index), new { id });
    }
}
