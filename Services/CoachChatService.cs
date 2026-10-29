using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class CoachChatService(ApplicationDbContext db)
{
    public IQueryable<Student> Assigned(string coach) => db.Students.Where(s => s.TrainingTeam != null && s.TrainingTeam.IsActive &&
        (s.TrainingTeam.CoachId == coach || s.TrainingTeam.AssistantCoachId==coach || db.CoachTeams.Any(x=>x.TrainingTeamId==s.TrainingTeamId&&x.UserId==coach)));

    public async Task<bool> IsCoach(string actor) => await db.Users.AnyAsync(x => x.Id == actor &&
        (!x.LockoutEnabled || x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow)) &&
        await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == actor && r.Name == Roles.Coach select ur).AnyAsync();

    public async Task<List<ChatCoach>> Recipients(int studentId)
    {
        var team = await db.Students.Where(x => x.Id == studentId && x.TrainingTeam != null && x.TrainingTeam.IsActive)
            .Select(x => new { x.TrainingTeamId, x.TrainingTeam!.CoachId, x.TrainingTeam.AssistantCoachId }).SingleOrDefaultAsync();
        var result = new List<ChatCoach>();
        if (team == null) return result;
        var coachIds=await db.CoachTeams.Where(x=>x.TrainingTeamId==team.TrainingTeamId).Select(x=>x.UserId).ToListAsync();
        if(team.CoachId!=null)coachIds.Add(team.CoachId);
        if(team.AssistantCoachId!=null)coachIds.Add(team.AssistantCoachId);
        foreach (var id in coachIds.Distinct())
            if (await IsCoach(id!)) {
                var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == id);
                result.Add(new(id!, ((user.FirstName + " " + user.LastName).Trim() is { Length: > 0 } name) ? name : "Trajneri"));
            }
        return result;
    }

    public async Task<Student> Authorize(int studentId, string coach, string? token)
    {
        if (!await IsCoach(coach)) throw new UnauthorizedAccessException();
        var student = await Assigned(coach).AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId) ?? throw new UnauthorizedAccessException();
        if (token != null) {
            var access = await new ParentAccessService(db).Resolve(token);
            if (access == null || !access.Students.Any(x => x.Id == studentId)) throw new UnauthorizedAccessException();
        }
        return student;
    }

    public async Task<ChatView> Load(int studentId, string coach, string? token, long? before = null)
    {
        var student = await Authorize(studentId, coach, token);
        var recipients = await Recipients(studentId);
        var messages = await db.CoachMessages.AsNoTracking().Where(x => x.StudentId == studentId && x.CoachId == coach && (!before.HasValue || x.Id < before))
            .OrderByDescending(x => x.Id).Take(101).ToListAsync();
        return new() { Student = student, CoachId = coach, CoachName = recipients.Single(x => x.Id == coach).Name, Token = token,
            Coaches = recipients, HasOlder = messages.Count > 100, Before = before, Messages = messages.Take(100).Reverse().ToList(),
            Input = new() { StudentId = studentId, CoachId = coach } };
    }

    public async Task Send(ChatInput input, string coach, string? token)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (string.IsNullOrWhiteSpace(input.Body) || input.RequestId == Guid.Empty) throw new ValidationException("Shkruani mesazhin dhe rihapni bisedën.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(20261023, {db.CurrentClubId})");
        await Authorize(input.StudentId, coach, token);
        var existing = await db.CoachMessages.SingleOrDefaultAsync(x => x.RequestId == input.RequestId);
        if (existing != null) {
            if (existing.StudentId != input.StudentId || existing.CoachId != coach || existing.FromParent != (token != null) || existing.Body != input.Body.Trim())
                throw new ValidationException("Rihapni bisedën për të dërguar mesazhin.");
            return;
        }
        db.CoachMessages.Add(new() { StudentId = input.StudentId, CoachId = coach, Body = input.Body.Trim(), FromParent = token != null, RequestId = input.RequestId });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task Read(int studentId, string coach, string? token, long through)
    {
        await Authorize(studentId, coach, token);
        await db.CoachMessages.Where(x => x.StudentId == studentId && x.CoachId == coach && x.Id <= through && x.FromParent == (token == null) && x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.ReadAt, DateTime.UtcNow));
    }

    public async Task<List<CoachInboxRow>> Inbox(string coach, int page = 1)
    {
        if (!await IsCoach(coach)) throw new UnauthorizedAccessException();
        return await Assigned(coach).Where(s => db.CoachMessages.Any(m => m.StudentId == s.Id && m.CoachId == coach))
            .Select(s => new CoachInboxRow { StudentId = s.Id, Name = s.FirstName + " " + s.LastName, ParentName = s.ParentName,
                Body = db.CoachMessages.Where(m => m.StudentId == s.Id && m.CoachId == coach).OrderByDescending(m => m.Id).Select(m => m.Body).First(),
                Date = db.CoachMessages.Where(m => m.StudentId == s.Id && m.CoachId == coach).Max(m => m.CreatedAt),
                Unread = db.CoachMessages.Count(m => m.StudentId == s.Id && m.CoachId == coach && m.FromParent && m.ReadAt == null) })
            .OrderByDescending(x => x.Date).ThenBy(x => x.StudentId).Skip((Math.Clamp(page,1,10000)-1)*50).Take(50).ToListAsync();
    }
}
