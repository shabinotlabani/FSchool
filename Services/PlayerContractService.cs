using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
namespace _2Korriku.Services;
public class PlayerContractService(ApplicationDbContext db)
{
 public static decimal MonthGross(PlayerContract c,DateOnly month)
 {
  var end=month.AddMonths(1).AddDays(-1);var from=c.StartsOn>month?c.StartsOn:month;var to=c.EffectiveEnd<end?c.EffectiveEnd:end;
  if(from>to)return 0;
  decimal gross=0;var day=from;
  while(day<=to){var salary=c.SalaryOn(day);var next=c.Annexes.Where(a=>a.Status=="Active"&&a.EffectiveOn>day&&a.EffectiveOn<=to).OrderBy(a=>a.EffectiveOn).Select(a=>(DateOnly?)a.EffectiveOn).FirstOrDefault();var segmentEnd=next?.AddDays(-1)??to;gross+=salary*(segmentEnd.DayNumber-day.DayNumber+1)/DateTime.DaysInMonth(month.Year,month.Month);day=segmentEnd.AddDays(1);}
  return Math.Round(gross,2,MidpointRounding.AwayFromZero);
 }
 public static ContractPayroll Payroll(decimal gross,ContractTerms terms)
 {
  decimal Round(decimal value)=>Math.Round(value,2,MidpointRounding.AwayFromZero);
  var pension=Round(gross*terms.EmployeePensionPercent/100);var taxable=gross-pension;
  var tax=Round(terms.EmployerType=="Secondary"?taxable*.10m:taxable<=250?0:taxable<=450?(taxable-250)*.08m:16+(taxable-450)*.10m);
  return new(gross,pension,tax,gross-pension-tax,Round(gross*terms.EmployerPensionPercent/100));
 }
 public static ContractPayroll MonthPayroll(IEnumerable<PlayerContract> contracts,DateOnly month)
 {
  var rows=contracts.Where(c=>MonthGross(c,month)>0).OrderBy(c=>c.StartsOn).ToList();if(rows.Count==0)return new(0,0,0,0,0);
  var gross=rows.Sum(c=>MonthGross(c,month));var employee=rows.Sum(c=>Payroll(MonthGross(c,month),c.Terms).EmployeePension);var employer=rows.Sum(c=>Payroll(MonthGross(c,month),c.Terms).EmployerPension);
  var taxable=gross-employee;var tax=Math.Round(rows.Last().Terms.EmployerType=="Secondary"?taxable*.1m:taxable<=250?0:taxable<=450?(taxable-250)*.08m:16+(taxable-450)*.1m,2,MidpointRounding.AwayFromZero);
  return new(gross,employee,tax,gross-employee-tax,employer);
 }
 public IQueryable<PlayerContract> Covering(int studentId,DateOnly month)=>db.PlayerContracts.Where(c=>c.StudentId==studentId&&c.Status!="Draft"&&c.StartsOn<month.AddMonths(1)&&(c.EndedOn??c.EndsOn)>=month);
 public static async Task<string> NextNumber(ApplicationDbContext db,int year)
 {
  var prefix=$"K-{year}-";var numbers=await db.PlayerContracts.AsNoTracking().Where(c=>c.Number.StartsWith(prefix)).Select(c=>c.Number).ToListAsync();var max=0;
  foreach(var number in numbers)if(int.TryParse(number[prefix.Length..],out var sequence)&&sequence>max)max=sequence;
  return $"{prefix}{max+1:0000}";
 }
 public async Task<int> Save(PlayerContractInput input,string actor,bool activate)
 {
  Validator.ValidateObject(input,new(input),true);if(!input.Simple)Validator.ValidateObject(input.Terms,new(input.Terms),true);
  if(input.RequestId==Guid.Empty||input.StartsOn.Year<1900||input.EndsOn.Year>2200||input.EndsOn<input.StartsOn||decimal.Round(input.GrossSalary,2)!=input.GrossSalary)throw new ValidationException("Kontrolloni periudhën dhe pagën bruto.");
  if(!input.Simple&&(input.Terms.EmployeePensionPercent<5||input.Terms.EmployerPensionPercent<5)&&string.IsNullOrWhiteSpace(input.Terms.PensionException))throw new ValidationException("Për kontribut nën 5% shënoni bazën e përjashtimit.");
  if(!input.Simple&&input.Terms.AtkRegisteredOn.HasValue&&(input.Terms.AtkRegisteredOn>BillingClock.Today||string.IsNullOrWhiteSpace(input.Terms.AtkReference)))throw new ValidationException("Kontrolloni datën dhe referencën e regjistrimit në ATK.");
  await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync():null;
  await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
  await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261017, 2010)");
    var staff=input.PersonType=="Staff";
  if(staff)input.StudentId=null;
  var student=staff?null:await db.Students.SingleOrDefaultAsync(s=>s.Id==input.StudentId)??throw new UnauthorizedAccessException();
  var staffPerson=staff?await db.SponsorshipBeneficiaries.SingleOrDefaultAsync(b=>b.Id==input.BeneficiaryId&&b.IsPerson&&b.StudentId==null&&b.StaffCategory!=null):null;
  if(staff&&staffPerson==null)throw new ValidationException("Zgjidhni personin nga regjistri i kontratave.");
  var birth=student?.DateOfBirth??staffPerson?.StaffBirthDate??throw new ValidationException("Plotësoni datëlindjen e personit.");
  var name=student?.FullName??staffPerson!.Name;
  if(input.Simple){
   var club=await new ClubBranding(db).Get();var identifier=staffPerson?.Identifier??"PLAYER-"+student!.Id;
   input.Number=string.IsNullOrWhiteSpace(input.Number)?await NextNumber(db,input.StartsOn.Year):input.Number;
   input.Terms.EmployerName=string.IsNullOrWhiteSpace(club.LegalName)?club.Name:club.LegalName;input.Terms.EmployerNumber=club.RegistrationNumber??"-";input.Terms.EmployerAddress=club.Address??club.City??"-";input.Terms.PersonalNumber=identifier;
   input.Terms.Representative=Value(input.Terms.Representative,club.ResponsiblePerson??"-");input.Terms.RepresentativePersonalNumber=string.IsNullOrWhiteSpace(input.Terms.RepresentativePersonalNumber)?club.ResponsiblePersonNumber:input.Terms.RepresentativePersonalNumber.Trim();input.Terms.Address=Value(input.Terms.Address);input.Terms.Qualification=Value(input.Terms.Qualification);input.Terms.Nationality=Value(input.Terms.Nationality,"Kosovë");input.Terms.Position=Value(input.Terms.Position,staffPerson?.StaffCategory=="ContractPlayer"?"Lojtar":"Staf");input.Terms.Duties=Value(input.Terms.Duties,"Detyrat sipas pozitës dhe akteve të brendshme të klubit.");input.Terms.Workplace=Value(input.Terms.Workplace,club.Address??club.Name);input.Terms.Schedule=Value(input.Terms.Schedule,"Sipas orarit të miratuar nga klubi");input.Terms.Leave=Value(input.Terms.Leave,"Sipas Ligjit të Punës dhe akteve të brendshme");input.Terms.Termination=Value(input.Terms.Termination,"Sipas Ligjit të Punës dhe kushteve të kësaj kontrate");input.Terms.BankAccount=Value(input.Terms.BankAccount);input.Terms.WeeklyHours=input.Terms.WeeklyHours is <1 or >60?40:input.Terms.WeeklyHours;input.Terms.EmployerType="Primary";input.Terms.EmployeePensionPercent=input.Terms.EmployeePensionPercent<0?5:input.Terms.EmployeePensionPercent;input.Terms.EmployerPensionPercent=input.Terms.EmployerPensionPercent<0?5:input.Terms.EmployerPensionPercent;
  }
  if(input.Id==0){var duplicate=await db.PlayerContracts.AsNoTracking().SingleOrDefaultAsync(c=>c.RequestId==input.RequestId);if(duplicate!=null){if(duplicate.StudentId!=input.StudentId||staff&&duplicate.BeneficiaryId!=input.BeneficiaryId)throw new ValidationException("Rihapni formularin.");return duplicate.Id;}}
  var row=input.Id==0?new PlayerContract{StudentId=input.StudentId,BeneficiaryId=staff?input.BeneficiaryId:null,RequestId=input.RequestId}:await db.PlayerContracts.SingleOrDefaultAsync(c=>c.Id==input.Id&&c.StudentId==input.StudentId&&(!staff||c.BeneficiaryId==input.BeneficiaryId))??throw new UnauthorizedAccessException();
  if(row.Status!="Draft")throw new ValidationException("Kontrata e aktivizuar ruhet si dokument historik. Për vazhdim shtoni kontratë të re.");
  if(input.Id!=0&&row.Revision!=input.Revision)throw new ValidationException("Kontrata ndryshoi. Rihapeni.");
  var number=input.Number.Trim().ToUpperInvariant();
  if(await db.PlayerContracts.AnyAsync(c=>c.Id!=row.Id&&c.Number==number))throw new ValidationException("Numri i kontratës ekziston.");
  if(input.Document!=null){
   if(input.Document.Length is <5 or >5242880)throw new ValidationException("Ngarkoni PDF deri në 5 MB.");
   using var memory=new MemoryStream();await input.Document.CopyToAsync(memory);var bytes=memory.ToArray();
   if(bytes.Length>5242880||!bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8))throw new ValidationException("Dokumenti duhet të jetë PDF.");
   row.SignedPdf=bytes;row.FileName=Path.GetFileName(input.Document.FileName);
  }
  if(activate){
   if(!input.Simple&&row.SignedPdf==null)throw new ValidationException("Për aktivizim ngarkoni kontratën e nënshkruar në PDF.");
   var age=input.StartsOn.Year-birth.Year-(input.StartsOn<birth.AddYears(input.StartsOn.Year-birth.Year)?1:0);
   if(!input.Simple&&age<15)throw new ValidationException("Kontrata e punës kërkon moshën së paku 15 vjeç.");
   if(!input.Simple&&age<18&&(!input.Terms.GuardianConsent||string.IsNullOrWhiteSpace(input.Terms.GuardianName)||input.Terms.WeeklyHours>30))throw new ValidationException("Për moshën nën 18 vjeç plotësoni kujdestarin, pëlqimin dhe orarin deri në 30 orë në javë.");
   if(await db.PlayerContracts.AnyAsync(c=>c.Id!=row.Id&&(staff?c.StudentId==null&&c.BeneficiaryId==input.BeneficiaryId:c.StudentId==input.StudentId)&&c.Status!="Draft"&&c.StartsOn<=input.EndsOn&&(c.EndedOn??c.EndsOn)>=input.StartsOn))throw new ValidationException("Personi ka kontratë në këtë periudhë.");
   if(student!=null){ var first=new DateOnly(input.StartsOn.Year,input.StartsOn.Month,1);
   await new BillingService(db).EnsureNoSeasonalChangeAsync([student.Id],first);
   if(await db.PendingBankPayments.AnyAsync(p=>p.StudentId==student.Id&&p.Kind=="Seasonal"&&p.ReviewedAt==null))throw new ValidationException("Zgjidhni kërkesën sezonale në pritje te verifikimi i bankës para aktivizimit.");
   if(await db.StudentFeeAssignments.AnyAsync(a=>a.StudentId==student.Id&&a.FamilyId!=null&&a.EffectiveMonth<=input.EndsOn)&&await new FamilyService(db).Latest(first).AnyAsync(a=>a.StudentId==student.Id&&a.FamilyId!=null))throw new ValidationException("Hiqeni lojtarin nga paketa familjare para aktivizimit të kontratës.");
   if(await db.StudentFeeAssignments.AnyAsync(a=>a.StudentId==student.Id&&a.FamilyId!=null&&a.EffectiveMonth>first&&a.EffectiveMonth<=input.EndsOn))throw new ValidationException("Ka paketë familjare të planifikuar brenda kontratës.");
   } var identifier=string.Concat(input.Terms.PersonalNumber.Trim().ToUpperInvariant().Where(c=>!char.IsWhiteSpace(c)));
   var beneficiary=staffPerson??await db.SponsorshipBeneficiaries.SingleOrDefaultAsync(b=>b.StudentId==input.StudentId&&b.StudentId!=null);
   beneficiary??=input.BeneficiaryId.HasValue?await db.SponsorshipBeneficiaries.SingleOrDefaultAsync(b=>b.Id==input.BeneficiaryId):await db.SponsorshipBeneficiaries.SingleOrDefaultAsync(b=>b.Identifier==identifier);
   if(input.BeneficiaryId.HasValue&&beneficiary==null)throw new ValidationException("Përfituesi nuk u gjet.");
   if(beneficiary!=null&&(!beneficiary.IsPerson||!staff&&beneficiary.StaffCategory!=null||beneficiary.StudentId.HasValue&&beneficiary.StudentId!=input.StudentId))throw new ValidationException("Përfituesi është i lidhur me një lojtar tjetër.");
   if(beneficiary==null){
    if(await db.SponsorshipBeneficiaries.AnyAsync(b=>b.IsPerson&&b.Name.ToUpper()==name.ToUpper()))throw new ValidationException("Ky emër ekziston në paga. Zgjidhni përfituesin ekzistues.");
    beneficiary=new(){Name=name,Identifier=identifier,IsPerson=true};db.SponsorshipBeneficiaries.Add(beneficiary);
   }
   beneficiary.StudentId=input.StudentId;row.Beneficiary=beneficiary;row.Status="Active";
  }
  row.Number=number;row.StartsOn=input.StartsOn;row.EndsOn=input.EndsOn;row.GrossSalary=input.GrossSalary;row.TermsData=JsonSerializer.Serialize(input.Terms);row.ActorId=actor;row.UpdatedAt=DateTime.UtcNow;row.Revision=Guid.NewGuid();
  if(input.Id==0)db.PlayerContracts.Add(row);
  await db.SaveChangesAsync();
  db.PlayerContractAudits.Add(new(){PlayerContractId=row.Id,ActorId=actor,Action=activate?"Aktivizim":"Ruajtje drafti",Snapshot=JsonSerializer.Serialize(new{row.Number,row.StartsOn,row.EndsOn,row.GrossSalary,row.TermsData,row.Status})});
  if(activate&&student!=null){student.ContractPending=false;await WaiveExisting(row,actor);}
  await db.SaveChangesAsync();await new BankVerificationService(db).SyncAutomaticAsync();
  if(tx!=null)await tx.CommitAsync();return row.Id;
 }
 static string Value(string? value,string fallback="-")=>string.IsNullOrWhiteSpace(value)?fallback:value.Trim();
 private async Task WaiveExisting(PlayerContract row,string actor)
 {
  var periods=await db.MonthlyFeePeriods.Where(p=>p.StudentId==row.StudentId).ToListAsync();
  foreach(var p in periods.Where(p=>new DateOnly(p.Year,p.Month,1)<=row.EndsOn&&new DateOnly(p.Year,p.Month,1).AddMonths(1)>row.StartsOn)){
   var entries=await db.StudentTransactions.Where(t=>t.StudentId==row.StudentId&&t.Year==p.Year&&t.Month==p.Month&&!t.IsCancelled&&(t.TransactionType=="MonthlyFee"||t.TransactionType=="MonthlyFeeAdjustment"||t.TransactionType=="Exemption"||t.TransactionType=="ExemptionReversal")).ToListAsync();
   var amount=Math.Max(0,entries.Sum(t=>t.Debit-t.Credit));
   if(amount>0)db.StudentTransactions.Add(new(){StudentId=row.StudentId!.Value,Year=p.Year,Month=p.Month,TransactionType="MonthlyFeeAdjustment",Description="Lirim mujor · Kontrata "+row.Number,Debit=0,Credit=amount,ReferenceType="PlayerContract",ReferenceId=row.Id,CreatedByUserId=actor,CreatedAt=DateTime.UtcNow,TransactionDate=DateTime.UtcNow});
   p.IsFeeWaived=true;p.FeePlanName="Me kontratë";p.FeeReason="Kontrata "+row.Number;
  }
 }
 public async Task End(int id,Guid revision,DateOnly date,string reason,string actor)
 {
  if(string.IsNullOrWhiteSpace(reason)||reason.Length>500||date<BillingClock.Today)throw new ValidationException("Shkruani arsyen dhe datën nga sot e tutje.");
  await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
  var row=await db.PlayerContracts.SingleOrDefaultAsync(c=>c.Id==id)??throw new UnauthorizedAccessException();
  if(row.Revision!=revision||row.Status!="Active"||date<row.StartsOn||date>row.EndsOn)throw new ValidationException("Kontrolloni periudhën ose rihapni kontratën.");
  row.EndedOn=date;row.Status="Terminated";row.EndReason=reason.Trim();row.Revision=Guid.NewGuid();row.UpdatedAt=DateTime.UtcNow;
  db.PlayerContractAudits.Add(new(){PlayerContractId=id,ActorId=actor,Action="Përfundim: "+reason.Trim(),Snapshot=JsonSerializer.Serialize(new{row.EndedOn,row.EndReason})});
  await db.SaveChangesAsync();await tx.CommitAsync();
 }
}
