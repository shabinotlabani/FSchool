using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Controllers;
[Authorize(Roles=Roles.SuperAdmin),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class PlayerContractsController(ApplicationDbContext db):Controller
{
 string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
 public async Task<IActionResult> Payroll(string? month,bool csv=false)
 {
  var date=TariffService.CurrentMonth;if(month!=null&&!TariffService.TryMonth(month,out date))return BadRequest();if(date<new DateOnly(2024,8,1)||date.Year>2100)return BadRequest();
  var contracts=await db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Include(c=>c.Annexes).Where(c=>c.Status!="Draft"&&c.StartsOn<date.AddMonths(1)&&(c.EndedOn??c.EndsOn)>=date).OrderBy(c=>c.Student.FirstName).ToListAsync();
  ViewData["Month"]=date;
  if(!csv)return View(contracts);
  string Cell(string? value){value??="";if(value.Length>0&&"=+-@\t\r".Contains(value[0]))value="'"+value;return "\""+value.Replace("\"","\"\"")+"\"";}
  string Money(decimal value)=>value.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
  var lines=new List<string>{"Muaji,Emri,Identifikuesi,Punedhenesi,Bruto,Kontributi punetorit,Tatimi,Neto,Kontributi klubit,Kosto,Llogaria bankare"};
  foreach(var group in contracts.GroupBy(c=>c.PersonKey)){var c=group.OrderBy(x=>x.StartsOn).Last();var p=PlayerContractService.MonthPayroll(group,date);lines.Add(string.Join(",",new[]{date.ToString("yyyy-MM"),c.PersonName,c.Terms.PersonalNumber,c.Terms.EmployerType,Money(p.Gross),Money(p.EmployeePension),Money(p.Tax),Money(p.Net),Money(p.EmployerPension),Money(p.Cost),c.Terms.BankAccount}.Select(Cell)));}
  var bytes=System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n",lines))).ToArray();return File(bytes,"text/csv","Pagat-kontraktuale-"+date.ToString("yyyy-MM")+".csv");
 }
 public async Task<IActionResult> Index(string? status)
 {
  var today=BillingClock.Today;var query=db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Include(c=>c.Annexes).AsQueryable();
  if(status=="Expiring")query=query.Where(c=>c.Status!="Draft"&&(c.EndedOn??c.EndsOn)>=today&&(c.EndedOn??c.EndsOn)<=today.AddDays(30));
  if(status=="Active")query=query.Where(c=>c.Status!="Draft"&&c.StartsOn<=today&&(c.EndedOn??c.EndsOn)>=today);
  ViewData["Status"]=status;
  return View(await query.OrderBy(c=>c.EndedOn??c.EndsOn).ToListAsync());
 }
 public async Task<IActionResult> Create(int? staffId)
 {
  var club=await new ClubBranding(db).Get();var m=new PlayerContractInput{Simple=true,PersonType="Staff",BeneficiaryId=staffId,PersonKey=staffId.HasValue?"S:"+staffId:null,Terms=new(){EmployerName=club.LegalName,EmployerNumber=club.RegistrationNumber??"",EmployerAddress=club.Address??club.City??"",Position="Staf",Workplace=club.Address??club.Name,WeeklyHours=40,Schedule="Sipas orarit të miratuar nga klubi",Leave="Sipas Ligjit të Punës",Termination="Sipas Ligjit të Punës",Duties="Detyrat sipas pozitës dhe akteve të brendshme të klubit."}};
  if(staffId.HasValue){var person=await db.SponsorshipBeneficiaries.AsNoTracking().SingleOrDefaultAsync(b=>b.Id==staffId&&b.StaffCategory!=null&&b.StudentId==null);if(person==null)return NotFound();m.Terms.PersonalNumber=person.Identifier;m.Terms.Position=person.StaffCategory=="ContractPlayer"?"Lojtar":m.Terms.Position;}await Choices(m);return View("Edit",m);
 }
 public async Task<IActionResult> Edit(int id)
 {
  var row=await db.PlayerContracts.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id);if(row==null)return NotFound();if(row.Status!="Draft")return RedirectToAction(nameof(Details),new{id});
  var m=new PlayerContractInput{Simple=true,Id=id,StudentId=row.StudentId,PersonType=row.StudentId.HasValue?"Player":"Staff",PersonKey=row.StudentId.HasValue?"P:"+row.StudentId:"S:"+row.BeneficiaryId,RequestId=row.RequestId,Revision=row.Revision,Number=row.Number,StartsOn=row.StartsOn,EndsOn=row.EndsOn,GrossSalary=row.GrossSalary,Terms=row.Terms,HasDocument=row.SignedPdf!=null,BeneficiaryId=row.BeneficiaryId};await Choices(m);return View(m);
 }
 [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(6291456)]
 public async Task<IActionResult> Save(PlayerContractInput m,string? command)
 {
  if(m.Simple){
   var parts=(m.PersonKey??"").Split(':',2);if(parts.Length==2&&parts[0]=="S"&&int.TryParse(parts[1],out var personId)){m.PersonType="Staff";m.StudentId=null;m.BeneficiaryId=personId;}else ModelState.AddModelError(nameof(m.PersonKey),"Zgjidhni personin nga regjistri i kontratave.");
   ModelState.Remove(nameof(m.StudentId));ModelState.Remove(nameof(m.BeneficiaryId));ModelState.Remove(nameof(m.PersonType));
  }
  if(ModelState.IsValid)try{var id=await new PlayerContractService(db).Save(m,Actor,command=="Activate");TempData["Success"]=command=="Activate"?"Kontrata u aktivizua.":"Kontrata u ruajt si draft.";return RedirectToAction(nameof(Details),new{id});}
  catch(UnauthorizedAccessException){return NotFound();}
  catch(Exception ex)when(ex is ValidationException or InvalidOperationException or DbUpdateConcurrencyException){ModelState.AddModelError("",ex is DbUpdateConcurrencyException?"Kontrata ndryshoi. Rihapeni.":ex.Message);}
  await Choices(m);return View("Edit",m);
 }
 public async Task<IActionResult> Details(int id)
 {
  var row=await db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Include(c=>c.Annexes.OrderByDescending(a=>a.EffectiveOn)).SingleOrDefaultAsync(c=>c.Id==id);if(row==null)return NotFound();
  ViewData["History"]=await db.PlayerContractAudits.AsNoTracking().Where(a=>a.PlayerContractId==id).OrderByDescending(a=>a.Id).ToListAsync();return View(row);
 }
 public async Task<IActionResult> Print(int id)
 {
  var row=await db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Include(c=>c.Annexes).SingleOrDefaultAsync(c=>c.Id==id);if(row==null)return NotFound();return View(row);
 }
 public async Task<IActionResult> Annex(int id)
 {
  var c=await db.PlayerContracts.AsNoTracking().Include(x=>x.Annexes).SingleOrDefaultAsync(x=>x.Id==id&&x.Status!="Draft");if(c==null)return NotFound();
  return View(new PlayerContractAnnexInput{PlayerContractId=id,EffectiveOn=BillingClock.Today,NewGrossSalary=c.SalaryOn(BillingClock.Today)});
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Annex(PlayerContractAnnexInput m,string? command)
 {
  var contract=await db.PlayerContracts.Include(x=>x.Annexes).SingleOrDefaultAsync(x=>x.Id==m.PlayerContractId&&x.Status!="Draft");if(contract==null)return NotFound();
  if(!m.EffectiveOn.HasValue||m.EffectiveOn<contract.StartsOn||m.EffectiveOn>contract.EffectiveEnd)ModelState.AddModelError(nameof(m.EffectiveOn),"Data duhet të jetë brenda afatit të kontratës.");
  if(m.NewGrossSalary==contract.SalaryOn(m.EffectiveOn??BillingClock.Today))ModelState.AddModelError(nameof(m.NewGrossSalary),"Paga e re duhet të jetë ndryshe nga paga aktuale.");
  if(ModelState.IsValid)try{
   var row=m.Id==0?new PlayerContractAnnex{PlayerContractId=contract.Id,CreatedAt=DateTime.UtcNow,ActorId=Actor}:await db.PlayerContractAnnexes.SingleOrDefaultAsync(x=>x.Id==m.Id&&x.PlayerContractId==contract.Id)??throw new ValidationException("Aneksi nuk u gjet.");
   if(row.Status!="Draft")throw new ValidationException("Aneksi i aktivizuar nuk ndryshohet.");if(m.Id!=0&&row.Revision!=m.Revision)throw new ValidationException("Aneksi ndryshoi. Rihapeni.");
   var date=m.EffectiveOn!.Value;row.Number=string.IsNullOrWhiteSpace(m.Number)?$"{contract.Number}-A{contract.Annexes.Count+1}":m.Number.Trim().ToUpperInvariant();row.EffectiveOn=date;row.PreviousGrossSalary=contract.SalaryOn(date);row.NewGrossSalary=m.NewGrossSalary;row.Reason=m.Reason.Trim();row.Status=command=="Activate"?"Active":"Draft";row.Revision=Guid.NewGuid();if(m.Id==0)db.PlayerContractAnnexes.Add(row);
   await db.SaveChangesAsync();db.PlayerContractAudits.Add(new(){PlayerContractId=contract.Id,ActorId=Actor,Action=row.Status=="Active"?"Aktivizim aneksi":"Ruajtje drafti të aneksit",Snapshot=System.Text.Json.JsonSerializer.Serialize(new{row.Number,row.EffectiveOn,row.PreviousGrossSalary,row.NewGrossSalary,row.Reason,row.Status})});await db.SaveChangesAsync();TempData["Success"]=row.Status=="Active"?"Aneksi u aktivizua.":"Aneksi u ruajt si draft.";return RedirectToAction(nameof(Details),new{id=contract.Id});
  }catch(Exception e)when(e is ValidationException or DbUpdateException){ModelState.AddModelError("",e is DbUpdateException?"Numri i aneksit ekziston.":e.Message);}
  return View(m);
 }
 public async Task<IActionResult> EditAnnex(int id)
 {
  var row=await db.PlayerContractAnnexes.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.Status=="Draft");if(row==null)return NotFound();return View("Annex",new PlayerContractAnnexInput{Id=row.Id,PlayerContractId=row.PlayerContractId,Revision=row.Revision,Number=row.Number,EffectiveOn=row.EffectiveOn,NewGrossSalary=row.NewGrossSalary,Reason=row.Reason});
 }
 public async Task<IActionResult> PrintAnnex(int id)
 {
  var row=await db.PlayerContractAnnexes.AsNoTracking().Include(x=>x.PlayerContract).ThenInclude(x=>x.Student).Include(x=>x.PlayerContract).ThenInclude(x=>x.Beneficiary).SingleOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();return View(row);
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> ChangeNumber(int id,Guid revision,string? number,bool automatic)
 {
  await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261017, 2010)");
  var row=await db.PlayerContracts.SingleOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();
  if(row.Revision!=revision){TempData["Error"]="Kontrata ndryshoi. Rihapeni.";return RedirectToAction(nameof(Details),new{id});}
  var next=automatic?await PlayerContractService.NextNumber(db,row.StartsOn.Year):(number??"").Trim().ToUpperInvariant();
  if(string.IsNullOrWhiteSpace(next)||next.Length>80){TempData["Error"]="Shkruani një numër të vlefshëm.";return RedirectToAction(nameof(Details),new{id});}
  if(await db.PlayerContracts.AnyAsync(x=>x.Id!=id&&x.Number==next)){TempData["Error"]="Ky numër kontrate ekziston.";return RedirectToAction(nameof(Details),new{id});}
  var previous=row.Number;if(previous==next){TempData["Success"]="Numri i kontratës është i njëjtë.";return RedirectToAction(nameof(Details),new{id});}
  row.Number=next;row.Revision=Guid.NewGuid();row.UpdatedAt=DateTime.UtcNow;db.PlayerContractAudits.Add(new(){PlayerContractId=id,ActorId=Actor,Action="Ndryshim i numrit të kontratës",Snapshot=System.Text.Json.JsonSerializer.Serialize(new{Previous=previous,Current=next,Automatic=automatic})});await db.SaveChangesAsync();await tx.CommitAsync();TempData["Success"]=$"Numri u ndryshua në {next}.";return RedirectToAction(nameof(Details),new{id});
 }
 public async Task<IActionResult> Document(int id)
 {
  var row=await db.PlayerContracts.AsNoTracking().Where(c=>c.Id==id).Select(c=>new{c.SignedPdf,c.FileName}).SingleOrDefaultAsync();
  return row?.SignedPdf==null?NotFound():File(row.SignedPdf,"application/pdf",row.FileName??"Kontrata.pdf");
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> End(int id,Guid revision,DateOnly date,string reason)
 {
  try{if(!ModelState.IsValid)throw new ValidationException("Kontrolloni të dhënat.");await new PlayerContractService(db).End(id,revision,date,reason,Actor);}
  catch(UnauthorizedAccessException){return NotFound();}
  catch(ValidationException e){TempData["Error"]=e.Message;}
  return RedirectToAction(nameof(Details),new{id});
 }
  public async Task<IActionResult> Staff(){ViewData["Staff"]=await db.SponsorshipBeneficiaries.AsNoTracking().Where(b=>b.StaffCategory!=null&&b.StudentId==null).OrderBy(b=>b.Name).ToListAsync();return View(new ContractStaffInput());}
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Staff(ContractStaffInput m)
 {
  var key=string.Concat((m.Identifier??"").Trim().ToUpperInvariant().Where(c=>!char.IsWhiteSpace(c)));
  if(m.BirthDate>BillingClock.Today||m.BirthDate<new DateOnly(1900,1,1))ModelState.AddModelError("BirthDate","Kontrolloni datëlindjen.");
  if(ModelState.IsValid){
   await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261017, 2010)");
   var existing=await db.SponsorshipBeneficiaries.SingleOrDefaultAsync(b=>b.Identifier==key);
   if(existing!=null&&(existing.StudentId!=null||!existing.IsPerson||existing.StaffCategory!=null))ModelState.AddModelError("Identifier","Ky person ekziston. Përdorni regjistrimin ekzistues.");
   else {var b=existing??new SponsorshipBeneficiary{Identifier=key,IsPerson=true};b.Name=m.Name.Trim();b.StaffCategory=m.Category;b.StaffBirthDate=m.BirthDate;if(existing==null)db.SponsorshipBeneficiaries.Add(b);await db.SaveChangesAsync();await tx.CommitAsync();return RedirectToAction(nameof(Staff));}
  }
  ViewData["Staff"]=await db.SponsorshipBeneficiaries.AsNoTracking().Where(b=>b.StaffCategory!=null&&b.StudentId==null).OrderBy(b=>b.Name).ToListAsync();return View(m);
 }
 async Task Choices(PlayerContractInput m){m.Students=[];m.Beneficiaries=await db.SponsorshipBeneficiaries.AsNoTracking().Where(b=>b.IsPerson&&b.StudentId==null&&b.StaffCategory!=null).OrderBy(b=>b.Name).ToListAsync();}
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Atk(int id,Guid revision,DateOnly date,string reference)
 {
  var row=await db.PlayerContracts.SingleOrDefaultAsync(c=>c.Id==id);if(row==null)return NotFound();
  if(!ModelState.IsValid||row.Revision!=revision||date.Year<1900||date>BillingClock.Today||string.IsNullOrWhiteSpace(reference)||reference.Length>100){TempData["Error"]="Kontrolloni datën, referencën ose rihapni kontratën.";return RedirectToAction(nameof(Details),new{id});}
  var terms=row.Terms;terms.AtkRegisteredOn=date;terms.AtkReference=reference.Trim();row.TermsData=System.Text.Json.JsonSerializer.Serialize(terms);row.Revision=Guid.NewGuid();
  db.PlayerContractAudits.Add(new(){PlayerContractId=id,ActorId=Actor,Action="Evidencë ATK",Snapshot=System.Text.Json.JsonSerializer.Serialize(new{date,reference})});
  try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){TempData["Error"]="Kontrata ndryshoi. Rihapeni.";}
  return RedirectToAction(nameof(Details),new{id});
 }
}
