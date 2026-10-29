using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class FieldCashService(ApplicationDbContext db)
{
    private const string LockSql = "SELECT pg_advisory_xact_lock(20261029, 3101)";

    private IQueryable<FieldPayment> AvailableQuery(string? operatorId = null) => db.FieldPayments.AsNoTracking()
        .Include(x => x.FieldBooking).ThenInclude(x => x!.FootballField).Include(x => x.Actor)
        .Where(x => x.Method == "Cash" && x.CancelledAt == null && x.ActorId != null
            && (operatorId == null || x.ActorId == operatorId)
            && !db.FieldCashHandoverItems.Any(item => item.FieldPaymentId == x.Id));

    public async Task<FieldCashDashboard> Load(string actorId, bool admin)
    {
        var model = new FieldCashDashboard { IsAdmin = admin };
        if (!admin)
        {
            model.AvailablePayments = await AvailableQuery(actorId).OrderByDescending(x => x.Id).ToListAsync();
            model.Available = model.AvailablePayments.Sum(x => x.Amount);
            model.Handovers = await db.FieldCashHandovers.AsNoTracking().Include(x => x.Items)
                .Include(x => x.ConfirmedBy).Include(x => x.RejectedBy).Where(x => x.OperatorId == actorId)
                .OrderByDescending(x => x.Id).Take(40).ToListAsync();
            return model;
        }

        model.Handovers = await db.FieldCashHandovers.AsNoTracking().Include(x => x.Operator).Include(x => x.Items)
            .ThenInclude(x => x.FieldPayment).ThenInclude(x => x!.FieldBooking).Include(x => x.ConfirmedBy).Include(x => x.RejectedBy)
            .OrderBy(x => x.Status == "Pending" ? 0 : 1).ThenByDescending(x => x.Id).Take(100).ToListAsync();
        var available = await AvailableQuery().ToListAsync();
        var pending = model.Handovers.Where(x => x.Status == "Pending").ToList();
        var ids = available.Select(x => x.ActorId!).Concat(pending.Select(x => x.OperatorId)).Distinct().ToList();
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        model.OperatorBalances = ids.Select(id => new FieldCashOperatorBalance {
            OperatorId = id,
            OperatorName = users.TryGetValue(id, out var user) ? DisplayName(user) : id,
            Available = available.Where(x => x.ActorId == id).Sum(x => x.Amount),
            Pending = pending.Where(x => x.OperatorId == id).Sum(x => x.Amount)
        }).OrderByDescending(x => x.Pending).ThenByDescending(x => x.Available).ToList();
        return model;
    }

    public async Task<int> Create(string operatorId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(LockSql);
        if (await db.FieldCashHandovers.AnyAsync(x => x.OperatorId == operatorId && x.Status == "Pending"))
            throw new InvalidOperationException("Keni një dorëzim cash në pritje të konfirmimit.");
        var payments = await AvailableQuery(operatorId).OrderBy(x => x.Id).ToListAsync();
        if (payments.Count == 0) throw new InvalidOperationException("Nuk ka cash të padorëzuar.");
        var handover = new FieldCashHandover { OperatorId = operatorId, Amount = payments.Sum(x => x.Amount), PaymentCount = payments.Count,
            Items = payments.Select(x => new FieldCashHandoverItem { FieldPaymentId = x.Id, Amount = x.Amount }).ToList() };
        db.FieldCashHandovers.Add(handover);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return handover.Id;
    }

    public async Task Confirm(int id, string adminId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(LockSql);
        var handover = await db.FieldCashHandovers.Include(x => x.Items).ThenInclude(x => x.FieldPayment).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Dorëzimi nuk u gjet.");
        if (handover.Status != "Pending") throw new InvalidOperationException("Ky dorëzim është përfunduar.");
        if (handover.Items.Count == 0 || handover.Items.Any(x => x.FieldPayment == null || x.FieldPayment.CancelledAt != null
            || x.FieldPayment.Method != "Cash" || x.FieldPayment.ActorId != handover.OperatorId || x.Amount != x.FieldPayment.Amount)
            || handover.Amount != handover.Items.Sum(x => x.Amount))
            throw new InvalidOperationException("Pagesat e dorëzimit kanë ndryshuar. Kthejeni për korrigjim.");
        handover.Status = "Confirmed"; handover.ConfirmedAt = DateTime.UtcNow; handover.ConfirmedById = adminId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task Reject(int id, string adminId, string reason)
    {
        reason = reason?.Trim() ?? "";
        if (reason.Length is < 3 or > 300) throw new InvalidOperationException("Shkruani arsyen e kthimit.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(LockSql);
        var handover = await db.FieldCashHandovers.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Dorëzimi nuk u gjet.");
        if (handover.Status != "Pending") throw new InvalidOperationException("Ky dorëzim është përfunduar.");
        db.FieldCashHandoverItems.RemoveRange(handover.Items);
        handover.Status = "Rejected"; handover.RejectedAt = DateTime.UtcNow; handover.RejectedById = adminId; handover.RejectionReason = reason;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public static string DisplayName(ApplicationUser user)
    {
        var full = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(full) ? user.UserName ?? "Përdorues" : full;
    }
}
