using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Read)]
public class StudentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly FinancialService _financialService;

    public StudentsController(ApplicationDbContext context, FinancialService financialService)
    {
        _context = context;
        _financialService = financialService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        search = search?.Trim();
        ViewData["Search"] = search;
        var query = _context.Students.AsNoTracking().Include(s=>s.TrainingTeam).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            foreach (var term in search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                // Treat wildcard characters as literal user input.
                var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                query = query.Where(x => EF.Functions.ILike(x.FirstName + " " + x.LastName, pattern)
                    || (x.ParentName != null && EF.Functions.ILike(x.ParentName, pattern))
                    || (x.ParentPhone != null && EF.Functions.ILike(x.ParentPhone, pattern)));
            }
        }

        var students = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync();

        var model = new List<StudentListItem>();
        foreach (var student in students)
        {
            var balance = await _financialService.GetCurrentBalanceAsync(student.Id);
            var tariff = await new TariffService(_context).ResolveAsync(student, TariffService.CurrentMonth);
            student.MonthlyFee = tariff.Amount;
            model.Add(new StudentListItem
            {
                Student = student,
                FeePlanName = tariff.Name,
                Balance = balance,
                Debt = Math.Max(balance, 0),
                Advance = Math.Max(-balance, 0)
            });
        }

        return View(model);
    }

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Create()
    {
        var model=new StudentRegistrationModel();await RegistrationChoices(model);return View(model);
    }
    private async Task RegistrationChoices(StudentRegistrationModel model)
    {
        model.FeePlans=await AvailablePlans();
        model.StandardPrice=(await new TariffService(_context).OptionsAsync(includeInactive:true)).Single(p=>p.Id==1).Amount;
        model.Families=await _context.PlayerFamilies.AsNoTracking().OrderBy(f=>f.Name).ToListAsync();
        foreach(var family in model.Families){model.FamilyCounts[family.Id]=(await new FamilyService(_context).MembersAsync(family.Id,TariffService.CurrentMonth)).Count;model.FamilyPlanIds[family.Id]=await new FamilyService(_context).PlanIdAsync(family.Id,TariffService.CurrentMonth);}
    }

    private async Task<List<FeePlanOption>> AvailablePlans() => (await new TariffService(_context).OptionsAsync()).Where(p=>!p.IsWaiver || User.IsInRole(Roles.Admin)).ToList();

    [HttpPost, Authorize(Roles = Roles.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentRegistrationModel model)
    {
        await RegistrationChoices(model);
        if(model.DateOfBirth.HasValue)model.SuggestedTeams=await new TeamService(_context).OptionsAsync(model.DateOfBirth);
        if(model.FeePlanId==3 && (!User.IsInRole(Roles.Admin) || string.IsNullOrWhiteSpace(model.FeeReason) || string.IsNullOrWhiteSpace(model.FeeNotes)))
            ModelState.AddModelError("", "Lirimi kërkon administratorin, arsyen dhe shënimin.");
        if (!ModelState.IsValid) return View(model);
        var now = DateTime.UtcNow;
        var student = new Student
        {
            FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(),
            DateOfBirth = model.DateOfBirth!.Value,
            ParentName = Clean(model.ParentName), ParentEmail = Clean(model.ParentEmail),
            ParentPhone = Clean(model.ParentPhone),
            Notes = Clean(model.Notes), IsActive = true,
            FirstMonthUsesWeeks = true, RegistrationDate = now, CreatedAt = now, UpdatedAt = now
        };
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2002)");
            var team=await new TeamService(_context).ValidatePlacementAsync(model.TrainingTeamId,student.DateOfBirth);
            student.TrainingTeamId=team?.Id;
            var plan = (await new TariffService(_context).OptionsAsync()).SingleOrDefault(p=>p.Id==model.FeePlanId)
                ?? throw new InvalidOperationException("Zgjidhni një tarifë të vlefshme.");
            if(plan.IsFamily && (!model.FamilyId.HasValue || await new FamilyService(_context).PlanIdAsync(model.FamilyId.Value,TariffService.CurrentMonth)!=plan.Id))
                throw new InvalidOperationException("Zgjidhni familjen me tarifën e zgjedhur.");
            student.MonthlyFee = plan.Amount;
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            if(team!=null)_context.StudentTeamChanges.Add(new StudentTeamChange{StudentId=student.Id,ToTeamId=team.Id,ToTeamName=team.Name,Reason="Regjistrim i ri",ActorId=User.FindFirstValue(ClaimTypes.NameIdentifier)});
            if(plan.IsFamily)await new FamilyService(_context).JoinRegistrationAsync(model.FamilyId??0,student,User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            else _context.StudentFeeAssignments.Add(new StudentFeeAssignment { StudentId=student.Id,FeePlanId=plan.Id,EffectiveMonth=TariffService.CurrentMonth,Reason=plan.IsWaiver?model.FeeReason!.Trim():"Tarifa e zgjedhur në regjistrim",Notes=Clean(model.FeeNotes),CreatedByUserId=User.FindFirstValue(ClaimTypes.NameIdentifier) });
            await _context.SaveChangesAsync();
            var today = BillingClock.Today;
            var billing=new BillingService(_context);
            var periodId=await billing.GenerateStudentMonthAsync(student.Id,today.Year,today.Month,User.FindFirstValue(ClaimTypes.NameIdentifier));
            await billing.ApplyFirstMonthOverrideAsync(periodId,model.FirstMonthAmount,model.FirstMonthReason,User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await transaction.CommitAsync();
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty,ex.Message); return View(model); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Regjistrimi nuk u ruajt. Provoni përsëri.");
            return View(model);
        }
        TempData["Success"] = $"Lojtari {student.FullName} u regjistrua dhe fatura e muajit u krijua automatikisht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> CreateFamily()
    {
        var model=new FamilyRegistrationModel();await FamilyChoices(model);return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> CreateFamily(FamilyRegistrationModel model)
    {
        if(ModelState.IsValid)try
        {
            var id=await new StudentWorkflowService(_context).RegisterFamilyAsync(model,User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            TempData["Success"]="Fëmijët u regjistruan së bashku. Faturat e para u krijuan sipas paketës së plotë familjare.";
            return RedirectToAction("Details","Families",new{id});
        }
        catch(Exception ex)when(ex is InvalidOperationException or System.ComponentModel.DataAnnotations.ValidationException){ModelState.AddModelError("",ex.Message);}
        catch(DbUpdateException){ModelState.AddModelError("","Regjistrimi nuk u ruajt. Asnjë pjesë e paketës nuk është regjistruar; kontrolloni të dhënat dhe provoni përsëri.");}
        if(model.Children==null||model.Children.Count==0)model.Children=[new(),new()];
        await FamilyChoices(model);return View(model);
    }
    private async Task FamilyChoices(FamilyRegistrationModel model)
    {
        model.Families=await _context.PlayerFamilies.AsNoTracking().OrderBy(f=>f.Name).ToListAsync();
        model.Plans=(await new TariffService(_context).OptionsAsync(includeInactive:true)).Where(p=>p.IsFamily).ToList();
        if(!model.Plans.Any(p=>p.Id==model.FeePlanId && p.IsActive))model.FeePlanId=model.Plans.FirstOrDefault(p=>p.IsActive)?.Id??0;
        model.Price=model.Plans.FirstOrDefault(p=>p.Id==model.FeePlanId);
        foreach(var family in model.Families){model.FamilyCounts[family.Id]=(await new FamilyService(_context).MembersAsync(family.Id,TariffService.CurrentMonth)).Count;model.FamilyPlanIds[family.Id]=await new FamilyService(_context).PlanIdAsync(family.Id,TariffService.CurrentMonth);}
        foreach(var child in model.Children)if(child.DateOfBirth.HasValue)child.Teams=await new TeamService(_context).OptionsAsync(child.DateOfBirth);
    }
    [HttpGet,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Edit(int id)
    {
        var student=await _context.Students.AsNoTracking().Include(s=>s.TrainingTeam).SingleOrDefaultAsync(s=>s.Id==id);if(student==null)return NotFound();
        var model=new StudentEditModel{Id=id,Revision=student.Revision,FirstName=student.FirstName,LastName=student.LastName,DateOfBirth=student.DateOfBirth,ParentName=student.ParentName,ParentPhone=student.ParentPhone,ParentEmail=student.ParentEmail,Notes=student.Notes,TrainingTeamId=student.TrainingTeamId,IsActive=student.IsActive,CurrentTeamName=student.TrainingTeam?.Name};
        await EditChoices(model);return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles=Roles.Write)]
    public async Task<IActionResult> Edit(StudentEditModel model)
    {
        if(!await _context.Students.AnyAsync(s=>s.Id==model.Id))return NotFound();
        if(ModelState.IsValid)try
        {
            await new StudentWorkflowService(_context).EditAsync(model,User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            TempData["Success"]="Të dhënat e lojtarit u ruajtën. Ndryshimi është regjistruar në historik.";
            return RedirectToAction("Account","Payments",new{id=model.Id});
        }
        catch(Exception ex)when(ex is InvalidOperationException or System.ComponentModel.DataAnnotations.ValidationException){ModelState.AddModelError("",ex.Message);}
        catch(DbUpdateConcurrencyException){ModelState.AddModelError("","Lojtari është ndryshuar ndërkohë. Rihapni formularin.");}
        catch(DbUpdateException){ModelState.AddModelError("","Ndryshimi nuk u ruajt. Kontrolloni të dhënat dhe provoni përsëri.");}
        await EditChoices(model);return View(model);
    }
    private async Task EditChoices(StudentEditModel model)
    {
        if(model.DateOfBirth.HasValue)model.Teams=await new TeamService(_context).OptionsAsync(model.DateOfBirth,excludingStudentId:model.Id);
        model.CurrentTeamName=await _context.TrainingTeams.Where(t=>t.Id==model.TrainingTeamId).Select(t=>t.Name).FirstOrDefaultAsync();
        model.History=await _context.StudentChanges.AsNoTracking().Include(c=>c.Actor).Where(c=>c.StudentId==model.Id).OrderByDescending(c=>c.CreatedAt).ThenByDescending(c=>c.Id).ToListAsync();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class StudentListItem
{
    public string FeePlanName { get; set; } = "";
    public Student Student { get; set; } = new();
    public decimal Balance { get; set; }
    public decimal Debt { get; set; }
    public decimal Advance { get; set; }
}
