using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class UserAdministrationService(ApplicationDbContext db, UserManager<ApplicationUser> users, ILogger<UserAdministrationService> logger)
{
    private static readonly DateTimeOffset DisabledUntil = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static bool IsDisabled(ApplicationUser user) => user.LockoutEnd >= DisabledUntil;
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Ky email është përdorur nga një llogari tjetër.",
            "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or "PasswordRequiresUpper" => "Fjalëkalimi kërkon së paku 6 karaktere, shkronjë të madhe, të vogël dhe numër.",
            "InvalidEmail" or "InvalidUserName" => "Emaili nuk është i vlefshëm.",
            "ConcurrencyFailure" => "Llogaria është ndryshuar ndërkohë. Hapeni përsëri.",
            _ => "Ndryshimi nuk u ruajt. Kontrolloni të dhënat."
        }).Distinct()));
    }

    public async Task<string> SaveAsync(UserEditModel input, string actorId)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2004)");
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var actor = await users.FindByIdAsync(actorId) ?? throw new InvalidOperationException("Hyni përsëri në llogari.");
        await db.Entry(actor).ReloadAsync();
        if (IsDisabled(actor) || !await users.IsInRoleAsync(actor, Roles.Admin)) throw new InvalidOperationException("Vetëm administratori mund të menaxhojë shfrytëzuesit.");
        var isNew = string.IsNullOrEmpty(input.Id);
        var user = isNew ? new ApplicationUser() : await users.FindByIdAsync(input.Id!) ?? throw new InvalidOperationException("Shfrytëzuesi nuk u gjet.");
        if (!isNew) await db.Entry(user).ReloadAsync();
        if (!isNew && user.Id == actorId && (!input.IsActive || input.Role != Roles.Admin)) throw new InvalidOperationException("Nuk mund ta çaktivizoni llogarinë tuaj ose t'ia hiqni rolin Admin.");
        if (!isNew && await users.IsInRoleAsync(user, Roles.Admin) && (!input.IsActive || input.Role != Roles.Admin))
        {
            var admins = await users.GetUsersInRoleAsync(Roles.Admin);
            if (!admins.Any(a => a.Id != user.Id && !IsDisabled(a))) throw new InvalidOperationException("Duhet të mbetet së paku një administrator aktiv.");
        }
        if (!isNew && (!input.IsActive || input.Role != Roles.Coach)
            && await db.TrainingTeams.AnyAsync(t=>t.CoachId==user.Id || t.AssistantCoachId==user.Id))
            throw new InvalidOperationException("Zëvendësoni trajnerin në ekipet përkatëse para çaktivizimit ose ndryshimit të rolit.");
        var email = input.Email.Trim();
        var duplicate = await users.FindByEmailAsync(email);
        if (duplicate != null && duplicate.Id != user.Id) throw new InvalidOperationException("Ky email është përdorur nga një llogari tjetër.");
        user.PhoneNumber = input.Phone?.Trim(); user.Specialization = input.Specialization?.Trim();
        user.FirstName = input.FirstName.Trim(); user.LastName = input.LastName.Trim();
        user.Email = email; user.UserName = email; user.EmailConfirmed = true;
        user.LockoutEnabled = true; user.LockoutEnd = input.IsActive ? null : DisabledUntil;
        user.AccessFailedCount = 0;
        Check(isNew ? await users.CreateAsync(user, input.Password!) : await users.UpdateAsync(user));
        var currentRoles = await users.GetRolesAsync(user);
        var remove = currentRoles.Where(r => r != input.Role).ToArray();
        if (remove.Length > 0) Check(await users.RemoveFromRolesAsync(user, remove));
        if (!currentRoles.Contains(input.Role)) Check(await users.AddToRoleAsync(user, input.Role));
        if (!isNew && !string.IsNullOrEmpty(input.Password))
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            Check(await users.ResetPasswordAsync(user, token, input.Password));
        }
        // Existing sessions must re-authenticate after permission or account changes.
        Check(await users.UpdateSecurityStampAsync(user));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("User administration: actor {ActorId}, target {UserId}, role {Role}, active {Active}, created {Created}", actorId, user.Id, input.Role, input.IsActive, isNew);
        return user.Id;
    }
}
