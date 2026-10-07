using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var localToday = BillingClock.LocalDate(now);
        var today = BillingClock.UtcDate(localToday);
        var tomorrow = BillingClock.UtcDate(localToday.AddDays(1));
        var monthStart = BillingClock.UtcDate(new DateOnly(localToday.Year, localToday.Month, 1));

        var activeStudents = await _context.Students.CountAsync(x => x.IsActive);

        var positions = await new BillingService(_context).GetFinancialPositionsAsync();
        var debtTotal = positions.Values.Sum(p => p.Debt);
        var advanceTotal = positions.Values.Sum(p => p.Advance);

        var paymentsToday = await _context.Payments.CountAsync(x => !x.IsCancelled && x.PaymentDate >= today && x.PaymentDate < tomorrow);
        var paymentsMonth = await _context.Payments.CountAsync(x => !x.IsCancelled && x.PaymentDate >= monthStart && x.PaymentDate <= now);
        var salesToday = await _context.Sales.CountAsync(x => x.Date >= today && x.Date < tomorrow);
        var salesMonth = await _context.Sales.CountAsync(x => x.Date >= monthStart && x.Date <= now);
        var lowStock = await _context.Products.CountAsync(x => x.IsActive && (x.CurrentStock <= x.MinimumStock || x.Variants.Any(v => v.IsActive && v.CurrentStock <= x.MinimumStock)));

        var debtIds = positions.Where(p => p.Value.Debt > 0).OrderByDescending(p => p.Value.Debt).Take(5).Select(p => p.Key).ToList();
        var studentsWithDebt = (await _context.Students.AsNoTracking().Where(s => debtIds.Contains(s.Id)).ToListAsync())
            .Select(s => new { student = s, Balance = positions[s.Id].Debt }).OrderByDescending(s => s.Balance).ToList();

        var latestPayments = await _context.Payments
            .Where(x => !x.IsCancelled)
            .OrderByDescending(x => x.PaymentDate)
            .Take(5)
            .Include(x => x.Student)
            .ToListAsync();

        var latestSales = await _context.Sales
            .OrderByDescending(x => x.Date)
            .Take(5)
            .Include(x => x.Student)
            .ToListAsync();

        return new DashboardStats
        {
            ActiveStudents = activeStudents,
            TotalDebt = debtTotal,
            TotalAdvanceCredit = advanceTotal,
            PaymentsToday = paymentsToday,
            PaymentsThisMonth = paymentsMonth,
            SalesToday = salesToday,
            SalesThisMonth = salesMonth,
            ProductsLowInStock = lowStock,
            StudentsWithHighestOutstandingDebt = studentsWithDebt.Select(x => new StudentDebtSummary
            {
                StudentName = x.student.FullName,
                Balance = x.Balance
            }).ToList(),
            LatestPayments = latestPayments,
            LatestSales = latestSales
        };
    }
}

public class DashboardStats
{
    public int ActiveStudents { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal TotalAdvanceCredit { get; set; }
    public int PaymentsToday { get; set; }
    public int PaymentsThisMonth { get; set; }
    public int SalesToday { get; set; }
    public int SalesThisMonth { get; set; }
    public int ProductsLowInStock { get; set; }
    public List<StudentDebtSummary> StudentsWithHighestOutstandingDebt { get; set; } = new();
    public List<Payment> LatestPayments { get; set; } = new();
    public List<Sale> LatestSales { get; set; } = new();
}

public class StudentDebtSummary
{
    public string StudentName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
