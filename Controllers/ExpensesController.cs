using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles=Roles.Read)]
public class ExpensesController(ApplicationDbContext db):Controller
{
    private ExpenseService Service=>new(db);
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(MoneyDateFilter filter,string? search,int? categoryId,bool includeCancelled=false)
    {
        var m=new ExpenseIndexModel{Filter=filter,Search=search,CategoryId=categoryId,IncludeCancelled=includeCancelled,Categories=await db.ExpenseCategories.AsNoTracking().Where(x=>x.ParentId==null).OrderBy(x=>x.Name).ToListAsync()};
        if(ModelState.IsValid){
            var query=db.Expenses.AsNoTracking().Where(x=>x.Date>=filter.From&&x.Date<=filter.To&&(!categoryId.HasValue||x.CategoryId==categoryId)&&(includeCancelled||x.CancelledAt==null));
            if(!string.IsNullOrWhiteSpace(search))foreach(var term in search.Split(' ',StringSplitOptions.RemoveEmptyEntries)){
                var value=term.ToLower();query=query.Where(x=>x.Description.ToLower().Contains(value)||(x.Recipient!=null&&x.Recipient.ToLower().Contains(value))||x.CategoryName.ToLower().Contains(value)||(x.SubcategoryName!=null&&x.SubcategoryName.ToLower().Contains(value))||(x.DocumentNumber!=null&&x.DocumentNumber.ToLower().Contains(value)));
            }
            m.Expenses=await query.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).ToListAsync();
        }
        return View(m);
    }
    [Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Categories()=>View(await db.ExpenseCategories.AsNoTracking().OrderBy(x=>x.Name).ToListAsync());
    private async Task CategoryChoices(ExpenseCategoryModel m){
        m.Parents=await db.ExpenseCategories.AsNoTracking().Where(x=>x.ParentId==null&&(x.IsActive||x.Id==m.ParentId)&&x.Id!=m.Id).OrderBy(x=>x.Name).ToListAsync();
        m.History=await db.ExpenseCategoryChanges.AsNoTracking().Include(x=>x.Actor).Where(x=>x.ExpenseCategoryId==m.Id).OrderByDescending(x=>x.Id).ToListAsync();
    }
    [HttpGet,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Category(int id=0,int? parentId=null){
        var m=new ExpenseCategoryModel{ParentId=parentId};
        if(id!=0){var c=await db.ExpenseCategories.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();m=new(){Id=c.Id,Revision=c.Revision,Name=c.Name,ParentId=c.ParentId,IsActive=c.IsActive};}
        await CategoryChoices(m);return View(m);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Category(ExpenseCategoryModel m){
        if(ModelState.IsValid)try{await Service.SaveCategoryAsync(m,Actor);TempData["Success"]="Kategoria u ruajt. Shpenzimet e mëparshme ruajnë emërtimin e tyre.";return RedirectToAction(nameof(Categories));}catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        await CategoryChoices(m);return View(m);
    }
    private async Task Choices(ExpenseCreateModel m){
        m.Categories=await db.ExpenseCategories.AsNoTracking().Where(x=>x.IsActive&&(x.ParentId==null||x.Parent!.IsActive)).OrderBy(x=>x.Name).ToListAsync();
        m.Available=await Service.AvailableAsync();
    }
    [HttpGet,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Create(){var m=new ExpenseCreateModel();await Choices(m);return View(m);}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Create(ExpenseCreateModel m){
        if(ModelState.IsValid)try{var id=await Service.CreateAsync(m,Actor);TempData["Success"]="Shpenzimi u regjistrua dhe u zbrit nga mjetet e mbledhura.";return RedirectToAction(nameof(Details),new{id});}catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        await Choices(m);return View(m);
    }
    private Task<Expense?> Find(int id)=>db.Expenses.AsNoTracking().Include(x=>x.CreatedBy).Include(x=>x.CancelledBy).SingleOrDefaultAsync(x=>x.Id==id);
    public async Task<IActionResult> Details(int id){var e=await Find(id);return e==null?NotFound():View(e);}
    public async Task<IActionResult> Print(int id){var e=await Find(id);return e==null?NotFound():View(e);}
    [HttpGet,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Cancel(int id){var e=await Find(id);return e==null?NotFound():View(new ExpenseCancelModel{Id=id,Expense=e});}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Admin)]
    public async Task<IActionResult> Cancel(ExpenseCancelModel m){
        m.Expense=await Find(m.Id);if(m.Expense==null)return NotFound();
        if(ModelState.IsValid)try{var id=await Service.CancelAsync(m,Actor);TempData["Success"]="Shpenzimi u anulua me arsye. Kthimi i mjeteve figuron në datën e sotme.";return RedirectToAction(nameof(Details),new{id});}catch(Exception ex)when(ex is InvalidOperationException or ValidationException){ModelState.AddModelError("",ex.Message);}
        return View(m);
    }
}
