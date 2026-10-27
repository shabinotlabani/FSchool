using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class FamilyService(ApplicationDbContext db)
{
    public const string BillingLock = "SELECT pg_advisory_xact_lock(20260929, 2002)";
    public async Task<Dictionary<int,string>> JoinBlocksAsync(IEnumerable<int> studentIds,int familyId,DateOnly month)
    {
        var ids=studentIds.ToList();
        var existing=familyId==0?new HashSet<int>():(await MembersAsync(familyId,month)).Select(x=>x.StudentId).ToHashSet();
        var states=await Latest(month).Include(x=>x.FeePlan).Where(x=>ids.Contains(x.StudentId)).ToListAsync();
        var current=await Latest(TariffService.CurrentMonth).Include(x=>x.FeePlan).Where(x=>ids.Contains(x.StudentId)).ToListAsync();
        var future=await db.StudentFeeAssignments.AsNoTracking().Where(x=>ids.Contains(x.StudentId)&&x.EffectiveMonth>month).ToListAsync();
        var result=new Dictionary<int,string>();
        var standardId=await new TariffService(db).StandardPlanIdAsync();
        foreach(var id in ids.Where(id=>!existing.Contains(id))){
            if(await db.Students.AnyAsync(s=>s.Id==id&&s.ContractPending)||await db.PlayerContracts.AnyAsync(c=>c.StudentId==id&&c.Status!="Draft"&&(c.EndedOn??c.EndsOn)>=month)){result[id]="Lojtar me kontratë";continue;}
            var assignments=states.Concat(current).Where(x=>x.StudentId==id).ToList();
            if(assignments.Any(x=>x.FamilyId.HasValue&&x.FamilyId!=familyId))result[id]="Është në një familje tjetër";
            else if(future.Any(x=>x.StudentId==id))result[id]="Ka ndryshim pakete të planifikuar";
            else if(assignments.Any(x=>x.FeePlanId!=standardId))result[id]="Ka paketë të veçantë: "+assignments.First(x=>x.FeePlanId!=standardId).FeePlan?.Name;
        }
        return result;
    }
    public async Task<int> PlanIdAsync(int familyId, DateOnly month) =>
        await db.FamilyTariffAssignments.AsNoTracking().Where(a => a.FamilyId == familyId && a.EffectiveMonth <= month)
            .OrderByDescending(a => a.EffectiveMonth).ThenByDescending(a => a.Id).Select(a => (int?)a.FeePlanId).FirstOrDefaultAsync() ?? await new TariffService(db).DefaultFamilyPlanIdAsync();
    public IQueryable<StudentFeeAssignment> Latest(DateOnly month) => db.StudentFeeAssignments.AsNoTracking()
        .Where(a=>a.EffectiveMonth<=month && !db.StudentFeeAssignments.Any(b=>b.StudentId==a.StudentId && b.EffectiveMonth<=month && (b.EffectiveMonth>a.EffectiveMonth || b.EffectiveMonth==a.EffectiveMonth && b.Id>a.Id)));
    public Task<List<StudentFeeAssignment>> MembersAsync(int familyId, DateOnly month) => Latest(month).Include(a=>a.Student)
        .Where(a=>a.FamilyId==familyId && a.FeePlan!.IsFamily).OrderBy(a=>a.FamilyOrder).ThenBy(a=>a.StudentId).ToListAsync();

    public async Task<int> SaveAsync(FamilyEditModel input, string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        TariffService.TryMonth(input.EffectiveMonth,out var month);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(BillingLock);
        var family=input.Id==0?new PlayerFamily():await db.PlayerFamilies.SingleOrDefaultAsync(f=>f.Id==input.Id)
            ??throw new InvalidOperationException("Familja nuk u gjet.");
        if(input.Id!=0 && family.Revision!=input.Revision)throw new InvalidOperationException("Familja ka ndryshuar. Rihapni formularin para ruajtjes.");
        var selectedPlan = await db.FeePlans.SingleOrDefaultAsync(p => p.Id == input.FeePlanId && p.IsFamily)
            ?? throw new InvalidOperationException("Zgjidhni një tarifë familjare.");
        var previousPlanId = input.Id == 0 ? 0 : await PlanIdAsync(input.Id, month);
        if (!selectedPlan.IsActive && previousPlanId != selectedPlan.Id)
            throw new InvalidOperationException("Kjo tarifë është hequr nga përdorimi. Zgjidhni një tarifë aktive.");
        if(await db.PlayerFamilies.AnyAsync(f=>f.Id!=input.Id && f.Name.ToUpper()==input.Name.Trim().ToUpper()))
            throw new InvalidOperationException("Ky emër familjeje ekziston. Përdorni një emër dallues.");
        var members=input.Id==0?new List<StudentFeeAssignment>():await MembersAsync(input.Id,month);
        var affected=members.Select(m=>m.StudentId).Union(input.StudentIds).ToList();
        var blocks=await JoinBlocksAsync(input.StudentIds,input.Id,month);
        if(blocks.Count>0){var blocked=await db.Students.AsNoTracking().FirstAsync(s=>s.Id==blocks.First().Key);throw new InvalidOperationException(blocked.FullName+": "+blocks.First().Value+". Shtimi në këtë familje nuk lejohet.");}
        var additions=input.StudentIds.Except(members.Select(x=>x.StudentId)).ToList();
        if(additions.Count>0&&!input.ConfirmIndividualTransfer)
            throw new InvalidOperationException("Konfirmoni kalimin e lojtarëve të rinj nga tarifa individuale në paketën familjare.");
        await new BillingService(db).EnsureNoSeasonalChangeAsync(affected, month);
        if(await db.StudentFeeAssignments.AnyAsync(a=>(affected.Contains(a.StudentId)||a.FamilyId==input.Id) && a.EffectiveMonth>month) || await db.FamilyTariffAssignments.AnyAsync(a=>a.FamilyId==input.Id && a.EffectiveMonth>month))
            throw new InvalidOperationException("Ka ndryshime të planifikuara pas këtij muaji. Zgjidhni muajin e fundit të planifikuar ose një muaj më vonë.");
        var students=await db.Students.AsNoTracking().Where(s=>input.StudentIds.Contains(s.Id)).ToDictionaryAsync(s=>s.Id);
        if(students.Count!=input.StudentIds.Count || students.Values.Any(s=>!s.IsActive && !members.Any(m=>m.StudentId==s.Id)))
            throw new InvalidOperationException("Për shtim në familje zgjidhni lojtarë aktivë.");
        if(await Latest(month).AnyAsync(a=>input.StudentIds.Contains(a.StudentId) && a.FamilyId!=null && a.FamilyId!=input.Id))
            throw new InvalidOperationException("Një fëmijë është në një familje tjetër. Hiqeni fillimisht nga ajo familje për të njëjtin muaj.");
        if(await db.MonthlyFeePeriods.AnyAsync(p=>affected.Contains(p.StudentId) && (p.Year>month.Year || p.Year==month.Year && p.Month>=month.Month)))
            throw new InvalidOperationException("Muaji ose një muaj pas tij është faturuar. Zgjidhni muajin pas periudhës së fundit të faturuar.");
        if(input.Id==0)db.PlayerFamilies.Add(family);
        family.PreferredPaymentMethod=input.PreferredPaymentMethod;family.Name=input.Name.Trim();family.Phone=input.Phone?.Trim();family.Revision=Guid.NewGuid();
        await db.SaveChangesAsync();
        db.FamilyTariffAssignments.Add(new() { FamilyId = family.Id, FeePlanId = input.FeePlanId,
            EffectiveMonth = input.Id == 0 ? TariffService.CurrentMonth : month, CreatedByUserId = actor });
        foreach(var previous in members.Where(m=>!input.StudentIds.Contains(m.StudentId)))
            db.StudentFeeAssignments.Add(new(){StudentId=previous.StudentId,FeePlanId=await new TariffService(db).StandardPlanIdAsync(),EffectiveMonth=month,Reason=input.Reason.Trim(),Notes="Largim nga familja "+family.Name,CreatedByUserId=actor});
        for(var i=0;i<input.StudentIds.Count;i++)
            db.StudentFeeAssignments.Add(new(){StudentId=input.StudentIds[i],FeePlanId=input.FeePlanId,FamilyId=family.Id,FamilyOrder=i+1,EffectiveMonth=month,Reason=input.Reason.Trim(),Notes=$"Familja {family.Name} · Renditja {i+1}",CreatedByUserId=actor});
        db.FamilyChanges.Add(new(){FamilyId=family.Id,EffectiveMonth=month,Reason=input.Reason.Trim(),ActorId=actor,
            Snapshot=$"Pagesa e zakonshme: {BillingService.MethodLabel(family.PreferredPaymentMethod)}. Tarifa: {selectedPlan.Name}. " +(additions.Count>0?"Kalimi individual në familje u konfirmua për: "+string.Join(", ",additions)+". ":"")+ string.Join("; ",input.StudentIds.Select((id,i)=>$"{i+1}. {students[id].FullName} (#{id})"))+(input.StudentIds.Count==0?"Pa fëmijë në paketë":"")});
        await db.SaveChangesAsync();await new BankVerificationService(db).SyncAutomaticAsync();await tx.CommitAsync();return family.Id;
    }

    // Called inside registration's transaction and billing lock. New children
    // are appended; already-issued monthly documents remain immutable.
    public async Task JoinRegistrationAsync(int familyId, Student student, string actor)
    {
        if(db.Database.CurrentTransaction==null)throw new InvalidOperationException("Kërkohet transaksioni i regjistrimit.");
        await db.Database.ExecuteSqlRawAsync(BillingLock);
        var family=await db.PlayerFamilies.SingleOrDefaultAsync(f=>f.Id==familyId)??throw new InvalidOperationException("Zgjidhni familjen e regjistruar.");
        var month=TariffService.CurrentMonth;
        if(await db.StudentFeeAssignments.AnyAsync(x=>x.StudentId==student.Id))throw new InvalidOperationException("Lojtari ka paketë ose familje ekzistuese. Ky veprim lejohet vetëm gjatë regjistrimit të ri.");
        var members=await MembersAsync(familyId,month);
        if(members.Count>=20)throw new InvalidOperationException("Familja ka arritur 20 anëtarë.");
        if(await db.StudentFeeAssignments.AnyAsync(a=>a.FamilyId==familyId && a.EffectiveMonth>month) || await db.FamilyTariffAssignments.AnyAsync(a=>a.FamilyId==familyId && a.EffectiveMonth>month))
            throw new InvalidOperationException("Familja ka një përbërje të planifikuar. Regjistrojeni me tarifë normale dhe shtojeni nga menaxhimi i familjes për muajin e planifikuar.");
        var order=members.Select(m=>m.FamilyOrder??0).DefaultIfEmpty().Max()+1;
        db.StudentFeeAssignments.Add(new(){StudentId=student.Id,FeePlanId=await PlanIdAsync(familyId,month),FamilyId=familyId,FamilyOrder=order,EffectiveMonth=month,Reason="Regjistrim i ri në familje",Notes=$"Familja {family.Name} · Renditja {order}",CreatedByUserId=actor});
        family.Revision=Guid.NewGuid();
        db.FamilyChanges.Add(new(){FamilyId=familyId,EffectiveMonth=month,Reason="Regjistrim i ri",ActorId=actor,Snapshot=$"Shtuar: {student.FullName} (#{student.Id}), renditja {order}. Faturat e lëshuara nuk ndryshohen."});
        await db.SaveChangesAsync();
    }

    public async Task<FamilyDetailsModel?> DetailsAsync(int id, DateOnly month)
    {
        await using var snapshot=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead):null;
        var family=await db.PlayerFamilies.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==id);if(family==null)return null;
        var result=new FamilyDetailsModel{Family=family,Month=month,History=await db.FamilyChanges.AsNoTracking().Include(c=>c.Actor).Where(c=>c.FamilyId==id).OrderByDescending(c=>c.CreatedAt).ThenByDescending(c=>c.Id).ToListAsync()};
        var members=await MembersAsync(id,month);var tariffs=new TariffService(db);
        for(var i=0;i<members.Count;i++)result.Members.Add(new(members[i].Student!,i+1,await tariffs.ResolveAsync(members[i].Student!,month)));
        if(snapshot!=null)await snapshot.CommitAsync();
        return result;
    }
}
