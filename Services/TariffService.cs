using System.ComponentModel.DataAnnotations;
using System.Globalization;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class TariffService(ApplicationDbContext db)
{
    public async Task<int> CreateAsync(CreateTariffModel input, string actor)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        if (await db.FeePlans.AnyAsync(p => p.Name.ToUpper() == input.Name.Trim().ToUpper()))
            throw new InvalidOperationException("Ekziston një tarifë me këtë emër.");
        var plan = new FeePlan { Name = input.Name.Trim(), IsFamily = input.IsFamily };
        db.FeePlans.Add(plan);
        await db.SaveChangesAsync();
        db.FeePlanPrices.Add(new FeePlanPrice { FeePlanId = plan.Id, EffectiveMonth = CurrentMonth,
            Amount = input.Amount, FamilyFirstAmount = input.IsFamily ? input.Amount : null,
            FamilyAdditionalAmount = input.IsFamily ? input.FamilyAdditionalAmount : null,
            FamilyFirstCount = input.FamilyFirstCount, SingleUsesStandard = input.SingleUsesStandard,
            Reason = "Krijim tarife", CreatedByUserId = actor });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return plan.Id;
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        var plan = await db.FeePlans.FindAsync(id) ?? throw new InvalidOperationException("Tarifa nuk u gjet.");
        plan.IsActive = active;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    public static DateOnly CurrentMonth => new(BillingClock.Today.Year,BillingClock.Today.Month,1);
    public static DateOnly NextMonth => CurrentMonth.AddMonths(1);
    public static bool TryMonth(string? input,out DateOnly month) => DateOnly.TryParseExact(input+"-01","yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out month);
    public async Task<List<FeePlanOption>> OptionsAsync(DateOnly? month=null, bool includeInactive=false)
    {
        var date=month??CurrentMonth;
        var plans=await db.FeePlans.AsNoTracking().Where(p=>includeInactive || p.IsActive).OrderBy(p=>p.Id).ToListAsync();
        var prices=await db.FeePlanPrices.AsNoTracking().Where(p=>p.EffectiveMonth<=date).ToListAsync();
        return plans.Where(p=>prices.Any(v=>v.FeePlanId==p.Id)).Select(p=>{var price=prices.Where(v=>v.FeePlanId==p.Id).OrderByDescending(v=>v.EffectiveMonth).ThenByDescending(v=>v.Id).First();return new FeePlanOption(p.Id,p.Name,p.IsFamily?price.FamilyFirstAmount??40:price.Amount,p.IsWaiver,price.FamilyAdditionalAmount??35,price.FamilyFirstCount,price.SingleUsesStandard,p.IsFamily,p.IsActive);}).ToList();
    }
    public async Task<ResolvedFee> ResolveAsync(Student student,DateOnly month)
    {
        var assignment=await db.StudentFeeAssignments.AsNoTracking().Include(a=>a.FeePlan).Where(a=>a.StudentId==student.Id && a.EffectiveMonth<=month).OrderByDescending(a=>a.EffectiveMonth).ThenByDescending(a=>a.Id).FirstOrDefaultAsync();
        if(assignment==null) return new(student.MonthlyFee,"Tarifë ekzistuese",false,null,null);
        var price=await db.FeePlanPrices.AsNoTracking().Where(p=>p.FeePlanId==assignment.FeePlanId && p.EffectiveMonth<=month).OrderByDescending(p=>p.EffectiveMonth).ThenByDescending(p=>p.Id).FirstAsync();
        if(assignment.FeePlan!.IsFamily && assignment.FamilyId.HasValue)
        {
            var members=await new FamilyService(db).MembersAsync(assignment.FamilyId.Value,month);
            var position=members.FindIndex(a=>a.StudentId==student.Id)+1;
            if(position==0)throw new InvalidOperationException("Anëtarësimi në familje nuk është i vlefshëm.");
            var amount=position<=price.FamilyFirstCount?price.FamilyFirstAmount??40:price.FamilyAdditionalAmount??35;
            if(members.Count==1 && price.SingleUsesStandard)
                amount=await db.FeePlanPrices.Where(p=>p.FeePlanId==1 && p.EffectiveMonth<=month).OrderByDescending(p=>p.EffectiveMonth).ThenByDescending(p=>p.Id).Select(p=>p.Amount).FirstAsync();
            var family=await db.PlayerFamilies.AsNoTracking().SingleAsync(f=>f.Id==assignment.FamilyId);
            var familyLabel=family.Name.Length>45?family.Name[..45]+"…":family.Name;
            return new(amount,$"Familjare · {familyLabel} · Fëmija {position}/{members.Count}",false,assignment.Reason,$"{assignment.Notes} · Paketa: {price.FamilyFirstCount} të parët nga {price.FamilyFirstAmount??40:0.##} €, të tjerët nga {price.FamilyAdditionalAmount??35:0.##} €." );
        }
        return new(price.Amount,assignment.FeePlan!.Name+(assignment.FeePlan!.IsFamily?" (tarifë e mëparshme, pa familje)":""),assignment.FeePlan.IsWaiver,assignment.Reason,assignment.Notes);
    }
    public async Task ChangePriceAsync(FeePriceModel input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        var plan=await db.FeePlans.FindAsync(input.FeePlanId)??throw new InvalidOperationException("Tarifa nuk u gjet.");
        input.IsFamily=plan.IsFamily;
        Validator.ValidateObject(input,new ValidationContext(input),true);
        if(plan.IsWaiver)throw new InvalidOperationException("Lirimi nga pagesa ka gjithmonë vlerën zero.");
        TryMonth(input.EffectiveMonth,out var month);
        var latest=await db.FeePlanPrices.Where(p=>p.FeePlanId==plan.Id && p.EffectiveMonth==month).OrderByDescending(p=>p.Id).FirstOrDefaultAsync();
        if(latest!=null && (plan.IsFamily?latest.FamilyFirstAmount==input.Amount && latest.FamilyAdditionalAmount==input.FamilyAdditionalAmount && latest.FamilyFirstCount==input.FamilyFirstCount && latest.SingleUsesStandard==input.SingleUsesStandard:latest.Amount==input.Amount) && latest.Reason==input.Reason.Trim())return;
        var legacyAmount=plan.IsFamily?await db.FeePlanPrices.Where(p=>p.FeePlanId==plan.Id && p.EffectiveMonth<=month).OrderByDescending(p=>p.EffectiveMonth).ThenByDescending(p=>p.Id).Select(p=>p.Amount).FirstAsync():input.Amount;
        db.FeePlanPrices.Add(new(){FeePlanId=plan.Id,EffectiveMonth=month,Amount=legacyAmount,FamilyFirstAmount=plan.IsFamily?input.Amount:null,FamilyAdditionalAmount=plan.IsFamily?input.FamilyAdditionalAmount:null,FamilyFirstCount=input.FamilyFirstCount,SingleUsesStandard=input.SingleUsesStandard,Reason=input.Reason.Trim(),CreatedByUserId=actor});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task AssignAsync(AssignFeeModel input,string actor)
    {
        Validator.ValidateObject(input,new ValidationContext(input),true);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
        if(!await db.Students.AnyAsync(s=>s.Id==input.StudentId))throw new InvalidOperationException("Lojtari nuk u gjet.");
        if(!await db.FeePlans.AnyAsync(p=>p.Id==input.FeePlanId && p.IsActive))throw new InvalidOperationException("Tarifa nuk u gjet.");
        TryMonth(input.EffectiveMonth,out var month);
        await new BillingService(db).EnsureNoSeasonalChangeAsync([input.StudentId], month);
        if((await db.FeePlans.SingleAsync(p=>p.Id==input.FeePlanId)).IsFamily)throw new InvalidOperationException("Paketa familjare caktohet te Familjet, duke zgjedhur fëmijët dhe renditjen.");
        if(await new FamilyService(db).Latest(month).AnyAsync(a=>a.StudentId==input.StudentId && a.FamilyId!=null))
            throw new InvalidOperationException("Hiqeni fillimisht lojtarin nga paketa te Familjet, për të njëjtin muaj, pastaj caktoni kategorinë e re.");
        var latest=await db.StudentFeeAssignments.Where(a=>a.StudentId==input.StudentId && a.EffectiveMonth==month).OrderByDescending(a=>a.Id).FirstOrDefaultAsync();
        if(latest!=null && latest.FeePlanId==input.FeePlanId && latest.Reason==input.Reason.Trim() && latest.Notes==input.Notes?.Trim())return;
        db.StudentFeeAssignments.Add(new(){StudentId=input.StudentId,FeePlanId=input.FeePlanId,EffectiveMonth=month,Reason=input.Reason.Trim(),Notes=input.Notes?.Trim(),CreatedByUserId=actor});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
