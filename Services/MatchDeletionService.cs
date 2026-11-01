using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class MatchDeletionService(ApplicationDbContext db)
{
    public async Task Delete(int id, Guid revision)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        // Serialize with both match editing and expense registration.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        var game = await db.MatchGames.SingleOrDefaultAsync(g => g.Id == id)
            ?? throw new UnauthorizedAccessException();
        await db.Entry(game).ReloadAsync();
        if (revision == Guid.Empty || game.Revision != revision)
            throw new ValidationException("Ndeshja ka ndryshuar ndërkohë. Rihapni faqen para fshirjes.");
        if (await db.Expenses.AnyAsync(e => e.MatchGameId == id))
            throw new ValidationException("Ndeshja ka shpenzime të lidhura dhe nuk mund të fshihet. Evidenca financiare duhet të ruhet.");
        db.MatchChanges.RemoveRange(await db.MatchChanges.Where(c => c.MatchGameId == id).ToListAsync());
        db.MatchPlayers.RemoveRange(await db.MatchPlayers.Where(p => p.MatchGameId == id).ToListAsync());
        await db.SaveChangesAsync();
        db.MatchGames.Remove(game);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
