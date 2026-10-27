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
  var contracts=await db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).Where(c=>c.Status!="Draft"&&c.StartsOn<date.AddMonths(1)&&(c.EndedOn??c.EndsOn)>=date).OrderBy(c=>c.Student.FirstName).ToListAsync();
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
  var today=BillingClock.Today;var query=db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).AsQueryable();
  if(status=="Expiring")query=query.Where(c=>c.Status!="Draft"&&(c.EndedOn??c.EndsOn)>=today&&(c.EndedOn??c.EndsOn)<=today.AddDays(30));
  if(status=="Active")query=query.Where(c=>c.Status!="Draft"&&c.StartsOn<=today&&(c.EndedOn??c.EndsOn)>=today);
  ViewData["Status"]=status;
  return View(await query.OrderBy(c=>c.EndedOn??c.EndsOn).ToListAsync());
 }
 public async Task<IActionResult> Create(int? staffId)
 {
  var club=await new ClubBranding(db).Get();var m=new PlayerContractInput{Simple=true,PersonType="Staff",BeneficiaryId=staffId,PersonKey=staffId.HasValue?"S:"+staffId:null,Terms=new(){EmployerName=club.LegalName,EmployerNumber=club.RegistrationNumber??"",EmployerAddress=club.Address??"",Position="Staf"}};
  if(staffId.HasValue){var person=await db.SponsorshipBeneficiaries.AsNoTracking().SingleOrDefaultAsync(b=>b.Id==staffId&&b.StaffCategory!=null&&b.StudentId==null);if(person==null)return NotFound();m.Terms.PersonalNumber=person.Identifier;}await Choices(m);return View("Edit",m);
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
  if(ModelState.IsValid)try{var id=await new PlayerContractService(db).Save(m,Actor,m.Simple||command=="Activate");return RedirectToAction(nameof(Details),new{id});}
  catch(UnauthorizedAccessException){return NotFound();}
  catch(Exception ex)when(ex is ValidationException or InvalidOperationException or DbUpdateConcurrencyException){ModelState.AddModelError("",ex is DbUpdateConcurrencyException?"Kontrata ndryshoi. Rihapeni.":ex.Message);}
  await Choices(m);return View("Edit",m);
 }
 public async Task<IActionResult> Details(int id)
 {
  var row=await db.PlayerContracts.AsNoTracking().Include(c=>c.Student).Include(c=>c.Beneficiary).SingleOrDefaultAsync(c=>c.Id==id);if(row==null)return NotFound();
  ViewData["History"]=await db.PlayerContractAudits.AsNoTracking().Where(a=>a.PlayerContractId==id).OrderByDescending(a=>a.Id).ToListAsync();return View(row);
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
