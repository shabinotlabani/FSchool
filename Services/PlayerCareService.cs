using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class PlayerCareService(ApplicationDbContext db)
{
    public async Task<string> Kind(string actor)
    {
        var user=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor);
        if(user==null||UserAdministrationService.IsDisabled(user))throw new UnauthorizedAccessException();
        var roles=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==actor select r.Name).ToListAsync();
        if(roles.Contains(Roles.Doctor))return Roles.Doctor;
        if(roles.Contains(Roles.Nutritionist))return Roles.Nutritionist;
        throw new UnauthorizedAccessException();
    }
    public IQueryable<CareAssignment> Assignments(string actor,string kind)=>db.CareAssignments.Where(x=>x.Active&&x.StaffId==actor&&x.Kind==kind&&x.Student.IsActive);
    public async Task<CareAssignment> Assigned(int student,string actor)
    {
        var kind=await Kind(actor);
        return await Assignments(actor,kind).Include(x=>x.Student.TrainingTeam).SingleOrDefaultAsync(x=>x.StudentId==student)??throw new UnauthorizedAccessException();
    }
    public async Task Save(CareInput input,string actor)
    {
        Validator.ValidateObject(input,new(input),true);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2022)");
        var assignment=await Assigned(input.StudentId,actor);
        if(input.Date==default||input.Date>BillingClock.Today||input.ReviewDate<input.Date)throw new ValidationException("Kontrolloni datën e vizitës dhe rikontrollit.");
        if(input.RequestId==Guid.Empty)throw new ValidationException("Rihapni formularin.");
        if(await db.CareEntries.AnyAsync(x=>x.RequestId==input.RequestId)){await tx.CommitAsync();return;}
        if(assignment.Kind==Roles.Doctor&&input.TrainingStatus is not ("Ready" or "Limited" or "Unavailable"))throw new ValidationException("Zgjidhni statusin për stërvitje.");
        if(input.ShareWithParent&&string.IsNullOrWhiteSpace(input.ParentInstructions))throw new ValidationException("Shkruani udhëzimin që do ta shohë prindi.");
        db.CareEntries.Add(new(){AssignmentId=assignment.Id,RequestId=input.RequestId,Date=input.Date,Subject=input.Subject.Trim(),PrivateNotes=input.PrivateNotes?.Trim(),TrainingStatus=assignment.Kind==Roles.Doctor?input.TrainingStatus:null,
            CoachInstructions=assignment.Kind==Roles.Doctor?input.CoachInstructions?.Trim():null,ReviewDate=input.ReviewDate,ShareWithParent=input.ShareWithParent,ParentInstructions=input.ParentInstructions?.Trim(),CreatedAt=DateTime.UtcNow});
        if(assignment.Kind==Roles.Nutritionist&&(input.Weight.HasValue||input.Height.HasValue)){
            var values=new Dictionary<string,string>();if(input.Weight.HasValue)values["Weight"]=input.Weight.Value.ToString(CultureInfo.InvariantCulture);if(input.Height.HasValue)values["Height"]=input.Height.Value.ToString(CultureInfo.InvariantCulture);
            db.DevelopmentRecords.Add(new(){StudentId=input.StudentId,CoachId=actor,Kind="Measurements",Date=input.Date,Data=JsonSerializer.Serialize(values),CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow});
        }
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    // Public projections deliberately never select subjects or private notes.
    private IQueryable<CareEntry> Current()=>db.CareEntries.AsNoTracking().Where(x=>x.Assignment.Active&&x.Assignment.Student.IsActive&&
        (x.Assignment.Staff.LockoutEnd==null||x.Assignment.Staff.LockoutEnd<DateTimeOffset.UtcNow)&&
        db.UserRoles.Any(ur=>ur.UserId==x.Assignment.StaffId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name==x.Assignment.Kind)));
    public async Task<List<CareNotice>> TrainingNotices(List<int> ids,DateOnly? onDate=null)
    {
        var date=onDate??BillingClock.Today;
        // Medical restrictions persist after staff reassignment until a new assessment releases them.
        var rows=await db.CareEntries.AsNoTracking().Where(x=>ids.Contains(x.Assignment.StudentId)&&x.Assignment.Kind==Roles.Doctor&&x.Date<=date)
            .OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id)
            .Select(x=>new CareNotice(x.Assignment.StudentId,x.TrainingStatus!,x.CoachInstructions,x.Date,x.ReviewDate)).ToListAsync();
        return rows.GroupBy(x=>x.StudentId).Select(x=>x.First()).ToList();
    }
    public async Task<List<ParentCareNotice>> ParentNotices(int id)
    {
        var rows=await Current().Where(x=>x.Assignment.StudentId==id).OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id)
            .Select(x=>new{x.AssignmentId,x.Assignment.Kind,x.ShareWithParent,x.ParentInstructions,x.Date}).ToListAsync();
        return rows.GroupBy(x=>x.AssignmentId).Select(g=>g.First()).Where(x=>x.ShareWithParent&&!string.IsNullOrWhiteSpace(x.ParentInstructions))
            .Select(x=>new ParentCareNotice(x.Kind,x.ParentInstructions!,x.Date)).ToList();
    }
}
