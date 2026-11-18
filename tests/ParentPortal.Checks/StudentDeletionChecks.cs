using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;

static class StudentDeletionChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options)
    {
        ApplicationDbContext Db(int club=1)=>new(options,new ClubContext{ClubId=club});
        void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS: "+label);}
        int id;Guid revision;
        await using(var db=Db()){
            var player=new Student{FirstName="Mistaken",LastName="Registration",DateOfBirth=new(2015,1,1)};
            db.Students.Add(player);await db.SaveChangesAsync();id=player.Id;revision=player.Revision;
            db.MonthlyFeePeriods.Add(new(){StudentId=id,Year=2026,Month=11});
            db.StudentTransactions.Add(new(){StudentId=id,TransactionType="MonthlyFee",Description="Initial fee",Debit=50});await db.SaveChangesAsync();
        }
        await using(var db=Db(2)){
            try{await new StudentDeletionService(db).Delete(id,revision);throw new Exception("Cross-club deletion allowed");}catch(ValidationException){}
        }
        await using(var db=Db()){
            try{await new StudentDeletionService(db).Delete(id,Guid.NewGuid());throw new Exception("Stale deletion allowed");}catch(ValidationException){}
            Check(await db.Students.AnyAsync(x=>x.Id==id),"Stale revision and other club cannot delete a player");
        }
        await using(var db=Db())await new StudentDeletionService(db).Delete(id,revision);
        await using(var db=Db()){
            Check(!await db.Students.AnyAsync(x=>x.Id==id)&&!await db.StudentTransactions.AnyAsync(x=>x.StudentId==id)&&!await db.MonthlyFeePeriods.AnyAsync(x=>x.StudentId==id),"Mistaken registration and unpaid generated fees are deleted together");
            var player=new Student{FirstName="Paid",LastName="Player",DateOfBirth=new(2015,1,1)};db.Students.Add(player);await db.SaveChangesAsync();id=player.Id;revision=player.Revision;
            db.Payments.Add(new(){StudentId=id,Amount=20});await db.SaveChangesAsync();
        }
        await using(var db=Db()){
            try{await new StudentDeletionService(db).Delete(id,revision);throw new Exception("Paid player deleted");}catch(ValidationException){}
            Check(await db.Students.AnyAsync(x=>x.Id==id)&&await db.Payments.AnyAsync(x=>x.StudentId==id),"Deletion preserves players with payment history");
        }
        var token=new string('C',64);
        await using(var db=Db()){
            db.Students.Add(new(){FirstName="Unbilled",LastName="Player",DateOfBirth=new(2015,1,1),MonthlyFee=40,ParentLinkToken=token,RegistrationDate=BillingClock.UtcDate(TariffService.CurrentMonth)});
            await db.SaveChangesAsync();
        }
        await using(var db=Db()){
            var warning=await new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,false,showDebtWarning:true);
            Check(warning is {NeedsAcknowledgement:true,Debt:>0},"Parent entry creates missing monthly fees and immediately shows unpaid debt");
        }
        await using(var db=Db()){
            var card=await new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,false,_=>true,showDebtWarning:true);
            Check(card is {NeedsAcknowledgement:false}&&card.Movements.Any(x=>x.Debit>0),"Acknowledged parent card contains the unpaid month without an operator refresh");
        }
    }
}
