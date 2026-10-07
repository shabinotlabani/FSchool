using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Read)]
public class SalesController(ApplicationDbContext db, IssueService issues) : Controller
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Hyni përsëri.");
    public async Task<IActionResult> Index(string? search, int? studentId, bool debtOnly = false)
    {
        var query = db.Sales.AsNoTracking().Include(s => s.Student).AsQueryable();
        if (studentId.HasValue) query = query.Where(s => s.StudentId == studentId);
        if (debtOnly) query = query.Where(s => s.GrandTotal > s.AmountPaid);
        if (!string.IsNullOrWhiteSpace(search))
            foreach (var term in search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                query = query.Where(s => EF.Functions.ILike(s.InvoiceNumber, pattern) || EF.Functions.ILike(s.PlayerName ?? s.Student!.FirstName + " " + s.Student.LastName, pattern));
            }
        ViewData["Search"] = search; ViewData["DebtOnly"] = debtOnly; ViewData["StudentId"] = studentId;
        return View(await query.OrderByDescending(s => s.Id).ToListAsync());
    }
    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Create(int? studentId, int? productId)
    {
        var model = new CreateIssueModel { StudentId = studentId ?? 0, Lines = [new() { ProductId = productId ?? 0 }] };
        await Choices(model); return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Create(CreateIssueModel model)
    {
        if (ModelState.IsValid)
            try { var id = await issues.CreateAsync(model, Actor, User.IsInRole(Roles.Admin)); return RedirectToAction(nameof(Details), new { id }); }
            catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        if (model.Lines == null || model.Lines.Count == 0) model.Lines = [new()];
        await Choices(model); return View(model);
    }
    private async Task Choices(CreateIssueModel model)
    {
        model.Students = await db.Students.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();
        model.Products = await db.Products.AsNoTracking().Include(p => p.Variants).Where(p => p.IsActive).OrderBy(p => p.Name).ThenBy(p => p.Size).ToListAsync();
    }
    private Task<Sale?> Load(int id) => db.Sales.AsNoTracking().Include(s => s.Student).Include(s => s.User).Include(s => s.Details).ThenInclude(d => d.Product).Include(s => s.Payments).SingleOrDefaultAsync(s => s.Id == id);
    public async Task<IActionResult> Details(int id) { var sale = await Load(id); return sale == null ? NotFound() : View(sale); }
    public async Task<IActionResult> Print(int id) { var sale = await Load(id); return sale == null ? NotFound() : View(sale); }
    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Pay(int id)
    {
        var sale = await Load(id); if (sale == null || sale.RequestId == null) return NotFound();
        return View(new IssuePaymentModel { SaleId = id, Sale = sale, Amount = sale.Due });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Pay(IssuePaymentModel model)
    {
        model.Sale = await Load(model.SaleId); if (model.Sale == null || model.Sale.RequestId == null) return NotFound();
        if (ModelState.IsValid)
            try { var id = await issues.PayAsync(model, Actor); return RedirectToAction("Receipt", "Payments", new { id }); }
            catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        return View(model);
    }
}
