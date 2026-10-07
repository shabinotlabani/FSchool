using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class FieldService(ApplicationDbContext db)
{
    private const string MoneyLockSql = "SELECT pg_advisory_xact_lock(20260929, 2002)";

    public async Task<int> PayAsync(FieldPaymentModel input, string actor)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync(MoneyLockSql);
        var existing = await db.FieldPayments.AsNoTracking().SingleOrDefaultAsync(p => p.RequestId == input.RequestId);
        if (existing != null)
        {
            if (existing.FieldBookingId != input.BookingId || existing.Amount != input.Amount || existing.Method != input.Method || existing.ActorId != actor)
                throw new InvalidOperationException("Ky formular është përdorur. Hapni një pagesë të re.");
            return existing.Id;
        }
        var booking = await db.FieldBookings.Include(b => b.Payments).Include(b => b.FootballField).SingleOrDefaultAsync(b => b.Id == input.BookingId)
            ?? throw new InvalidOperationException("Termini nuk u gjet.");
        if (booking.IsCancelled || booking.Revision != input.Revision) throw new InvalidOperationException("Termini ose pagesat kanë ndryshuar. Rihapni formularin.");
        if (input.Amount > booking.Due) throw new InvalidOperationException("Shuma tejkalon borxhin e mbetur të terminit.");
        var payment = new FieldPayment { FieldBookingId = booking.Id, RequestId = input.RequestId, Amount = input.Amount, Method = input.Method, ActorId = actor,
            Description = $"{booking.CustomerName} · {booking.FootballField!.Name} · {booking.Date:dd.MM.yyyy} · {booking.StartsAt:HH:mm}–{booking.EndsAt:HH:mm}" };
        db.FieldPayments.Add(payment); booking.Revision = Guid.NewGuid();
        booking.Changes.Add(new FieldBookingChange { ActorId = actor, Description = $"Arkëtim: {input.Amount:N2} € · {BillingService.MethodLabel(input.Method)}" });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return payment.Id;
    }

    public async Task<int> CancelPaymentAsync(FieldPaymentCancelModel input, string actor)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync(MoneyLockSql);
        var payment = await db.FieldPayments.Include(p => p.FieldBooking).SingleOrDefaultAsync(p => p.Id == input.Id)
            ?? throw new InvalidOperationException("Pagesa nuk u gjet.");
        if (payment.CancelledAt.HasValue) return payment.FieldBookingId;
        if (payment.Amount > await new ExpenseService(db).AvailableAsync())
            throw new InvalidOperationException("Pagesa nuk mund të anulohet: mjetet janë përdorur për shpenzime. Korrigjoni fillimisht shpenzimet përkatëse.");
        payment.CancelledAt = DateTime.UtcNow; payment.CancellationReason = input.Reason.Trim();
        payment.FieldBooking!.Revision = Guid.NewGuid();
        payment.FieldBooking.Changes.Add(new FieldBookingChange { ActorId = actor, Description = $"Anulim i pagesës {payment.Number}: {payment.Amount:N2} € · {payment.CancellationReason}" });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return payment.FieldBookingId;
    }

    // Share the team lock: a team edit and a private reservation cannot race.
    public async Task<int> SaveFieldAsync(FieldEditModel input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var name = input.Name.Trim();
        if (await db.FootballFields.AnyAsync(f => f.Id != input.Id && f.Name.ToUpper() == name.ToUpper()))
            throw new InvalidOperationException("Ekziston një fushë me këtë emër.");
        var field = input.Id == 0 ? new FootballField() : await db.FootballFields.SingleOrDefaultAsync(f => f.Id == input.Id)
            ?? throw new InvalidOperationException("Fusha nuk u gjet.");
        if (input.Id != 0 && field.Revision != input.Revision) throw new InvalidOperationException("Fusha u ndryshua ndërkohë. Hapeni përsëri.");
        if (!input.IsActive && (await db.TrainingTeams.AnyAsync(t => t.FootballFieldId == field.Id && t.IsActive)
            || await db.FieldBookings.AnyAsync(b => b.FootballFieldId == field.Id && !b.IsCancelled && b.Date >= BillingClock.Today)))
            throw new InvalidOperationException("Para çaktivizimit, zhvendosni ekipet aktive dhe zhvendosni ose anuloni terminet e ardhshme të kësaj fushe.");
        field.Name = name; field.Location = input.Location?.Trim(); field.Notes = input.Notes?.Trim();
        field.IsActive = input.IsActive; field.Revision = Guid.NewGuid();
        if (input.Id == 0) db.FootballFields.Add(field);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return field.Id;
    }

    public async Task<int> SaveBookingAsync(FieldBookingEditModel input, string actor)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        if (input.Id == 0)
        {
            var existing = await db.FieldBookings.AsNoTracking().SingleOrDefaultAsync(b => b.RequestId == input.RequestId);
            if (existing != null)
            {
                if (existing.FootballFieldId != input.FootballFieldId || existing.Date != input.Date || existing.StartsAt != input.StartsAt
                    || existing.EndsAt != input.EndsAt || existing.Price != input.Price || existing.CustomerName != input.CustomerName.Trim()
                    || existing.Phone != input.Phone?.Trim() || existing.Notes != input.Notes?.Trim() || existing.SeriesWeeks != input.Weeks)
                    throw new InvalidOperationException("Ky formular është përdorur. Hapni një termin të ri ose ndryshoni rezervimin ekzistues.");
                return existing.Id;
            }
        }
        var field = await db.FootballFields.SingleOrDefaultAsync(f => f.Id == input.FootballFieldId && f.IsActive)
            ?? throw new InvalidOperationException("Zgjidhni një fushë aktive.");
        var booking = input.Id == 0 ? new FieldBooking { RequestId = input.RequestId } : await db.FieldBookings.Include(b => b.Payments).SingleOrDefaultAsync(b => b.Id == input.Id)
            ?? throw new InvalidOperationException("Termini nuk u gjet.");
        if (input.Id != 0 && (booking.Revision != input.Revision || booking.IsCancelled))
            throw new InvalidOperationException("Termini u ndryshua ose u anulua. Hapeni përsëri.");
        if (input.Price < booking.Paid) throw new InvalidOperationException("Çmimi nuk mund të jetë më i ulët se shuma e paguar. Korrigjoni fillimisht pagesat përkatëse.");
        var start = input.StartsAt!.Value; var end = input.EndsAt!.Value;
        var dates = Enumerable.Range(0, input.Weeks).Select(week => input.Date.AddDays(week * 7)).ToList();
        var conflicts = await db.FieldBookings.AsNoTracking().Where(b => b.Id != input.Id && !b.IsCancelled && b.FootballFieldId == field.Id && dates.Contains(b.Date) && b.StartsAt < end && b.EndsAt > start)
            .Select(b => b.Date).Distinct().OrderBy(date => date).ToListAsync();
        if (conflicts.Count > 0)
            throw new InvalidOperationException("Fusha është e rezervuar më " + string.Join(", ", conflicts.Select(date => date.ToString("dd.MM.yyyy"))) + ". Asnjë termin nuk u ruajt. Ndryshoni datën, orarin ose fushën.");
        var day = ((int)input.Date.DayOfWeek + 6) % 7 + 1;
        if (await db.TrainingSessions.AnyAsync(s => s.TrainingTeam!.IsActive && s.TrainingTeam.FootballFieldId == field.Id && s.Day == day && s.StartsAt < end && s.EndsAt > start))
            throw new InvalidOperationException("Në këtë orar fusha përdoret nga ekipet. Zgjidhni një orar ose fushë tjetër për terminin privat.");
        booking.Price = input.Price;
        booking.FootballFieldId = field.Id; booking.CustomerName = input.CustomerName.Trim(); booking.Phone = input.Phone?.Trim();
        booking.Date = input.Date; booking.StartsAt = start; booking.EndsAt = end; booking.Notes = input.Notes?.Trim(); booking.Revision = Guid.NewGuid();
        if (input.Id == 0) db.FieldBookings.Add(booking);
        if (input.Id == 0)
        {
            booking.SeriesWeeks = input.Weeks;
            booking.SeriesId = input.Weeks > 1 ? input.RequestId : null;
            foreach (var date in dates.Skip(1))
            {
                var next = new FieldBooking { RequestId = Guid.NewGuid(), SeriesId = booking.SeriesId, SeriesWeeks = input.Weeks,
                    FootballFieldId = field.Id, CustomerName = booking.CustomerName, Phone = booking.Phone,
                    Date = date, StartsAt = start, EndsAt = end, Price = input.Price, Notes = booking.Notes };
                next.Changes.Add(new FieldBookingChange { ActorId = actor, Description = $"Rezervim javor ({input.Weeks} javë): {field.Name} · {date:dd.MM.yyyy} · {start:HH:mm}–{end:HH:mm} · {next.CustomerName} · {next.Phone} · {next.Price:N2} € · {next.Notes}" });
                db.FieldBookings.Add(next);
            }
        }
        booking.Changes.Add(new FieldBookingChange { ActorId = actor, Description = $"{(input.Id == 0 ? "Rezervuar" : "Ndryshuar")}: {field.Name} · {booking.Date:dd.MM.yyyy} · {start:HH:mm}–{end:HH:mm} · {booking.CustomerName} · {booking.Phone} · {booking.Price:N2} € · {booking.Notes}" });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return booking.Id;
    }

    public async Task CancelAsync(FieldBookingCancelModel input, string actor)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var booking = await db.FieldBookings.Include(b => b.Payments).SingleOrDefaultAsync(b => b.Id == input.Id) ?? throw new InvalidOperationException("Termini nuk u gjet.");
        if (booking.IsCancelled) return;
        if (booking.Revision != input.Revision) throw new InvalidOperationException("Termini u ndryshua ndërkohë. Hapeni përsëri para anulimit.");
        if (booking.Paid > 0) throw new InvalidOperationException("Termini ka pagesa aktive. Anuloni dhe ktheni fillimisht pagesat, pastaj anuloni terminin.");
        booking.IsCancelled = true; booking.CancellationReason = input.Reason.Trim(); booking.Revision = Guid.NewGuid();
        booking.Changes.Add(new FieldBookingChange { ActorId = actor, Description = "Anuluar: " + booking.CancellationReason });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<FieldScheduleModel> ScheduleAsync(DateOnly from, int? fieldId, bool includeCancelled)
    {
        var model = new FieldScheduleModel { From = from, FieldId = fieldId, IncludeCancelled = includeCancelled,
            Fields = await db.FootballFields.AsNoTracking().OrderBy(f => f.Name).ToListAsync(),
            UnassignedTeams = await db.TrainingTeams.CountAsync(t => t.IsActive && t.FootballFieldId == null) };
        var sessions = await db.TrainingSessions.AsNoTracking().Include(s => s.TrainingTeam).ThenInclude(t => t!.FootballField)
            .Where(s => s.TrainingTeam!.IsActive && s.TrainingTeam.FootballFieldId != null && (!fieldId.HasValue || s.TrainingTeam.FootballFieldId == fieldId)).ToListAsync();
        for (var date = from; date <= model.To; date = date.AddDays(1))
        {
            var day = ((int)date.DayOfWeek + 6) % 7 + 1;
            model.Items.AddRange(sessions.Where(s => s.Day == day).Select(s => new FieldScheduleItem {
                Date = date, FieldId = s.TrainingTeam!.FootballFieldId!.Value, FieldName = s.TrainingTeam.FootballField!.Name,
                StartsAt = s.StartsAt, EndsAt = s.EndsAt, Name = s.TrainingTeam.Name, TeamId = s.TrainingTeamId }));
        }
        var bookings = await db.FieldBookings.AsNoTracking().Include(b => b.FootballField).Include(b => b.Payments)
            .Where(b => b.Date >= from && b.Date <= model.To && (includeCancelled || !b.IsCancelled) && (!fieldId.HasValue || b.FootballFieldId == fieldId)).ToListAsync();
        model.Items.AddRange(bookings.Select(b => new FieldScheduleItem { Date = b.Date, FieldId = b.FootballFieldId, FieldName = b.FootballField!.Name,
            StartsAt = b.StartsAt, EndsAt = b.EndsAt, Name = b.CustomerName, BookingId = b.Id, IsCancelled = b.IsCancelled, Due = b.Due, CanCancel = !b.IsCancelled && b.Paid == 0 }));
        model.Items = model.Items.OrderBy(i => i.Date).ThenBy(i => i.StartsAt).ThenBy(i => i.FieldName).ThenBy(i => i.Name).ToList();
        return model;
    }
}
