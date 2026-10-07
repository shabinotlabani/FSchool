using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class FinancialService
{
    private readonly ApplicationDbContext _context;

    public FinancialService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetCurrentBalanceAsync(int studentId)
    {
        var positions = await new BillingService(_context).GetFinancialPositionsAsync(studentId);
        return positions.GetValueOrDefault(studentId)?.AvailableBalance ?? 0;
    }

    public async Task<List<StudentTransaction>> GetStatementAsync(int studentId)
    {
        var transactions = await _context.StudentTransactions
            .Include(x => x.CreatedByUser)
            .Where(x => x.StudentId == studentId && !x.IsCancelled)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.Id)
            .ToListAsync();

        decimal running = 0m;
        foreach (var item in transactions)
        {
            running += item.Debit - item.Credit;
            item.TransactionDate = item.TransactionDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(item.TransactionDate, DateTimeKind.Utc) : item.TransactionDate;
            item.Description = item.Description ?? string.Empty;
        }

        return transactions;
    }

    public async Task<(decimal debt, decimal advance)> GetDebtSummaryAsync(int studentId)
    {
        var position = (await new BillingService(_context).GetFinancialPositionsAsync(studentId)).GetValueOrDefault(studentId);
        return (position?.Debt ?? 0, position?.Advance ?? 0);
    }

    public async Task<decimal> GetStudentDebtTotalAsync() => (await new BillingService(_context).GetFinancialPositionsAsync()).Values.Sum(p => p.Debt);
    public async Task<decimal> GetStudentAdvanceTotalAsync() => (await new BillingService(_context).GetFinancialPositionsAsync()).Values.Sum(p => p.Advance);

    public async Task GenerateMonthlyFeeAsync(int studentId, int year, int month, string userId, decimal amount, string? exemptionNote = null)
    {
        await new BillingService(_context).GenerateStudentMonthAsync(studentId, year, month, userId);
    }
}