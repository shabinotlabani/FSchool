using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class BillingService
{
    public Task<int?> CurrentFamilyIdAsync(int studentId) => new FamilyService(db).Latest(TariffService.CurrentMonth)
        .Where(a => a.StudentId == studentId && a.FeePlan!.IsFamily)
        .Select(a => a.FamilyId).SingleOrDefaultAsync();

    public async Task<FamilyBillingModel?> GetFamilyAccountAsync(int familyId)
    {
        await using var snapshot = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead) : null;
        var family = await db.PlayerFamilies.AsNoTracking().SingleOrDefaultAsync(f => f.Id == familyId);
        if (family == null) return null;
        var result = new FamilyBillingModel { Family = family };
        var members = await new FamilyService(db).MembersAsync(familyId, TariffService.CurrentMonth);
        foreach (var member in members)
            result.Members.Add((await GetAccountAsync(member.StudentId))!);
        result.State = Hash(new { Month = TariffService.CurrentMonth, family.Revision,
            Members = result.Members.Select(m => new { m.Student.Id, m.MonthlyDue }).OrderBy(m => m.Id) });
        result.Payments = await db.FamilyPayments.AsNoTracking().Where(p => p.FamilyId == familyId)
            .OrderByDescending(p => p.Id).ToListAsync();
        if (snapshot != null) await snapshot.CommitAsync();
        return result;
    }

    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));

    // A family receipt must settle every current member's outstanding monthly debt.
    public static Dictionary<int, decimal> AllocateFamilyPayment(IEnumerable<(int StudentId, decimal Due)> members, decimal amount)
    {
        var debts = members.Where(m => m.Due > 0).OrderBy(m => m.StudentId).ToList();
        var total = debts.Sum(m => m.Due);
        if (amount <= 0 || amount != total || decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException("Lejohet vetëm pagesa e plotë e borxhit të familjes. Rihapni formularin për shumën aktuale.");
        return debts.ToDictionary(m => m.StudentId, m => m.Due);
    }

    public Task<int> RegisterFamilyPaymentAsync(FamilyPaymentModel input, string actorId) => WriteAsync(async () =>
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var hash = Hash(new { input.FamilyId, input.Amount, input.PaymentDate, input.PaymentMethod,
            Notes = input.Notes?.Trim(), input.ExpectedState });
        var duplicate = await db.FamilyPayments.SingleOrDefaultAsync(p => p.RequestId == input.RequestId);
        if (duplicate != null)
        {
            if (duplicate.PayloadHash != hash || duplicate.CreatedByUserId != actorId)
                throw new InvalidOperationException("Ky formular është përdorur. Hapni një pagesë të re.");
            return duplicate.Id;
        }
        var account = await GetFamilyAccountAsync(input.FamilyId)
            ?? throw new InvalidOperationException("Familja nuk u gjet.");
        if (account.Members.Count == 0) throw new InvalidOperationException("Familja nuk ka anëtarë në paketën aktuale.");
        if (input.ExpectedState != account.State)
            throw new InvalidOperationException("Paketa ose borxhi i familjes ka ndryshuar. Rihapni pagesën familjare.");
        var allocations = AllocateFamilyPayment(account.Members.Select(m => (m.Student.Id, m.MonthlyDue)), input.Amount!.Value);
        var batch = new FamilyPayment { FamilyId = input.FamilyId, FamilyName = account.Family.Name,
            RequestId = input.RequestId, PayloadHash = hash, Amount = input.Amount.Value,
            PaymentDate = BillingClock.UtcDate(input.PaymentDate!.Value), PaymentMethod = input.PaymentMethod,
            Notes = input.Notes?.Trim(), CreatedByUserId = actorId };
        db.FamilyPayments.Add(batch);
        foreach (var allocation in allocations.Where(a => a.Value > 0))
            batch.Payments.Add(new Payment { StudentId = allocation.Key, Amount = allocation.Value,
                PaymentDate = batch.PaymentDate, PaymentMethod = batch.PaymentMethod, Notes = batch.Notes,
                CreatedByUserId = actorId });
        await db.SaveChangesAsync();
        foreach (var payment in batch.Payments)
        {
            var entry = Entry(payment.StudentId, input.PaymentDate.Value.Year, input.PaymentDate.Value.Month,
                "Payment", $"Pagesë familjare #{batch.Id} · {batch.FamilyName}", 0, payment.Amount, "Payment", payment.Id, actorId);
            entry.TransactionDate = payment.PaymentDate;
            db.StudentTransactions.Add(entry);
        }
        await db.SaveChangesAsync();
        return batch.Id;
    });

    public Task<int> CancelFamilyPaymentAsync(int id, string reason, string actorId) => WriteAsync(async () =>
    {
        ValidateReason(reason);
        var batch = await db.FamilyPayments.Include(p => p.Payments).SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Pagesa familjare nuk u gjet.");
        if (batch.CancelledAt.HasValue) return batch.FamilyId;
        if (batch.Amount > await new ExpenseService(db).AvailableAsync())
            throw new InvalidOperationException("Pagesa familjare nuk mund të anulohet: mjetet janë përdorur për shpenzime.");
        var now = DateTime.UtcNow;
        foreach (var payment in batch.Payments)
        {
            var original = await db.StudentTransactions.SingleOrDefaultAsync(t => t.TransactionType == "Payment" && t.ReferenceId == payment.Id && !t.IsCancelled);
            if (original == null || payment.IsCancelled)
                throw new InvalidOperationException("Pagesa familjare kërkon kontroll të regjistrimeve financiare.");
            payment.IsCancelled = true;
            payment.CancelledAt = now;
            payment.CancellationReason = reason.Trim();
            db.StudentTransactions.Add(Entry(payment.StudentId, original.Year, original.Month, "PaymentReversal",
                $"Anulim pagese familjare #{id}: {reason.Trim()}", payment.Amount, 0, "Payment", payment.Id, actorId));
        }
        batch.CancelledAt = now;
        batch.CancellationReason = reason.Trim();
        await db.SaveChangesAsync();
        return batch.FamilyId;
    });
}
