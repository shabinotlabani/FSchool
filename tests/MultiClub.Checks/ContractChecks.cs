using System.ComponentModel.DataAnnotations;
using System.Text;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
static class ContractChecks
{
 public static ContractTerms Terms()=>new(){EmployerName="Club Test",EmployerNumber="123456789",EmployerAddress="Prishtine",Representative="Club Manager",PersonalNumber="9876543210",Address="Prishtine",Qualification="Sportist",BankAccount="XK051234567890123456",Duties="Training and matches",Workplace="Club grounds",Schedule="Monday to Friday",Leave="Annual leave under applicable law",Termination="Written notice under applicable law"};
 public static IFormFile Pdf()=>new FormFile(new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\n% Contract fixture\n%%EOF")),0,33,"Document","contract.pdf");
 public static async Task Run(DbContextOptions<ApplicationDbContext> options)
 {
  ApplicationDbContext Db(int club=1)=>new(options,new ClubContext{ClubId=club});
  void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS: "+label);}
  async Task Reject(Func<Task> action,string label){try{await action();}catch(Exception e)when(e is ValidationException or InvalidOperationException or UnauthorizedAccessException or DbUpdateException){Console.WriteLine("PASS: "+label);return;}throw new Exception("Expected rejection: "+label);}
  var month=TariffService.CurrentMonth;int studentId,contractId;
  await using(var db=Db()){
   db.Users.Add(new(){Id="contract-admin",UserName="contract-admin@example.test"});var student=new Student{FirstName="Contract",LastName="Player",TrainingTeam=new TrainingTeam{Name="Contract team",MinAge=0,MaxAge=100,Capacity=30},DateOfBirth=new(2000,1,1),MonthlyFee=80,RegistrationDate=BillingClock.UtcDate(month)};db.Students.Add(student);await db.SaveChangesAsync();studentId=student.Id;
   await new BillingService(db).GenerateStudentMonthAsync(studentId,month.Year,month.Month,"contract-admin");
  }
  PlayerContractInput Input()=>new(){StudentId=studentId,Number="PC-2026-1",StartsOn=month,EndsOn=month.AddMonths(1).AddDays(-1),GrossSalary=1000,Terms=Terms(),Document=Pdf()};
  await using(var db=Db()){
   var input=Input();var service=new PlayerContractService(db);contractId=await service.Save(input,"contract-admin",true);
   Check(await service.Save(input,"contract-admin",true)==contractId,"Contract activation is idempotent");
   Check(await db.SponsorshipBeneficiaries.CountAsync(b=>b.StudentId==studentId)==1,"Contract creates one linked payroll beneficiary");
   var student=await db.Students.SingleAsync(s=>s.Id==studentId);
   Check((await new TariffService(db).ResolveAsync(student,month)).IsWaiver,"Active contract waives monthly fees");
   Check((await new TariffService(db).ResolveAsync(student,month.AddMonths(1))).Amount==80,"Standard fee resumes after contract expires");
   Check(await db.StudentTransactions.Where(t=>t.StudentId==studentId).SumAsync(t=>t.Debit-t.Credit)==0,"Contract adjusts an existing monthly charge without deleting history");
   Check(await db.StudentTransactions.CountAsync(t=>t.StudentId==studentId&&t.ReferenceType=="PlayerContract")==1,"Repeat activation cannot duplicate waiver credit");
   Check(await db.SponsorshipSpending.CountAsync(s=>s.Beneficiary.StudentId==studentId)==0,"Contract does not invent salary payments");
   var blocks=await new FamilyService(db).JoinBlocksAsync([studentId],0,month);Check(blocks.ContainsKey(studentId),"Contract player cannot enter a fee-paying family package");
  }
  await using(var db=Db()){var input=Input();input.Number="PC-overlap";await Reject(()=>new PlayerContractService(db).Save(input,"contract-admin",true),"Overlapping player contracts rejected");}
  await using(var db=Db(2)){Check(!await db.PlayerContracts.AnyAsync(c=>c.Id==contractId),"Contracts isolated by club");await Reject(()=>new PlayerContractService(db).Save(Input(),"contract-admin",true),"Cannot contract another club's player");}
  await using(var db=Db()){var input=Input();input.Number="PC-file";input.Document=null;await Reject(()=>new PlayerContractService(db).Save(input,"contract-admin",true),"Activation requires signed PDF");}
  await using(var db=Db()){
   var count=await db.Students.CountAsync();var person=new SponsorshipBeneficiary{Name="Technical Manager",Identifier="STAFF-TEST-1",IsPerson=true,StaffCategory="Management",StaffBirthDate=new(1985,1,1)};db.SponsorshipBeneficiaries.Add(person);await db.SaveChangesAsync();
   var input=Input();input.StudentId=null;input.PersonType="Staff";input.BeneficiaryId=person.Id;input.Number="STAFF-C-1";
   var id=await new PlayerContractService(db).Save(input,"contract-admin",true);var row=await db.PlayerContracts.Include(c=>c.Beneficiary).SingleAsync(c=>c.Id==id);
   Check(row.StudentId==null&&row.PersonName==person.Name&&row.SalaryRole=="Staff","Staff contract has correct payroll identity without a player");
   Check(await db.Students.CountAsync()==count,"Staff registration does not create a player or monthly account");
   var contractedPlayer=new SponsorshipBeneficiary{Name="Contract Player",Identifier="PLAYER-TEST-1",IsPerson=true,StaffCategory="ContractPlayer",StaffBirthDate=new(2000,1,1)};db.SponsorshipBeneficiaries.Add(contractedPlayer);await db.SaveChangesAsync();
   var playerInput=Input();playerInput.StudentId=null;playerInput.PersonType="Staff";playerInput.BeneficiaryId=contractedPlayer.Id;playerInput.Number="PLAYER-C-1";
   var playerId=await new PlayerContractService(db).Save(playerInput,"contract-admin",true);var playerRow=await db.PlayerContracts.Include(c=>c.Beneficiary).SingleAsync(c=>c.Id==playerId);
   Check(playerRow.StudentId==null&&playerRow.SalaryRole=="Player","Contract player is standalone but classified as player for sponsorship payroll");
   Check(await db.Students.CountAsync()==count,"Contract player does not create an academy player");
  }  var primary=PlayerContractService.Payroll(1000,Terms());Check(primary.EmployeePension==50&&primary.Tax==66&&primary.Net==884&&primary.Cost==1050,"Primary payroll separates pension, tax, net and employer cost");
  var secondary=Terms();secondary.EmployerType="Secondary";Check(PlayerContractService.Payroll(1000,secondary).Net==855,"Secondary payroll applies flat tax to taxable pay");
  var zero=Terms();zero.EmployeePensionPercent=0;Check(PlayerContractService.Payroll(250,zero).Tax==0&&PlayerContractService.Payroll(450,zero).Tax==16,"Payroll tax thresholds use taxable income");
  await using(var db=Db()){
   var row=await db.PlayerContracts.SingleAsync(c=>c.Id==contractId);await new PlayerContractService(db).End(contractId,row.Revision,BillingClock.Today,"Agreed termination","contract-admin");
   Check((await new TariffService(db).ResolveAsync(await db.Students.SingleAsync(s=>s.Id==studentId),month)).Amount==0,"Early termination retains current calendar-month waiver");
   Check(await db.PlayerContractAudits.CountAsync(a=>a.PlayerContractId==contractId)==2,"Contract activation and termination are audited");
  }
 }
}
