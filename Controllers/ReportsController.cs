using _2Korriku.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = _2Korriku.Models.Roles.Read)]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(_2Korriku.Models.MoneyDateFilter filter)
    {
        if(!ModelState.IsValid)return View(new _2Korriku.Models.CashReportModel{Filter=filter});
        return View(await new _2Korriku.Services.ExpenseService(_context).ReportAsync(filter));
    }

    public async Task<IActionResult> Print(_2Korriku.Models.MoneyDateFilter filter)
    {
        if(!ModelState.IsValid)return BadRequest("Zgjidhni periudhë të vlefshme.");
        return View(await new _2Korriku.Services.ExpenseService(_context).ReportAsync(filter));
    }

    public async Task<IActionResult> Operations(string? search, string stock = "low")
        => View(await OperationsModel(search, stock));

    public async Task<IActionResult> PrintDebts(string? search)
        => View(await OperationsModel(search, "none"));

    private async Task<ReportsViewModel> OperationsModel(string? search, string stock)
    {
        var positions = await new _2Korriku.Services.BillingService(_context).GetFinancialPositionsAsync();
        var debtIds = positions.Where(p => p.Value.Debt > 0).Select(p => p.Key).ToList();
        var students = await _context.Students.AsNoTracking().Include(s => s.TrainingTeam).Where(s => debtIds.Contains(s.Id)).ToListAsync();
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            foreach (var term in search.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                students = students.Where(s => $"{s.FullName} {s.ParentName} {s.ParentPhone} {s.TrainingTeam?.Name}".Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        var studentsWithDebt = students.Select(s => new ReportsStudentDebtViewModel { Student = s, Balance = positions[s.Id].Debt }).OrderByDescending(s => s.Balance).ThenBy(s => s.Student.LastName).ThenBy(s => s.Student.FirstName).ToList();

        var products = _context.Products.AsNoTracking().Include(p => p.Variants).Where(p => p.IsActive);
        if (stock != "all") products = products.Where(p => p.CurrentStock <= p.MinimumStock || p.Variants.Any(v => v.IsActive && v.CurrentStock <= p.MinimumStock));
        var lowStockProducts = stock == "none" ? [] : await products.OrderBy(p => p.CategoryId).ThenBy(p => p.Name).ToListAsync();

        return new ReportsViewModel
        {
            Search = search, Stock = stock == "all" ? "all" : "low",
            StudentsWithDebt = studentsWithDebt,
            LowStockProducts = lowStockProducts
        };
    }
}

public class ReportsStudentDebtViewModel
{
    public _2Korriku.Models.Student Student { get; set; } = new();
    public decimal Balance { get; set; }
}

public class ReportsViewModel
{
    public string? Search { get; set; }
    public string Stock { get; set; } = "low";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<ReportsStudentDebtViewModel> StudentsWithDebt { get; set; } = new();
    public List<_2Korriku.Models.Product> LowStockProducts { get; set; } = new();
}
