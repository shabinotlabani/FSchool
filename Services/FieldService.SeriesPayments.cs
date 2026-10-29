using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public partial class FieldService
{
    public async Task<List<FieldBooking>> SeriesBookingsAsync(int bookingId)
    {
        var anchor = await db.FieldBookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("Termini nuk u gjet.");
        if (!anchor.SeriesId.HasValue) throw new InvalidOperationException("Ky termin nuk ka rezervime të përsëritura.");
        return await db.FieldBookings.Include(b => b.Payments).Include(b => b.FootballField)
            .Where(b => b.SeriesId == anchor.SeriesId).OrderBy(b => b.Date).ThenBy(b => b.Id).ToListAsync();
    }

    public static string SeriesState(IEnumerable<FieldBooking> bookings) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(bookings.OrderBy(b => b.Id).Select(b => new { b.Id, b.Revision, b.Price, b.Paid, b.IsCancelled })))));

    public async Task<List<FieldBooking>> ValidateSeriesPaymentAsync(FieldPaymentModel input,bool confirmedBank=false)
    {
        var bookings = await SeriesBookingsAsync(input.BookingId);
        var due = bookings.Where(b => !b.IsCancelled && b.Due > 0).ToList();
        if (SeriesState(bookings) != input.ExpectedState || due.Count == 0 || !confirmedBank && input.Amount != due.Sum(b => b.Due))
            throw new InvalidOperationException("Rezervimet ose pagesat kanë ndryshuar. Rihapni pagesën e plotë.");
        return due;
    }

    private async Task<int> PaySeriesAsync(FieldPaymentModel input, string actor)
    {
        Validator.ValidateObject(input, new(input), true);
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync() : null;
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlRawAsync(MoneyLockSql);
        var existing = await db.FieldPayments.Where(p => p.BatchId == input.RequestId).OrderBy(p => p.Id).ToListAsync();
        if (existing.Count > 0)
        {
            var series = await SeriesBookingsAsync(input.BookingId);
            if (existing.Sum(p => p.Amount) != input.Amount || existing.Any(p => p.Method != input.Method || p.ActorId != actor || p.BatchState != input.ExpectedState || !series.Any(b => b.Id == p.FieldBookingId)))
                throw new InvalidOperationException("Ky formular është përdorur. Rihapni pagesën.");
            return existing[0].Id;
        }
        if (await db.FieldPayments.AnyAsync(p => p.RequestId == input.RequestId)) throw new InvalidOperationException("Ky formular është përdorur.");
        var confirmedBank=await BankVerificationService.IsConfirmedAmount(db,input.RequestId,"Field",input.Amount,actor);
        var bookings = await ValidateSeriesPaymentAsync(input,confirmedBank);
        var remaining=input.Amount;
        FieldPayment? first = null;
        foreach (var booking in bookings)
        {
            if(remaining<=0)break;
            var amount = booking==bookings.Last()?remaining:Math.Min(remaining,booking.Due);
            remaining-=amount;
            var payment = new FieldPayment { BatchId = input.RequestId, BatchState = input.ExpectedState,
                RequestId = first == null ? input.RequestId : Guid.NewGuid(), FieldBookingId = booking.Id,
                Amount = amount, Method = input.Method, ActorId = actor,
                Description = $"{booking.CustomerName} · {booking.FootballField!.Name} · {booking.Date:dd/MM/yyyy} · {booking.StartsAt:HH:mm}–{booking.EndsAt:HH:mm}" };
            first ??= payment;
            db.FieldPayments.Add(payment);
            booking.Revision = Guid.NewGuid();
            booking.Changes.Add(new FieldBookingChange { ActorId = actor, Description = $"Pagesë e rezervimit · Ky termin: {amount:N2} · {BillingService.MethodLabel(input.Method)}" });
        }
        await db.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
        return first!.Id;
    }
}
