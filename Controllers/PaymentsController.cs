using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Read)]
public class PaymentsController(ApplicationDbContext db, BillingService billing) : Controller
{
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Progress(int id)
    {
        var player=await db.Students.AsNoTracking().Include(x=>x.TrainingTeam).SingleOrDefaultAsync(x=>x.Id==id);
        if(player==null)return NotFound();
        var medical=await db.CareEntries.AsNoTracking().Where(x=>x.Assignment.StudentId==id&&x.Assignment.Kind==Roles.Doctor&&x.Date<=BillingClock.Today)
            .OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).Select(x=>new CareNotice(id,x.TrainingStatus!,x.CoachInstructions,x.Date,x.ReviewDate)).ToListAsync();
        return View(new AccountProgress(player,await new ParentProgressService(db).Load(id,BillingClock.Today),medical));
    }
    private IActionResult AfterPayment(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            var path=returnUrl.Split('?', '#')[0].TrimEnd('/');
            var root=Request.PathBase.Value??"";
            if(path.Equals(root+"/Payments",StringComparison.OrdinalIgnoreCase)||path.Equals(root+"/Payments/Index",StringComparison.OrdinalIgnoreCase))return LocalRedirect(returnUrl);
        }
        return RedirectToAction(nameof(Index));
    }
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Hyni përsëri në llogari.");
    private static string StaffName(ApplicationUser? user)
    {
        if (user == null) return "Administrata";
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? "Administrata" : fullName;
    }

    public async Task<IActionResult> Index(int? year, int? month, string? search, bool unpaidOnly = false)
    {
        var today = BillingClock.Today;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (y < 2000 || y > 9998 || m < 1 || m > 12) return BadRequest("Muaji nuk është i vlefshëm.");
        var invoices = (await billing.GetInvoicesAsync()).Where(i => i.Period.Year == y && i.Period.Month == m).ToList();
        var assignments = await new FamilyService(db).Latest(TariffService.CurrentMonth)
            .Include(a => a.Family).Where(a => a.FeePlan!.IsFamily && a.FamilyId != null).ToListAsync();
        var byStudent = assignments.ToDictionary(a => a.StudentId);
        var families = invoices.Where(i => byStudent.ContainsKey(i.Student.Id))
            .GroupBy(i => byStudent[i.Student.Id].FamilyId!.Value)
            .Select(g => new FamilyInvoiceGroup { Family = byStudent[g.First().Student.Id].Family!, Invoices = g.ToList() }).ToList();
        invoices = invoices.Where(i => !byStudent.ContainsKey(i.Student.Id)).ToList();
        var otherDebts=await new OtherDebtService(db).LoadAsync();
        var rows=families.Select(f=>new BillingGridRow{Family=f.Family,Invoices=f.Invoices,Students=f.Invoices.Select(i=>i.Student).ToList()})
            .Concat(invoices.Select(i=>new BillingGridRow{Invoices=[i],Students=[i.Student]})).ToList();
        var debtIds=otherDebts.Select(x=>x.StudentId).Distinct().ToList();
        var debtStudents=await db.Students.AsNoTracking().Where(x=>debtIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        foreach(var debt in otherDebts){
            var family=byStudent.GetValueOrDefault(debt.StudentId)?.Family;
            var row=rows.FirstOrDefault(x=>family!=null?x.Family?.Id==family.Id:x.Family==null&&x.Students.Any(s=>s.Id==debt.StudentId));
            if(row==null){row=new BillingGridRow{Family=family};rows.Add(row);}
            if(!row.Students.Any(x=>x.Id==debt.StudentId))row.Students.Add(debtStudents[debt.StudentId]);
            row.OtherDebts.Add(debt);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            foreach (var term in search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                rows=rows.Where(r=>r.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||(r.Family?.Phone?.Contains(term)??false)||r.Students.Any(s=>s.FullName.Contains(term,StringComparison.OrdinalIgnoreCase)||(s.ParentPhone?.Contains(term)??false))).ToList();
            }
        }
        var pendingBank=await db.PendingBankPayments.AsNoTracking().Where(x=>x.ReviewedAt==null).Select(x=>new{x.StudentId,x.Kind,x.TargetId,x.Amount}).ToListAsync();
        foreach(var row in rows)row.PendingBankAmount=pendingBank.Where(p=>p.StudentId.HasValue&&row.Students.Any(s=>s.Id==p.StudentId)||p.Kind=="Family"&&row.Family!=null&&p.TargetId==row.Family.Id).Sum(p=>p.Amount);
        if(unpaidOnly)rows=rows.Where(r=>r.TotalDue>0).ToList();
        invoices=rows.Where(r=>r.Family==null).SelectMany(r=>r.Invoices).ToList();
        families=rows.Where(r=>r.Family!=null).Select(r=>new FamilyInvoiceGroup{Family=r.Family!,Invoices=r.Invoices}).ToList();
        await new MonthlyAttendanceService(db).PopulateAsync(invoices.Concat(families.SelectMany(f=>f.Invoices)),y,m);
        return View(new BillingIndexModel { Rows=rows.OrderBy(r=>r.Name).ToList(),OtherDebts=rows.SelectMany(r=>r.OtherDebts).ToList(), Year = y, Month = m, Search = search?.Trim(), UnpaidOnly = unpaidOnly,
            Families = families.OrderBy(f => f.Family.Name).ToList(), Invoices = invoices.OrderBy(i => i.Student.LastName).ThenBy(i => i.Student.FirstName).ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Generate()
    {
        var count = await billing.GenerateDueFeesAsync(ActorId);
        TempData["Success"] = count == 0 ? "Të gjitha faturat janë të përditësuara. Nuk u krijua asnjë dublikatë." : $"U krijuan {count} fatura mujore.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Account(int id)
    {
        var model = await billing.GetAccountAsync(id);
        if (model != null) model.AttendanceHistory = await new MonthlyAttendanceService(db).HistoryAsync(id);
        return model == null ? NotFound() : View(model);
    }

    public async Task<IActionResult> PrintAccount(int id)
    {
        var model = await billing.GetAccountAsync(id);
        return model == null ? NotFound() : View(model);
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var period = await db.MonthlyFeePeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (period == null) return NotFound();
        var account = (await billing.GetAccountAsync(period.StudentId))!;
        var invoice = account.Invoices.Single(i => i.Period.Id == id);
        var preparedBy = StaffName(await db.Users.FindAsync(ActorId));
        return View("Document", new PaymentDocumentModel { Number = invoice.Number, Date = BillingClock.LocalDate(period.GeneratedAt), Student = account.Student, Description = invoice.MonthLabel, Amount = invoice.Amount, Exempted = invoice.Exempted, Paid = invoice.Paid, Due = invoice.Due, PreparedBy = preparedBy, Notes = string.Join(" · ", new[] { period.ProrationDescription, period.FeePlanName, period.FeeReason, period.FeeNotes, "Gjendja më " + BillingClock.Today.ToString("dd.MM.yyyy") }.Where(s => !string.IsNullOrWhiteSpace(s))) });
    }

    public async Task<IActionResult> Receipt(int id, string? returnUrl = null)
    {
        var payment = await db.Payments.AsNoTracking().Include(p => p.Student).Include(p => p.CreatedByUser).Include(p => p.Sale).Include(p=>p.PersonalCharge).SingleOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        if (payment.FamilyPaymentId.HasValue) return RedirectToAction(nameof(FamilyReceipt), new { id = payment.FamilyPaymentId, returnUrl });
        return View("Document", new PaymentDocumentModel { IsReceipt = true, ServiceDescription = payment.PersonalCharge?.Description, SaleNumber = payment.Sale?.InvoiceNumber, IsCancelled = payment.IsCancelled, CancellationReason = payment.CancellationReason, Number = $"ARK-{BillingClock.LocalDate(payment.PaymentDate).Year}-{payment.Id:000000}", Date = BillingClock.LocalDate(payment.PaymentDate), Student = payment.Student!, Amount = payment.Amount, SeasonalDescription = payment.SeasonalStartMonth.HasValue ? payment.SeasonalLabel : null, SeasonalGross = payment.SeasonalRate * payment.SeasonalMonths ?? 0, SeasonalDiscount = payment.SeasonalRate * (payment.SeasonalMonths / 6) ?? 0, Method = BillingService.MethodLabel(payment.PaymentMethod), Notes = payment.Notes, PreparedBy = StaffName(payment.CreatedByUser) });
    }

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Seasonal(int studentId, string? startMonth, int months = 6)
    {
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return NotFound();
        var model = new SeasonalPaymentModel { StudentId = studentId, StudentName = student.FullName, StartMonth = startMonth ?? TariffService.CurrentMonth.ToString("yyyy-MM"), Months = months };
        try { model.Quote = await billing.QuoteSeasonalAsync(model); model.ExpectedRate = model.Quote.Rate; }
        catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError(string.Empty, ex.Message); }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Seasonal(SeasonalPaymentModel model, string? returnUrl = null)
    {
        ViewData["PaymentReturnUrl"]=returnUrl;
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == model.StudentId);
        if (student == null) return NotFound();
        model.StudentName = student.FullName;
        if (ModelState.IsValid)
        {
            try
            {
                if(model.PaymentMethod=="Bank"){await new BankVerificationService(db).QueueSeasonal(model,ActorId);TempData["Success"]="Pagesa bankare u ruajt në pritje. Borxhi mbetet deri në konfirmim.";return AfterPayment(returnUrl);}
                var id = await billing.RegisterSeasonalAsync(model, ActorId);
                TempData["Success"] = "Pagesa sezonale u ruajt. Muajt e mbuluar dhe zbritja ruhen në historik.";
                TempData["PaymentReceiptId"]=id; return AfterPayment(returnUrl);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        try{model.Quote=await billing.QuoteSeasonalAsync(new SeasonalPaymentModel{StudentId=model.StudentId,StartMonth=model.StartMonth,Months=model.Months});}
        catch(Exception ex)when(ex is InvalidOperationException or ValidationException){
            if(!ModelState.Values.SelectMany(x=>x.Errors).Any(x=>x.ErrorMessage==ex.Message))ModelState.AddModelError("",ex.Message);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Debt(int id, string? returnUrl = null)
    {
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Operator))
            return RedirectToAction(nameof(Account), new { id, returnUrl });
        var account = await billing.GetAccountAsync(id);
        if (account == null) return NotFound();
        // Pay an existing debt in its own ledger, never turn equipment/training debt into a monthly advance.
        if (account.MonthlyDue > 0)
            return RedirectToAction(nameof(Register), new { studentId = id, returnUrl });
        var sale = account.Sales.Where(s => s.Status != "Cancelled" && s.Due > 0 && s.RequestId.HasValue).OrderBy(s => s.Date).ThenBy(s => s.Id).FirstOrDefault();
        if (sale != null) return RedirectToAction("Pay", "Sales", new { id = sale.Id, returnUrl });
        var charge = account.PersonalTrainings.SelectMany(t => t.Charges).Where(c => c.Due > 0).OrderBy(c => c.Period).ThenBy(c => c.Id).FirstOrDefault();
        if (charge != null) return RedirectToAction("Pay", "PersonalTraining", new { id = charge.Id, returnUrl });
        return RedirectToAction(nameof(Account), new { id, returnUrl });
    }

    [HttpGet, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Register(int? studentId, string? returnUrl = null)
    {
        if (!studentId.HasValue) return RedirectToAction(nameof(Index));
        var familyId = await billing.CurrentFamilyIdAsync(studentId.Value);
        if (familyId.HasValue) return RedirectToAction(nameof(Family), new { id = familyId, returnUrl });
        var account = await billing.GetAccountAsync(studentId.Value);
        if (account == null) return NotFound();
        var automatic=await db.PendingBankPayments.Where(x=>x.Kind=="Monthly"&&x.StudentId==studentId&&x.IsAutomatic&&x.ReviewedAt==null).SumAsync(x=>x.Amount);
        return View(new RegisterPaymentModel { AutomaticBankAmount=automatic, PaymentMethod = account.Student.PreferredPaymentMethod, StudentId = account.Student.Id, StudentName = account.Student.FullName, Balance = account.MonthlyDue > 0 ? account.MonthlyDue : account.MonthlyBalance + account.SeasonalReserve, Amount = account.MonthlyDue > 0 ? account.MonthlyDue : null });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Register(RegisterPaymentModel model, string? returnUrl = null)
    {
        ViewData["PaymentReturnUrl"]=returnUrl;
        var familyId = await billing.CurrentFamilyIdAsync(model.StudentId);
        if (familyId.HasValue) return RedirectToAction(nameof(Family), new { id = familyId, returnUrl });
        var account = await billing.GetAccountAsync(model.StudentId);
        if (account == null) return NotFound();
        model.StudentName = account.Student.FullName;
        model.AutomaticBankAmount=await db.PendingBankPayments.Where(x=>x.Kind=="Monthly"&&x.StudentId==model.StudentId&&x.IsAutomatic&&x.ReviewedAt==null).SumAsync(x=>x.Amount);
        model.Balance = account.MonthlyDue > 0 ? account.MonthlyDue : account.MonthlyBalance + account.SeasonalReserve;
        if (!ModelState.IsValid) return View(model);
        try
        {
            if(model.PaymentMethod=="Bank"){if(model.AutomaticBankAmount<=0)await new BankVerificationService(db).QueueMonthly(model,ActorId);TempData["Success"]="Pagesa bankare u ruajt në pritje. Borxhi mbetet deri në konfirmim.";return AfterPayment(returnUrl);}
            var receiptId = await billing.RegisterPaymentAsync(model, ActorId);
            TempData["Success"] = "Pagesa u ruajt. U mbuluan fillimisht faturat më të vjetra; teprica mbetet parapagim.";
            TempData["PaymentReceiptId"]=receiptId; return AfterPayment(returnUrl);
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Exempt(int id)
    {
        var period = await db.MonthlyFeePeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (period == null) return NotFound();
        var invoice = (await billing.GetInvoicesAsync(period.StudentId)).Single(i => i.Period.Id == id);
        if (!invoice.CanExempt) ModelState.AddModelError(string.Empty, "Lirimi lejohet vetëm për faturat pa asnjë pagesë, pa lirim aktiv dhe jashtë sezonit të paguar.");
        return View(new ExemptInvoiceModel { PeriodId = id, Invoice = invoice, Amount = invoice.Amount });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Exempt(ExemptInvoiceModel model)
    {
        var period = await db.MonthlyFeePeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.PeriodId);
        if (period == null) return NotFound();
        model.Invoice = (await billing.GetInvoicesAsync(period.StudentId)).Single(i => i.Period.Id == period.Id);
        if (!ModelState.IsValid) return View(model);
        try
        {
            await billing.ExemptAsync(model, ActorId);
            TempData["Success"] = "Lirimi u regjistrua me gjurmë të plotë. Shuma e liruar nuk figuron si borxh.";
            return RedirectToAction(nameof(Account), new { id = period.StudentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Invoice = (await billing.GetInvoicesAsync(period.StudentId)).Single(i => i.Period.Id == period.Id);
            return View(model);
        }
    }

    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ReverseExemption(int id)
    {
        var entry = await db.StudentMonthlyExemptions.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null) return NotFound();
        ViewData["Action"] = nameof(ReverseExemption);
        return View("Reverse", new ReverseBillingModel { Id = id, StudentId = entry.StudentId, Description = $"Kthim lirimi: {entry.Student!.FullName} · {entry.Month:00}/{entry.Year} · {entry.Amount:N2}" });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ReverseExemption(ReverseBillingModel model)
    {
        var entry = await db.StudentMonthlyExemptions.FindAsync(model.Id);
        if (entry == null) return NotFound();
        model.StudentId = entry.StudentId;
        ViewData["Action"] = nameof(ReverseExemption);
        model.Description = $"Kthim lirimi · {entry.Month:00}/{entry.Year} · {entry.Amount:N2}";
        if (!ModelState.IsValid) return View("Reverse", model);
        try
        {
            var id = await billing.ReverseExemptionAsync(model.Id, model.Reason, ActorId);
            TempData["Success"] = "Lirimi u kthye. Regjistrimi fillestar dhe arsyeja e kthimit ruhen në historik.";
            return RedirectToAction(nameof(Account), new { id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("Reverse", model); }
    }

    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CancelPayment(int id)
    {
        var entry = await db.Payments.Include(p => p.Student).FirstOrDefaultAsync(p => p.Id == id);
        if (entry == null) return NotFound();
        if (entry.FamilyPaymentId.HasValue) return RedirectToAction(nameof(CancelFamilyPayment), new { id = entry.FamilyPaymentId });
        ViewData["Action"] = nameof(CancelPayment);
        ViewData["SeasonalCancellation"] = entry.SeasonalStartMonth.HasValue;
        return View("Reverse", new ReverseBillingModel { Id = id, StudentId = entry.StudentId, Description = $"Anulim pagese #{id}: {entry.Student!.FullName} · {entry.Amount:N2}" });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CancelPayment(ReverseBillingModel model)
    {
        var entry = await db.Payments.FindAsync(model.Id);
        if (entry == null) return NotFound();
        if (entry.FamilyPaymentId.HasValue) return RedirectToAction(nameof(CancelFamilyPayment), new { id = entry.FamilyPaymentId });
        model.StudentId = entry.StudentId;
        model.Description = $"Anulim pagese #{model.Id} · {entry.Amount:N2}";
        ViewData["Action"] = nameof(CancelPayment);
        ViewData["SeasonalCancellation"] = entry.SeasonalStartMonth.HasValue;
        if (!ModelState.IsValid) return View("Reverse", model);
        try
        {
            var id = await billing.CancelPaymentAsync(model.Id, model.Reason, ActorId);
            TempData["Success"] = "Pagesa u anulua me shënim korrigjues. Historiku origjinal u ruajt.";
            return RedirectToAction(nameof(Account), new { id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("Reverse", model); }
    }
    public async Task<IActionResult> Family(int id)
    {
        var account = await billing.GetFamilyAccountAsync(id);
        if (account == null) return NotFound();
        return View(new FamilyPaymentModel { AutomaticBankAmount=await db.PendingBankPayments.Where(x=>x.Kind=="Family"&&x.TargetId==id&&x.IsAutomatic&&x.ReviewedAt==null).SumAsync(x=>x.Amount), PaymentMethod=account.Family.PreferredPaymentMethod, OtherDebts=(await new OtherDebtService(db).LoadAsync()).Where(x=>account.Members.Any(m=>m.Student.Id==x.StudentId)).ToList(),FamilyId = id, Account = account, Amount = account.Due, ExpectedState = account.State });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Write)]
    public async Task<IActionResult> Family(FamilyPaymentModel model, string? returnUrl = null)
    {
        ViewData["PaymentReturnUrl"]=returnUrl;
        if (ModelState.IsValid)
            try
            {
                if(model.PaymentMethod=="Bank"){if(!await db.PendingBankPayments.AnyAsync(x=>x.Kind=="Family"&&x.TargetId==model.FamilyId&&x.IsAutomatic&&x.ReviewedAt==null))await new BankVerificationService(db).QueueFamily(model,ActorId);TempData["Success"]="Pagesa bankare u ruajt në pritje të konfirmimit.";return AfterPayment(returnUrl);} var id = await billing.RegisterFamilyPaymentAsync(model, ActorId);
                TempData["Success"] = "Pagesa e plotë e familjes u regjistrua.";
                TempData["FamilyReceiptId"]=id; return AfterPayment(returnUrl);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ValidationException) { ModelState.AddModelError("", ex.Message); }
        model.AutomaticBankAmount=await db.PendingBankPayments.Where(x=>x.Kind=="Family"&&x.TargetId==model.FamilyId&&x.IsAutomatic&&x.ReviewedAt==null).SumAsync(x=>x.Amount);model.Account = await billing.GetFamilyAccountAsync(model.FamilyId);
        if (model.Account == null) return NotFound();
        model.OtherDebts=(await new OtherDebtService(db).LoadAsync()).Where(x=>model.Account.Members.Any(m=>m.Student.Id==x.StudentId)).ToList();
        return View(model);
    }

    public async Task<IActionResult> FamilyReceipt(int id, string? returnUrl = null)
    {
        var payment = await db.FamilyPayments.AsNoTracking().Include(p => p.CreatedByUser)
            .Include(p => p.Payments).ThenInclude(p => p.Student).SingleOrDefaultAsync(p => p.Id == id);
        return payment == null ? NotFound() : View(payment);
    }

    [HttpGet, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CancelFamilyPayment(int id)
    {
        var payment = await db.FamilyPayments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        ViewData["FamilyId"] = payment.FamilyId;
        return View(new ReverseBillingModel { Id = id, Description = $"{payment.FamilyName} · {payment.Amount:N2}" });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CancelFamilyPayment(ReverseBillingModel model)
    {
        var payment = await db.FamilyPayments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == model.Id);
        if (payment == null) return NotFound();
        if (ModelState.IsValid)
            try
            {
                var id = await billing.CancelFamilyPaymentAsync(model.Id, model.Reason, ActorId);
                TempData["Success"] = "Pagesa familjare u anulua për të gjithë anëtarët.";
                return RedirectToAction(nameof(Family), new { id });
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        ViewData["FamilyId"] = payment.FamilyId;
        model.Description = $"{payment.FamilyName} · {payment.Amount:N2}";
        return View(model);
    }
}
