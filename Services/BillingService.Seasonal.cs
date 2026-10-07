using System.ComponentModel.DataAnnotations;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class BillingService
{
    public static decimal ReservedSeasonalFunds(IEnumerable<Payment> payments, IEnumerable<MonthlyInvoiceView> invoices)
    {
        var list = invoices.ToList();
        return payments.Where(p => !p.IsCancelled && p.SeasonalStartMonth.HasValue).Sum(p =>
        {
            var paidUntil = p.SeasonalStartMonth!.Value.AddMonths(p.SeasonalMonths!.Value - p.SeasonalMonths.Value / 6);
            var used = list.Where(i => i.Student.Id == p.StudentId && new DateOnly(i.Period.Year,i.Period.Month,1) >= p.SeasonalStartMonth.Value && new DateOnly(i.Period.Year,i.Period.Month,1) < paidUntil)
                .Sum(i => Math.Min(p.SeasonalRate!.Value, Math.Max(0, i.Amount - i.Exempted)));
            return Math.Max(0, p.Amount - used);
        });
    }

    public async Task<Dictionary<int, FinancialPosition>> GetFinancialPositionsAsync(int? studentId = null)
    {
        await using var snapshot = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead) : null;
        var balances = await db.StudentTransactions.AsNoTracking().Where(t => !t.IsCancelled && (!studentId.HasValue || t.StudentId == studentId.Value))
            .GroupBy(t => t.StudentId).Select(g => new { Id = g.Key, Balance = g.Sum(t => t.Debit - t.Credit) }).ToListAsync();
        var seasons = await db.Payments.AsNoTracking().Where(p => !p.IsCancelled && p.SeasonalStartMonth != null && (!studentId.HasValue || p.StudentId == studentId.Value)).ToListAsync();
        var invoices = seasons.Count == 0 ? [] : await GetInvoicesAsync(studentId);
        var personal = await db.PersonalCharges.Where(c=>!c.IsCancelled && (!studentId.HasValue || c.PersonalTraining!.StudentId==studentId)).GroupBy(c=>c.PersonalTraining!.StudentId).Select(g=>new{Id=g.Key,Due=g.Sum(c=>c.Amount-c.Paid-c.Waived)}).ToDictionaryAsync(x=>x.Id,x=>x.Due);
        var result = balances.ToDictionary(b => b.Id, b => new FinancialPosition(b.Balance, ReservedSeasonalFunds(seasons.Where(p => p.StudentId == b.Id), invoices.Where(i => i.Student.Id == b.Id)), personal.GetValueOrDefault(b.Id)));
        if (snapshot != null) await snapshot.CommitAsync();
        return result;
    }

    public async Task EnsureNoSeasonalChangeAsync(IEnumerable<int> studentIds, DateOnly effectiveMonth)
    {
        var ids = studentIds.ToList();
        var seasons = await db.Payments.AsNoTracking().Where(p => ids.Contains(p.StudentId) && !p.IsCancelled && p.SeasonalStartMonth != null).ToListAsync();
        if (seasons.Any(p => p.SeasonalEndMonth >= effectiveMonth))
            throw new InvalidOperationException("Lojtari ka pagesë sezonale. Ndryshimi i kategorisë / familjes lejohet pas përfundimit të sezonit ose pas anulimit të tij me arsye.");
    }

    private async Task<Payment?> ActiveSeasonAsync(int studentId, DateOnly month)
    {
        var payments = await db.Payments.AsNoTracking().Where(p => p.StudentId == studentId && !p.IsCancelled && p.SeasonalStartMonth != null).ToListAsync();
        return payments.SingleOrDefault(p => p.SeasonalStartMonth <= month && p.SeasonalEndMonth >= month);
    }

    public async Task<SeasonalQuote> QuoteSeasonalAsync(SeasonalPaymentModel input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        TariffService.TryMonth(input.StartMonth, out var start);
        var end = start.AddMonths(input.Months - 1);
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == input.StudentId)
            ?? throw new InvalidOperationException("Lojtari nuk u gjet.");
        if (!student.IsActive) throw new InvalidOperationException("Pagesa sezonale lejohet vetëm për lojtarë aktivë.");
        var registered = BillingClock.LocalDate(student.RegistrationDate);
        if (start < new DateOnly(registered.Year, registered.Month, 1)) throw new InvalidOperationException("Sezoni nuk mund të fillojë para regjistrimit të lojtarit.");
        var assignments = await db.StudentFeeAssignments.AsNoTracking().Include(a=>a.FeePlan).Where(a => a.StudentId == student.Id).ToListAsync();
        StudentFeeAssignment? At(DateOnly date) => assignments.Where(a => a.EffectiveMonth <= date).OrderByDescending(a => a.EffectiveMonth).ThenByDescending(a => a.Id).FirstOrDefault();
        if (At(TariffService.CurrentMonth)?.FeePlan?.IsFamily == true)
            throw new InvalidOperationException("Paketa familjare nuk përfiton pagesë sezonale.");
        for (var month = start; month <= end; month = month.AddMonths(1))
            if (At(month)?.FeePlan is { IsFamily: true } or { IsWaiver: true })
                throw new InvalidOperationException("Sezoni nuk mund të përfshijë muaj me paketë familjare ose lirim nga pagesa.");
        var seasons = await db.Payments.AsNoTracking().Where(p => p.StudentId == student.Id && !p.IsCancelled && p.SeasonalStartMonth != null).ToListAsync();
        if (seasons.Any(p => p.SeasonalStartMonth <= end && p.SeasonalEndMonth >= start))
            throw new InvalidOperationException("Kjo periudhë mbulohet nga një pagesë sezonale. Zgjidhni muajin pas përfundimit të saj.");
        var fee = await new TariffService(db).ResolveAsync(student, start);
        if (fee.IsWaiver || fee.Amount <= 0 || decimal.Round(fee.Amount, 2) != fee.Amount || fee.Amount > 999999999999999.99m)
            throw new InvalidOperationException("Sezoni kërkon tarifë mujore pozitive dhe të vlefshme.");
        var invoices = await GetInvoicesAsync(student.Id);
        foreach (var invoice in invoices.Where(i => new DateOnly(i.Period.Year, i.Period.Month, 1) >= start && new DateOnly(i.Period.Year, i.Period.Month, 1) <= end))
            if (invoice.Paid > 0 || invoice.Exempted > 0 || invoice.Period.IsFeeWaived || invoice.Amount != fee.Amount)
                throw new InvalidOperationException("Një muaj i zgjedhur ka pagesë, lirim ose tarifë tjetër. Fillojeni sezonin nga muaji vijues i pambuluar.");
        if (await db.StudentMonthlyExemptions.AnyAsync(e => e.StudentId == student.Id && (e.Year > start.Year || e.Year == start.Year && e.Month >= start.Month) && (e.Year < end.Year || e.Year == end.Year && e.Month <= end.Month)))
            throw new InvalidOperationException("Periudha ka historik lirimesh. Zgjidhni një periudhë tjetër për sezonin.");
        return new(start, input.Months, fee.Amount);
    }

    public Task<int> RegisterSeasonalAsync(SeasonalPaymentModel input, string actorId) => WriteAsync(async () =>
    {
        // Retries remain valid even when the calendar or tariff has since changed.
        TariffService.TryMonth(input.StartMonth, out var start);
        var duplicate = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.RequestId == input.RequestId);
        if (duplicate != null)
        {
            if (duplicate.SeasonalStartMonth != start || duplicate.SeasonalMonths != input.Months || duplicate.SeasonalRate != input.ExpectedRate || duplicate.StudentId != input.StudentId || duplicate.CreatedByUserId != actorId || duplicate.PaymentMethod != input.PaymentMethod || BillingClock.LocalDate(duplicate.PaymentDate) != input.PaymentDate || duplicate.Notes != input.Notes?.Trim())
                throw new InvalidOperationException("Ky formular është përdorur. Hapni një pagesë të re.");
            return duplicate.Id;
        }
        var quote = await QuoteSeasonalAsync(input);
        if (input.ExpectedRate != quote.Rate) throw new InvalidOperationException("Tarifa ka ndryshuar. Rillogaritni sezonin para arkëtimit.");
        var payment = new Payment { StudentId = input.StudentId, RequestId = input.RequestId, Amount = quote.Total, SeasonalStartMonth = quote.Start, SeasonalMonths = quote.Months, SeasonalRate = quote.Rate, PaymentDate = BillingClock.UtcDate(input.PaymentDate!.Value), PaymentMethod = input.PaymentMethod, Notes = input.Notes?.Trim(), CreatedByUserId = actorId };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        var entry = Entry(input.StudentId, input.PaymentDate.Value.Year, input.PaymentDate.Value.Month, "SeasonalPayment", $"Arkëtim sezonal #{payment.Id} · {payment.SeasonalLabel}", 0, quote.Total, "Payment", payment.Id, actorId);
        entry.TransactionDate = payment.PaymentDate;
        db.StudentTransactions.Add(entry);
        var periods = await db.MonthlyFeePeriods.Where(p => p.StudentId == input.StudentId).ToListAsync();
        foreach (var period in periods)
        {
            var month = new DateOnly(period.Year, period.Month, 1);
            if (month < quote.Start || month > quote.End) continue;
            AddSeasonalDiscount(payment, month, actorId);
        }
        await db.SaveChangesAsync();
        // Generate the current month if missing; future months are generated when due.
        if (quote.Start == TariffService.CurrentMonth && !periods.Any(p => p.Year == quote.Start.Year && p.Month == quote.Start.Month))
            await CreateFeeAsync((await db.Students.FindAsync(input.StudentId))!, quote.Start.Year, quote.Start.Month, actorId);
        return payment.Id;
    });

    private void AddSeasonalDiscount(Payment payment, DateOnly month, string? actorId)
    {
        var freeFrom = payment.SeasonalStartMonth!.Value.AddMonths(payment.SeasonalMonths!.Value - payment.SeasonalMonths.Value / 6);
        if (month < freeFrom || month > payment.SeasonalEndMonth) return;
        db.StudentTransactions.Add(Entry(payment.StudentId, month.Year, month.Month, "SeasonalDiscount", $"Zbritje sezonale · {month:MM.yyyy} · Arkëtimi #{payment.Id}", 0, payment.SeasonalRate!.Value, "Payment", payment.Id, actorId));
    }

    private async Task<int> CancelSeasonalAsync(Payment payment, string reason, string actorId)
    {
        var entries = await db.StudentTransactions.Where(t => t.ReferenceId == payment.Id && t.ReferenceType == "Payment" && !t.IsCancelled && (t.TransactionType == "SeasonalPayment" || t.TransactionType == "SeasonalDiscount")).ToListAsync();
        if (entries.Count(t => t.TransactionType == "SeasonalPayment") != 1)
            throw new InvalidOperationException("Pagesa sezonale nuk ka regjistrim financiar të vlefshëm.");
        payment.IsCancelled = true;
        payment.CancellationReason = reason.Trim();
        foreach (var entry in entries)
            db.StudentTransactions.Add(Entry(payment.StudentId, entry.Year, entry.Month, entry.TransactionType + "Reversal", $"Anulim sezoni #{payment.Id} · {entry.Month:00}/{entry.Year}: {reason.Trim()}", entry.Credit, 0, "Payment", payment.Id, actorId));
        await db.SaveChangesAsync();
        return payment.StudentId;
    }
}

public record FinancialPosition(decimal LedgerBalance, decimal SeasonalReserve, decimal PersonalDebt = 0)
{
    private decimal CoreBalance => LedgerBalance + SeasonalReserve - PersonalDebt;
    public decimal AvailableBalance => PersonalDebt > 0 ? PersonalDebt + Math.Max(0, CoreBalance) : CoreBalance;
    public decimal Debt => Math.Max(0, AvailableBalance);
    public decimal Advance => Math.Max(0, -CoreBalance) + SeasonalReserve;
}
