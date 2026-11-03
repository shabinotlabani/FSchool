using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using _2Korriku.Data;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public record StudentDuplicateInput(string? FirstName, string? LastName, string? Phone, string? ParentPhone);
public record StudentDuplicateMatch(int Id, string FirstName, string LastName, DateOnly DateOfBirth,
    string? Team, string? ParentName, string? Phone, string? ParentPhone, bool IsActive,
    bool NameMatches, bool PhoneMatches);
public record StudentDuplicateReview(List<StudentDuplicateMatch> Matches, string Token);
public class StudentDuplicateWarning(StudentDuplicateReview review) : InvalidOperationException("Kontrolloni lojtarët ekzistues para se të vazhdoni.")
{
    public StudentDuplicateReview Review { get; } = review;
}

public class StudentDuplicateService(ApplicationDbContext db)
{
    public static string NormalizeName(string? value) => Regex.Replace((value ?? "").Trim(), @"\s+", " ").Normalize().ToUpperInvariant();
    public static string NormalizePhone(string? value)
    {
        var digits = new string((value ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digits.StartsWith("00")) digits = digits[2..];
        // Kosovo local numbers and their international representation are equivalent.
        if (digits.Length == 9 && digits.StartsWith('0')) digits = "383" + digits[1..];
        return digits.Length >= 6 ? digits : "";
    }

    public async Task<StudentDuplicateReview> FindAsync(IEnumerable<StudentDuplicateInput> input)
    {
        var keys = input.Select(x => new {
            First = NormalizeName(x.FirstName), Last = NormalizeName(x.LastName),
            Phone = NormalizePhone(x.Phone), ParentPhone = NormalizePhone(x.ParentPhone)
        }).ToList();
        if (!keys.Any(k => k.First.Length > 0 && k.Last.Length > 0 || k.Phone.Length > 0 || k.ParentPhone.Length > 0))
            return new([], "");
        // Keep the tenant query filter and include inactive players. Select only review details.
        var players = await db.Students.AsNoTracking().OrderBy(x => x.Id).Select(x => new {
            x.Id, x.FirstName, x.LastName, x.DateOfBirth, Team = x.TrainingTeam == null ? null : x.TrainingTeam.Name,
            x.ParentName, x.Phone, x.ParentPhone, x.IsActive
        }).ToListAsync();
        var matches = new List<StudentDuplicateMatch>();
        foreach (var player in players)
        {
            var name = keys.Any(k => k.First.Length > 0 && k.Last.Length > 0 && k.First == NormalizeName(player.FirstName) && k.Last == NormalizeName(player.LastName));
            var phones = new[] { NormalizePhone(player.Phone), NormalizePhone(player.ParentPhone) }.Where(x => x.Length > 0).ToHashSet();
            var phone = keys.Any(k => phones.Contains(k.Phone) || phones.Contains(k.ParentPhone));
            if (name || phone) matches.Add(new(player.Id, player.FirstName, player.LastName, player.DateOfBirth,
                player.Team, player.ParentName, player.Phone, player.ParentPhone, player.IsActive, name, phone));
        }
        // Acknowledgement is specific to these inputs and the exact records displayed.
        var token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { db.CurrentClubId, keys, matches }))));
        return new(matches, token);
    }

    public async Task RequireReviewAsync(IEnumerable<StudentDuplicateInput> input, bool confirmed, string? token)
    {
        var review = await FindAsync(input);
        if (review.Matches.Count > 0 && (!confirmed || token != review.Token)) throw new StudentDuplicateWarning(review);
    }
}
