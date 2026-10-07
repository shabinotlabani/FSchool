using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class StudentWorkflowService(ApplicationDbContext db)
{
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    public async Task<int> RegisterFamilyAsync(FamilyRegistrationModel input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        foreach(var child in input.Children)Validator.ValidateObject(child,new ValidationContext(child),true);
        var payload=JsonSerializer.Serialize(new{input.FamilyId,FeePlanId=input.FamilyId.HasValue?0:input.FeePlanId,FamilyName=input.FamilyId.HasValue?null:Clean(input.FamilyName),ParentName=Clean(input.ParentName),ParentPhone=Clean(input.ParentPhone),ParentEmail=Clean(input.ParentEmail),Children=input.Children.Select(c=>new{FirstName=c.FirstName.Trim(),LastName=c.LastName.Trim(),c.DateOfBirth,c.TrainingTeamId,c.FirstMonthAmount,FirstMonthReason=Clean(c.FirstMonthReason),Notes=Clean(c.Notes)})});
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        var previous=await db.FamilyRegistrations.AsNoTracking().SingleOrDefaultAsync(r=>r.RequestId==input.RequestId);
        if(previous!=null)
        {
            if(previous.ActorId!=actor || previous.PayloadHash!=hash)throw new InvalidOperationException("Ky formular është ruajtur. Hapni një regjistrim të ri për të dhëna të tjera.");
            return previous.FamilyId;
        }
        // Hold the same locks and order as individual registration. Each saved
        // child reserves a team seat before the following child is checked.
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        PlayerFamily family;
        if(input.FamilyId.HasValue)
            family=await db.PlayerFamilies.SingleOrDefaultAsync(f=>f.Id==input.FamilyId)??throw new InvalidOperationException("Familja nuk u gjet.");
        else
        {
            if (!await db.FeePlans.AnyAsync(p => p.Id == input.FeePlanId && p.IsFamily && p.IsActive))
                throw new InvalidOperationException("Zgjidhni një tarifë familjare aktive.");
            var name=input.FamilyName!.Trim();
            if(await db.PlayerFamilies.AnyAsync(f=>f.Name.ToUpper()==name.ToUpper()))throw new InvalidOperationException("Familja ekziston. Zgjidheni nga lista ose shkruani një emër dallues.");
            family=new(){Name=name,Phone=Clean(input.ParentPhone)};db.PlayerFamilies.Add(family);await db.SaveChangesAsync();
            db.FamilyTariffAssignments.Add(new() { FamilyId = family.Id, FeePlanId = input.FeePlanId,
                EffectiveMonth = TariffService.CurrentMonth, CreatedByUserId = actor });
            await db.SaveChangesAsync();
        }
        var familyService=new FamilyService(db);
        if((await familyService.MembersAsync(family.Id,TariffService.CurrentMonth)).Count+input.Children.Count>20)
            throw new InvalidOperationException("Familja nuk mund të ketë mbi 20 fëmijë në paketë.");
        var students=new List<Student>();
        foreach(var child in input.Children)
        {
            var team=await new TeamService(db).ValidatePlacementAsync(child.TrainingTeamId,child.DateOfBirth!.Value);
            var student=new Student{FirstMonthUsesWeeks=true,FirstName=child.FirstName.Trim(),LastName=child.LastName.Trim(),DateOfBirth=child.DateOfBirth.Value,ParentName=Clean(input.ParentName),ParentPhone=Clean(input.ParentPhone),ParentEmail=Clean(input.ParentEmail),Notes=Clean(child.Notes),TrainingTeamId=team?.Id};
            db.Students.Add(student);await db.SaveChangesAsync();
            if(team!=null)db.StudentTeamChanges.Add(new(){StudentId=student.Id,ToTeamId=team.Id,ToTeamName=team.Name,Reason="Regjistrim familjar",ActorId=actor});
            await familyService.JoinRegistrationAsync(family.Id,student,actor);
            students.Add(student);
        }
        // All siblings are members BEFORE the first monthly charge is resolved.
        var billing=new BillingService(db);var tariffs=new TariffService(db);var today=BillingClock.Today;
        foreach(var student in students)
        {
            student.MonthlyFee=(await tariffs.ResolveAsync(student,TariffService.CurrentMonth)).Amount;
            var periodId=await billing.GenerateStudentMonthAsync(student.Id,today.Year,today.Month,actor);
            var child=input.Children[students.IndexOf(student)];
            await billing.ApplyFirstMonthOverrideAsync(periodId,child.FirstMonthAmount,child.FirstMonthReason,actor);
        }
        db.FamilyRegistrations.Add(new(){RequestId=input.RequestId,PayloadHash=hash,FamilyId=family.Id,ActorId=actor});
        await db.SaveChangesAsync();await tx.CommitAsync();return family.Id;
    }

    public async Task EditAsync(StudentEditModel input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var student=await db.Students.SingleOrDefaultAsync(s=>s.Id==input.Id)??throw new InvalidOperationException("Lojtari nuk u gjet.");
        await db.Entry(student).ReloadAsync();
        if(student.Revision!=input.Revision)throw new InvalidOperationException("Të dhënat kanë ndryshuar ndërkohë. Rihapni editimin për të mos mbishkruar ndryshimet.");
        if(!input.IsActive && input.TrainingTeamId.HasValue && student.TrainingTeamId!=input.TrainingTeamId)
            throw new InvalidOperationException("Lojtari joaktiv nuk mund të caktohet në ekip të ri.");
        if(input.IsActive && (!student.IsActive || student.TrainingTeamId!=input.TrainingTeamId || student.DateOfBirth!=input.DateOfBirth))
            await new TeamService(db).ValidatePlacementAsync(input.TrainingTeamId,input.DateOfBirth!.Value,student.Id);
        static object Snapshot(Student s)=>new{s.FirstName,s.LastName,s.DateOfBirth,s.ParentName,s.ParentPhone,s.ParentEmail,s.Notes,s.IsActive,s.TrainingTeamId};
        var before=JsonSerializer.Serialize(Snapshot(student));var changes=new List<string>();
        if(student.FirstName!=input.FirstName.Trim()||student.LastName!=input.LastName.Trim())changes.Add("Emri / mbiemri");
        if(student.DateOfBirth!=input.DateOfBirth)changes.Add("Datëlindja");
        if(student.ParentName!=Clean(input.ParentName)||student.ParentPhone!=Clean(input.ParentPhone)||student.ParentEmail!=Clean(input.ParentEmail))changes.Add("Të dhënat e prindit");
        if(student.Notes!=Clean(input.Notes))changes.Add("Shënimet");
        if(student.IsActive!=input.IsActive)changes.Add(input.IsActive?"Riaktivizim":"Çaktivizim");
        if(student.TrainingTeamId!=input.TrainingTeamId)
        {
            changes.Add("Ekipi");
            var oldName=await db.TrainingTeams.Where(t=>t.Id==student.TrainingTeamId).Select(t=>t.Name).FirstOrDefaultAsync();
            var newName=await db.TrainingTeams.Where(t=>t.Id==input.TrainingTeamId).Select(t=>t.Name).FirstOrDefaultAsync();
            db.StudentTeamChanges.Add(new(){StudentId=student.Id,FromTeamId=student.TrainingTeamId,FromTeamName=oldName,ToTeamId=input.TrainingTeamId,ToTeamName=newName,Reason=input.Reason.Trim(),ActorId=actor});
        }
        if(changes.Count==0)return;
        student.FirstName=input.FirstName.Trim();student.LastName=input.LastName.Trim();student.DateOfBirth=input.DateOfBirth!.Value;
        student.ParentName=Clean(input.ParentName);student.ParentEmail=Clean(input.ParentEmail);student.ParentPhone=Clean(input.ParentPhone);student.Notes=Clean(input.Notes);
        student.IsActive=input.IsActive;student.TrainingTeamId=input.TrainingTeamId;student.UpdatedAt=DateTime.UtcNow;student.Revision=Guid.NewGuid();
        db.StudentChanges.Add(new(){StudentId=student.Id,ActorId=actor,Reason=input.Reason.Trim(),Before=before,After=JsonSerializer.Serialize(Snapshot(student)),Summary=string.Join("; ",changes)});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
