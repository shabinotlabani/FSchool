using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Controllers;

[AllowAnonymous,Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parent-portal")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None),ParentModule("Equipment")]
public class ParentEquipmentController(ApplicationDbContext db):Controller
{
    [HttpGet("p/{token}/equipment")]
    public async Task<IActionResult> Index(string token,int studentId,bool panel=false)
    {
        var access=await new ParentAccessService(db).Resolve(token);
        var child=access?.Students.SingleOrDefault(s=>s.Id==studentId);if(child==null)return NotFound();
        var reserved=await EquipmentOrderService.Reserved(db);
        var products=await db.ProductVariants.AsNoTracking().Where(v=>v.IsActive&&v.Product!.IsActive&&v.Product.SalePrice>0)
            .OrderBy(v=>v.Product!.Name).ThenBy(v=>v.Id)
            .Select(v=>new EquipmentOption(v.Id,v.ProductId,v.Product!.Name,v.Size,v.Product.UnitName,v.Product.SalePrice,v.CurrentStock,v.Product.Photo!=null)).ToListAsync();
        products=products.Select(p=>p with{Available=Math.Max(0,p.Available-reserved.GetValueOrDefault(p.Id))}).Where(p=>p.Available>1).ToList();
        var orders=await db.Sales.AsNoTracking().Include(s=>s.Details).Where(s=>s.StudentId==child.Id&&s.IsParentOrder).OrderByDescending(s=>s.Id).ToListAsync();
        var model=new ParentEquipment(token,child.Id,child.FullName,products,orders);
        ViewData["EquipmentPanel"]=panel;
        return panel?PartialView("_Panel",model):View(model);
    }
    [HttpPost("p/{token}/equipment/order"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Order(string token,EquipmentOrderInput input,bool panel=false)
    {
        var access=await new ParentAccessService(db).Resolve(token);
        if(access==null||!access.Students.Any(s=>s.Id==input.StudentId))return NotFound();
        try{
            if(!ModelState.IsValid)throw new ValidationException("Kontrolloni madhësinë dhe sasinë e porosisë.");
            await new EquipmentOrderService(db).Order(token,input);
            TempData["Success"]="Faleminderit për porosinë! Rekuizita u rezervua dhe vlera u shtua si borxh. Klubi do ta konfirmojë dorëzimin.";
        }catch(Exception e)when(e is InvalidOperationException or ValidationException){TempData["Error"]=e.Message;}
        return RedirectToAction(nameof(Index),new{token,studentId=input.StudentId,panel});
    }
    [HttpGet("p/{token}/equipment/{id:int}/photo")]
    public async Task<IActionResult> Photo(string token,int id)
    {
        if(await new ParentAccessService(db).Resolve(token)==null)return NotFound();
        var bytes=await db.Products.Where(p=>p.Id==id&&p.IsActive).Select(p=>p.Photo).SingleOrDefaultAsync();
        Response.Headers["X-Content-Type-Options"]="nosniff";
        return bytes==null?NotFound():File(bytes,"image/webp");
    }
}
