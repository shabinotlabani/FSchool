using System.ComponentModel.DataAnnotations;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class BillingService
{
    public async Task<int> CorrectionRevisionAsync(int studentId) =>
        await db.StudentTransactions.Where(t => t.StudentId == studentId).MaxAsync(t => (int?)t.Id) ?? 0;

    public Task<int> CorrectMonthlyFeeAsync(CorrectMonthlyFeeModel input, string actor) => WriteAsync(async () =>
    {
        Validator.ValidateObject(input, new(input), true);
        ValidateReason(input.Reason);
        var amount = input.Amount!.Value;
        if (decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException("Shuma lejon deri në dy shifra dhjetore.");
        var period = await db.MonthlyFeePeriods.SingleOrDefaultAsync(p => p.Id == input.PeriodId)
            ?? throw new InvalidOperationException("Fatura nuk u gjet.");
        await db.Entry(period).ReloadAsync();
        var invoice = (await GetInvoicesAsync(period.StudentId)).Single(i => i.Period.Id == period.Id);
        if (!invoice.CanCorrectFee)
            throw new InvalidOperationException("Korrigjimi nuk lejohet për faturat sezonale ose me lirim aktiv.");
        if (await db.PendingBankPayments.AnyAsync(p => p.StudentId == period.StudentId && p.Kind == "Seasonal" && p.ReviewedAt == null))
            throw new InvalidOperationException("Përfundoni shqyrtimin e pagesës sezonale para korrigjimit.");
        if (input.ExpectedRevision != await CorrectionRevisionAsync(period.StudentId))
            throw new InvalidOperationException("Kartela ka ndryshuar ndërkohë. Rihapni formularin e korrigjimit.");
        if (amount < invoice.Paid)
            throw new InvalidOperationException("Tarifa e korrigjuar nuk mund të jetë më e vogël se shuma e mbuluar.");
        if (amount == invoice.Amount)
            throw new InvalidOperationException("Shkruani një tarifë të ndryshme nga tarifa aktuale.");
        var difference = amount - invoice.Amount;
        var description = $"Tarifa {invoice.Amount:0.00} → {amount:0.00}: {input.Reason.Trim()}";
        db.StudentTransactions.Add(Entry(period.StudentId, period.Year, period.Month,
            "MonthlyFeeAdjustment", description, Math.Max(0, difference), Math.Max(0, -difference),
            "MonthlyFeeCorrection", period.Id, actor));
        if (period.FirstMonthFinal.HasValue)
        {
            period.FirstMonthFinal = amount;
            period.FirstMonthReason = input.Reason.Trim();
        }
        await db.SaveChangesAsync();
        return period.StudentId;
    });
}
