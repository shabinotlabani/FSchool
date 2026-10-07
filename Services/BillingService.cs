using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class BillingService(ApplicationDbContext db)
{
    // Every billing writer takes the same transaction-scoped lock, including workers
    // in other application instances. The unique month and request indexes are a second guard.
    private async Task<T> WriteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var owned = db.Database.CurrentTransaction == null;
        await using var transaction = owned ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)", ct);
        var result = await action();
        if (transaction != null) await transaction.CommitAsync(ct);
        return result;
    }

    public Task<int> GenerateDueFeesAsync(string? actorId = null, DateOnly? asOf = null, CancellationToken ct = default) => WriteAsync(async () =>
    {
        var today = asOf ?? BillingClock.Today;
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var students = await db.Students.Where(s => s.IsActive).ToListAsync(ct);
        var periods = await db.MonthlyFeePeriods.AsNoTracking().ToListAsync(ct);
        var count = 0;
        foreach (var student in students)
        {
            var registration = BillingClock.LocalDate(student.RegistrationDate);
            if (registration > today) continue;
            var firstRegistrationMonth = new DateOnly(registration.Year, registration.Month, 1);
            var known = periods.Where(p => p.StudentId == student.Id).ToList();
            // Existing players start now, never with an unexpected historical back-bill.
            var start = known.Count == 0 ? currentMonth : known.Min(p => new DateOnly(p.Year, p.Month, 1));
            if (start < firstRegistrationMonth) start = firstRegistrationMonth;
            for (var month = start; month <= currentMonth; month = month.AddMonths(1))
            {
                ct.ThrowIfCancellationRequested();
                if (known.Any(p => p.Year == month.Year && p.Month == month.Month)) continue;
                if(!await WasActiveInMonthAsync(student.Id,month,ct))continue;
                await CreateFeeAsync(student, month.Year, month.Month, actorId, ct);
                count++;
            }
        }
        count += await new PersonalTrainingService(db).GenerateAsync(actorId, today, ct);
        return count;
    }, ct);

    public Task<int> GenerateStudentMonthAsync(int studentId, int year, int month, string? actorId) => WriteAsync(async () =>
    {
        var existing = await db.MonthlyFeePeriods.FirstOrDefaultAsync(p => p.StudentId == studentId && p.Year == year && p.Month == month);
        if (existing != null) return existing.Id;
        var student = await db.Students.FindAsync(studentId) ?? throw new InvalidOperationException("Lojtari nuk u gjet.");
        var requested = new DateOnly(year, month, 1);
        var today = BillingClock.Today;
        var registered = BillingClock.LocalDate(student.RegistrationDate);
        if (!student.IsActive || requested > new DateOnly(today.Year, today.Month, 1) || requested < new DateOnly(registered.Year, registered.Month, 1))
            throw new InvalidOperationException("Muaji nuk është i vlefshëm për faturim.");
        if(!await WasActiveInMonthAsync(student.Id,requested))throw new InvalidOperationException("Lojtari ishte joaktiv në këtë muaj.");
        return await CreateFeeAsync(student, year, month, actorId);
    });

    internal async Task<bool> WasActiveInMonthAsync(int studentId,DateOnly month,CancellationToken ct=default)
    {
        var end=BillingClock.UtcDate(month.AddMonths(1));
        var snapshot=await db.StudentChanges.AsNoTracking().Where(c=>c.StudentId==studentId && c.CreatedAt<end).OrderByDescending(c=>c.CreatedAt).ThenByDescending(c=>c.Id).Select(c=>c.After).FirstOrDefaultAsync(ct);
        if(snapshot==null)return true;
        using var json=System.Text.Json.JsonDocument.Parse(snapshot);
        return json.RootElement.GetProperty("IsActive").GetBoolean();
    }

    private async Task<int> CreateFeeAsync(Student student, int year, int month, string? actorId, CancellationToken ct = default)
    {
        var seasonal = await ActiveSeasonAsync(student.Id, new DateOnly(year,month,1));
        var fee = seasonal == null ? await new TariffService(db).ResolveAsync(student, new DateOnly(year,month,1))
            : new ResolvedFee(seasonal.SeasonalRate!.Value, "Pagesa sezonale", false, seasonal.SeasonalLabel, $"Arkëtimi #{seasonal.Id}; tarifa ruhet për sezonin.");
        if (fee.Amount < 0 || decimal.Round(fee.Amount, 2) != fee.Amount)
            throw new InvalidOperationException("Tarifa mujore e lojtarit nuk është e vlefshme.");
        var registration = BillingClock.LocalDate(student.RegistrationDate);
        var firstMonth = seasonal == null && !fee.IsWaiver && fee.Amount > 0 && registration.Year == year && registration.Month == month;
        var weekly = firstMonth && student.FirstMonthUsesWeeks;
        var prorated = firstMonth && (weekly || registration.Day > 1);
        var amount = weekly ? FirstMonthPricing.Calculate(fee.Amount, registration) : prorated ? MonthlyProration.Calculate(fee.Amount,registration) : fee.Amount;
        var period = new MonthlyFeePeriod { FirstMonthWeeks = weekly ? FirstMonthPricing.Weeks(registration) : null, FirstMonthSuggested = weekly ? amount : null, FirstMonthFinal = weekly ? amount : null, FullMonthlyAmount = prorated ? fee.Amount : null, ProratedFrom = prorated ? registration : null, FeePlanName=fee.Name,IsFeeWaived=fee.IsWaiver,FeeReason=fee.Reason,FeeNotes=fee.Notes,StudentId = student.Id, Year = year, Month = month, Generated = true, GeneratedAt = DateTime.UtcNow, CreatedByUserId = actorId };
        db.MonthlyFeePeriods.Add(period);
        await db.SaveChangesAsync(ct);
        db.StudentTransactions.Add(Entry(student.Id, year, month, "MonthlyFee", $"Fletëpagesë {month:00}/{year} · {fee.Name}" + (weekly ? $" · Nga {registration:dd.MM.yyyy}, {FirstMonthPricing.Weeks(registration)}/4 javë" : prorated ? $" · Nga {registration:dd.MM.yyyy}, {DateTime.DaysInMonth(year,month)-registration.Day+1}/{DateTime.DaysInMonth(year,month)} ditë" : ""), amount, 0, "MonthlyFee", period.Id, actorId));
        if (seasonal != null) AddSeasonalDiscount(seasonal, new DateOnly(year,month,1), actorId);
        // Preserve any pre-existing approved exemption as a separate credit, never erase the charge.
        var exemptions = await db.StudentMonthlyExemptions.Where(e => e.StudentId == student.Id && e.Year == year && e.Month == month).ToListAsync(ct);
        var remaining = amount;
        foreach (var exemption in exemptions)
        {
            var exemptionAmount = Math.Min(remaining, Math.Max(0, exemption.Amount));
            if (exemptionAmount <= 0) continue;
            db.StudentTransactions.Add(Entry(student.Id, year, month, "Exemption", "Lirim: " + exemption.Reason, 0, exemptionAmount, "Exemption", exemption.Id, exemption.CreatedByUserId));
            remaining -= exemptionAmount;
        }
        await db.SaveChangesAsync(ct);
        return period.Id;
    }

    public async Task<List<MonthlyInvoiceView>> GetInvoicesAsync(int? studentId = null)
    {
        await using var snapshot = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead) : null;
        var periods = await db.MonthlyFeePeriods.AsNoTracking().Include(p => p.Student)
            .Where(p => !studentId.HasValue || p.StudentId == studentId.Value).OrderBy(p => p.Year).ThenBy(p => p.Month).ThenBy(p => p.Id).ToListAsync();
        var entries = await db.StudentTransactions.AsNoTracking().Where(t => !t.IsCancelled && (!studentId.HasValue || t.StudentId == studentId.Value)).ToListAsync();
        var seasons = await db.Payments.AsNoTracking().Where(p => !p.IsCancelled && p.SeasonalStartMonth != null && (!studentId.HasValue || p.StudentId == studentId.Value)).ToListAsync();
        var ledgers = entries.ToLookup(t => t.StudentId);
        var invoices = new List<MonthlyInvoiceView>();
        foreach (var studentPeriods in periods.GroupBy(p => p.StudentId))
        {
            var ledger = ledgers[studentPeriods.Key].ToList();
            var available = Math.Max(0, ledger.Where(t => !t.TransactionType.StartsWith("Training") && !t.TransactionType.StartsWith("Sale") && !t.TransactionType.StartsWith("Seasonal") && t.TransactionType != "MonthlyFee" && t.TransactionType != "MonthlyFeeAdjustment" && t.TransactionType != "Exemption" && t.TransactionType != "ExemptionReversal").Sum(t => t.Credit - t.Debit));
            foreach (var period in studentPeriods)
            {
                var monthEntries = ledger.Where(t => t.Year == period.Year && t.Month == period.Month).ToList();
                var amount = monthEntries.Where(t => (t.TransactionType == "MonthlyFee" || t.TransactionType == "MonthlyFeeAdjustment")).Sum(t => t.Debit - t.Credit);
                var exempted = monthEntries.Where(t => t.TransactionType == "Exemption" || t.TransactionType == "ExemptionReversal").Sum(t => t.Credit - t.Debit);
                var discount = monthEntries.Where(t => t.TransactionType is "SeasonalDiscount" or "SeasonalDiscountReversal").Sum(t => t.Credit - t.Debit);
                var date = new DateOnly(period.Year, period.Month, 1);
                var season = seasons.SingleOrDefault(p => p.StudentId == period.StudentId && p.SeasonalStartMonth <= date && p.SeasonalEndMonth >= date);
                var due = Math.Max(0, amount - exempted - discount);
                var reserved = season != null && date < season.SeasonalStartMonth!.Value.AddMonths(season.SeasonalMonths!.Value - season.SeasonalMonths.Value / 6) ? Math.Min(due, season.SeasonalRate!.Value) : 0;
                var paid = Math.Min(available, due - reserved);
                available -= paid;
                invoices.Add(new MonthlyInvoiceView { Period = period, Student = period.Student!, Amount = amount, Exempted = exempted + discount, SeasonalDiscount = discount, IsSeasonal = season != null, Paid = paid + reserved });
            }
        }
        if (snapshot != null) await snapshot.CommitAsync();
        return invoices;
    }

    public async Task<PlayerBillingModel?> GetAccountAsync(int studentId)
    {
        await using var snapshot = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead) : null;
        var student = await db.Students.AsNoTracking().Include(s=>s.TrainingTeam).ThenInclude(t=>t!.Sessions).FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return null;
        var currentFee = await new TariffService(db).ResolveAsync(student,TariffService.CurrentMonth);
        student.MonthlyFee = currentFee.Amount;
        var account = new PlayerBillingModel
        {
            CurrentFamilyId = await CurrentFamilyIdAsync(studentId),
            Student = student,
            CurrentFeeName = currentFee.Name,
            FirstMonthChanges = await db.FirstMonthFeeChanges.AsNoTracking().Include(x=>x.Actor).Where(x=>x.Period!.StudentId==studentId).OrderByDescending(x=>x.Id).ToListAsync(),
            PersonalTrainings = await db.PersonalTrainings.AsNoTracking().Include(x=>x.Sessions).Include(x=>x.Charges).Where(x=>x.StudentId==studentId).OrderByDescending(x=>x.Id).ToListAsync(),
            FeeAssignments = await db.StudentFeeAssignments.AsNoTracking().Include(a=>a.FeePlan).Include(a=>a.CreatedByUser).Where(a=>a.StudentId==studentId).OrderByDescending(a=>a.EffectiveMonth).ThenByDescending(a=>a.Id).ToListAsync(),
            Sales = await db.Sales.AsNoTracking().Where(s => s.StudentId == studentId).OrderByDescending(s => s.Id).ToListAsync(),
            Invoices = await GetInvoicesAsync(studentId),
            History = await db.StudentTransactions.AsNoTracking().Include(t => t.CreatedByUser).Where(t => t.StudentId == studentId).OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).ToListAsync(),
            Payments = await db.Payments.AsNoTracking().Include(p => p.CreatedByUser).Where(p => p.StudentId == studentId).OrderByDescending(p => p.CreatedAt).ToListAsync(),
            Exemptions = await db.StudentMonthlyExemptions.AsNoTracking().Include(e => e.CreatedByUser).Where(e => e.StudentId == studentId).OrderByDescending(e => e.CreatedAt).ToListAsync()
        };
        if (snapshot != null) await snapshot.CommitAsync();
        return account;
    }

    public Task<int> RegisterPaymentAsync(RegisterPaymentModel input, string actorId) => WriteAsync(async () =>
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (await CurrentFamilyIdAsync(input.StudentId) != null)
            throw new InvalidOperationException("Lojtari është në paketë familjare. Përdorni pagesën e përbashkët të familjes.");
        var duplicate = await db.Payments.SingleOrDefaultAsync(p => p.RequestId == input.RequestId);
        if (duplicate != null)
        {
            if (duplicate.PersonalChargeId != null || duplicate.SeasonalStartMonth != null || duplicate.SaleId != null || duplicate.StudentId != input.StudentId || duplicate.Amount != input.Amount || duplicate.CreatedByUserId != actorId)
                throw new InvalidOperationException("Ky formular është përdorur. Hapni një pagesë të re.");
            return duplicate.Id;
        }
        if (!await db.Students.AnyAsync(s => s.Id == input.StudentId)) throw new InvalidOperationException("Lojtari nuk u gjet.");
        var payment = new Payment { StudentId = input.StudentId, RequestId = input.RequestId, Amount = input.Amount!.Value, PaymentDate = BillingClock.UtcDate(input.PaymentDate!.Value), PaymentMethod = input.PaymentMethod, Notes = input.Notes?.Trim(), CreatedByUserId = actorId };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        var entry = Entry(input.StudentId, input.PaymentDate.Value.Year, input.PaymentDate.Value.Month, "Payment", $"Pagesë #{payment.Id} · {MethodLabel(input.PaymentMethod)}", 0, payment.Amount, "Payment", payment.Id, actorId);
        entry.TransactionDate = payment.PaymentDate;
        db.StudentTransactions.Add(entry);
        await db.SaveChangesAsync();
        return payment.Id;
    });

    public Task<int> ExemptAsync(ExemptInvoiceModel input, string actorId) => WriteAsync(async () =>
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (input.ExemptionType is not ("Sickness" or "Vacation")) throw new InvalidOperationException("Zgjidhni sëmundje ose pushime.");
        var period = await db.MonthlyFeePeriods.FindAsync(input.PeriodId) ?? throw new InvalidOperationException("Fatura nuk u gjet.");
        var invoice = (await GetInvoicesAsync(period.StudentId)).Single(i => i.Period.Id == period.Id);
        if (invoice.IsSeasonal) throw new InvalidOperationException("Muaji mbulohet nga pagesa sezonale. Për korrigjim, anuloni fillimisht sezonin me arsye.");
        if (invoice.Exempted > 0) throw new InvalidOperationException("Fatura ka lirim aktiv. Ktheni lirimin ekzistues para korrigjimit.");
        if (!invoice.CanExempt) throw new InvalidOperationException("Lirimi lejohet vetëm për faturat pa asnjë pagesë. Fatura e paguar, edhe pjesërisht, nuk mund të lirohet.");
        if (input.Amount > invoice.Amount || decimal.Round(input.Amount!.Value, 2) != input.Amount.Value)
            throw new InvalidOperationException("Lirimi nuk mund të tejkalojë tarifën dhe lejon vetëm dy shifra dhjetore.");
        var exemption = new StudentMonthlyExemption { StudentId = period.StudentId, Year = period.Year, Month = period.Month, Amount = input.Amount.Value, ExemptionType = input.ExemptionType, Reason = input.Reason.Trim(), Notes = input.Notes?.Trim(), CreatedByUserId = actorId };
        db.StudentMonthlyExemptions.Add(exemption);
        await db.SaveChangesAsync();
        db.StudentTransactions.Add(Entry(period.StudentId, period.Year, period.Month, "Exemption", $"Lirim ({TypeLabel(input.ExemptionType)}): {exemption.Reason}", 0, exemption.Amount, "Exemption", exemption.Id, actorId));
        await db.SaveChangesAsync();
        return exemption.Id;
    });

    public Task<int> ReverseExemptionAsync(int id, string reason, string actorId) => WriteAsync(async () =>
    {
        ValidateReason(reason);
        var exemption = await db.StudentMonthlyExemptions.FindAsync(id) ?? throw new InvalidOperationException("Lirimi nuk u gjet.");
        if (await db.StudentTransactions.AnyAsync(t => t.TransactionType == "ExemptionReversal" && t.ReferenceId == id)) return exemption.StudentId;
        var credit = await db.StudentTransactions.Where(t => t.TransactionType == "Exemption" && t.ReferenceId == id && !t.IsCancelled).SumAsync(t => t.Credit);
        if (credit <= 0) throw new InvalidOperationException("Ky lirim i vjetër nuk ka regjistrim financiar për kthim automatik.");
        db.StudentTransactions.Add(Entry(exemption.StudentId, exemption.Year, exemption.Month, "ExemptionReversal", "Kthim lirimi: " + reason.Trim(), credit, 0, "Exemption", id, actorId));
        await db.SaveChangesAsync();
        return exemption.StudentId;
    });

    public Task<int> CancelPaymentAsync(int id, string reason, string actorId) => WriteAsync(async () =>
    {
        ValidateReason(reason);
        var payment = await db.Payments.FindAsync(id) ?? throw new InvalidOperationException("Pagesa nuk u gjet.");
        if (payment.FamilyPaymentId.HasValue)
            throw new InvalidOperationException("Kjo pagesë është pjesë e arkëtimit familjar. Anuloni pagesën e plotë të familjes.");
        if (payment.IsCancelled) return payment.StudentId;
        await db.Entry(payment).ReloadAsync();
        if (payment.IsCancelled) return payment.StudentId;
        await new ExpenseService(db).EnsureReceiptCancellationAsync(payment);
        payment.CancelledAt = DateTime.UtcNow;
        if (payment.PersonalChargeId.HasValue) return await new PersonalTrainingService(db).CancelPaymentAsync(payment, reason, actorId);
        if (payment.SeasonalStartMonth.HasValue) return await CancelSeasonalAsync(payment, reason, actorId);
        var original = await db.StudentTransactions.SingleOrDefaultAsync(t => t.TransactionType == (payment.SaleId.HasValue ? "SalePayment" : "Payment") && t.ReferenceId == id && !t.IsCancelled);
        if (original == null) throw new InvalidOperationException("Pagesa nuk ka regjistrim financiar të lidhur. Kërkohet kontroll manual.");
        payment.IsCancelled = true;
        payment.CancellationReason = reason.Trim();
        if (payment.SaleId.HasValue)
        {
            var sale = await db.Sales.FindAsync(payment.SaleId.Value) ?? throw new InvalidOperationException("Dalja nuk u gjet.");
            await db.Entry(sale).ReloadAsync();
            sale.AmountPaid -= payment.Amount;
        }
        db.StudentTransactions.Add(Entry(payment.StudentId, original.Year, original.Month, payment.SaleId.HasValue ? "SalePaymentReversal" : "PaymentReversal", $"Anulim pagese #{id}: {reason.Trim()}", payment.Amount, 0, payment.SaleId.HasValue ? "SalePayment" : "Payment", id, actorId));
        await db.SaveChangesAsync();
        return payment.StudentId;
    });

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 200) throw new InvalidOperationException("Shkruani arsyen (deri në 200 karaktere).");
    }

    private static StudentTransaction Entry(int studentId, int year, int month, string type, string description, decimal debit, decimal credit, string referenceType, int referenceId, string? actorId) => new()
    {
        StudentId = studentId, Year = year, Month = month, TransactionType = type,
        Description = description, Debit = debit, Credit = credit, ReferenceType = referenceType,
        ReferenceId = referenceId, CreatedByUserId = actorId, CreatedAt = DateTime.UtcNow, TransactionDate = DateTime.UtcNow
    };
    public static string TypeLabel(string type) => type == "Sickness" ? "Sëmundje" : "Pushime";
    public static string MethodLabel(string method) => method switch { "Cash" => "Para në dorë", "Bank" => "Bankë", _ => "Tjetër" };
}
