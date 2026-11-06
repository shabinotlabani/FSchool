using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public record ParentAccess(PlayerFamily? Family,List<Student> Students);
public class ParentAccessService(ApplicationDbContext db)
{
    public async Task<string?> EnsureLinkFor(int studentId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        var link = await LinkFor(studentId);
        if (link == null) return null;
        var token = link.Token;
        if (string.IsNullOrEmpty(token))
        {
            token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            if (link.Family != null)
                (await db.PlayerFamilies.SingleAsync(x => x.Id == link.Family.Id)).ParentLinkToken = token;
            else
                (await db.Students.SingleAsync(x => x.Id == studentId)).ParentLinkToken = token;
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return token;
    }

    public async Task<ParentAccess?> Resolve(string token)
    {
        if(token.Length!=64||token.Any(c=>!char.IsAsciiHexDigit(c)))return null;
        var family=await db.PlayerFamilies.AsNoTracking().SingleOrDefaultAsync(x=>x.ParentLinkToken==token);
        if(family!=null){
            // Membership is always current, never selected by the report's historical month.
            var members=await new FamilyService(db).MembersAsync(family.Id,TariffService.CurrentMonth);
            return members.Count==0?null:new(family,members.Select(x=>x.Student!).ToList());
        }
        var student=await db.Students.AsNoTracking().SingleOrDefaultAsync(x=>x.ParentLinkToken==token);
        return student==null?null:new(null,[student]);
    }
    public async Task<ParentLinkView?> LinkFor(int studentId)
    {
        var student=await db.Students.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==studentId);if(student==null)return null;
        var familyId=await new FamilyService(db).Latest(TariffService.CurrentMonth).Where(x=>x.StudentId==studentId&&x.FeePlan!.IsFamily).Select(x=>x.FamilyId).SingleOrDefaultAsync();
        var family=familyId.HasValue?await db.PlayerFamilies.AsNoTracking().SingleAsync(x=>x.Id==familyId):null;
        return new(){Student=student,Family=family,Members=family==null?[student]:(await new FamilyService(db).MembersAsync(family.Id,TariffService.CurrentMonth)).Select(x=>x.Student!).ToList()};
    }
}
