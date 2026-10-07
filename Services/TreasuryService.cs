using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class TreasuryService(ApplicationDbContext db)
{
    private async Task<T> Write<T>(Func<Task<T>> action)
    {
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        var result = await action();
        if (tx != null) await tx.CommitAsync();
        return result;
    }

    public Task<int> CreateAsync(TreasuryCreateModel model, string actor) => Write(async () =>
    {
        Validator.ValidateObject(model, new(model), true);
        var owner = string.IsNullOrWhiteSpace(model.Owner) ? null : model.Owner.Trim();
        var notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        var existing = await db.TreasuryEntries.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == model.RequestId);
        if (existing != null)
        {
            if (existing.Kind != model.Kind || existing.Method != model.Method || existing.Date != model.Date || existing.Amount != model.Amount
                || existing.Owner != owner || existing.Notes != notes || existing.CreatedById != actor)
                throw new InvalidOperationException("Ky formular është përdorur. Rihapni formularin.");
            return existing.Id;
        }
        if (model.Kind == TreasuryEntry.Opening)
        {
            if (await db.TreasuryEntries.AnyAsync(x => x.Kind == TreasuryEntry.Opening && x.Method == model.Method && x.CancelledAt == null))
                throw new InvalidOperationException("Gjendja fillestare për këtë arkë është regjistruar. Për korrigjim, anuloni regjistrimin ekzistues.");
        }
        else
        {
            var money = new ExpenseService(db);
            var available = Math.Min(await money.AvailableFromAsync(model.Date!.Value, model.Method), await money.AvailableFromAsync(model.Date.Value));
            if (model.Amount > available)
                throw new InvalidOperationException($"Mjete të pamjaftueshme në {(model.Method == "Cash" ? "Cash" : "Bankë")}. Për këtë datë mund të tërhiqni deri në {Math.Max(0, available):N2} €.");
        }
        var entry = new TreasuryEntry { RequestId = model.RequestId, Kind = model.Kind, Method = model.Method, Date = model.Date!.Value,
            Amount = model.Amount, Owner = owner, Notes = notes, CreatedById = actor };
        db.TreasuryEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry.Id;
    });

    public Task<int> CancelAsync(TreasuryCancelModel model, string actor) => Write(async () =>
    {
        Validator.ValidateObject(model, new(model), true);
        var entry = await db.TreasuryEntries.SingleOrDefaultAsync(x => x.Id == model.Id) ?? throw new InvalidOperationException("Regjistrimi nuk u gjet.");
        await db.Entry(entry).ReloadAsync();
        if (entry.CancelledAt.HasValue) return entry.Id;
        if (entry.Kind == TreasuryEntry.Opening)
        {
            var money = new ExpenseService(db);
            var available = Math.Min(await money.AvailableFromAsync(BillingClock.Today, entry.Method), await money.AvailableAsync());
            if (entry.Amount > available) throw new InvalidOperationException("Gjendja fillestare nuk mund të anulohet: mjetet janë përdorur. Korrigjoni fillimisht daljet përkatëse.");
        }
        entry.CancelledAt = DateTime.UtcNow;
        entry.CancelledById = actor;
        entry.CancellationReason = model.Reason.Trim();
        await db.SaveChangesAsync();
        return entry.Id;
    });
}
