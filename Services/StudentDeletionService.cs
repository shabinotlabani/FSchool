using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace _2Korriku.Services;

public class StudentDeletionService(ApplicationDbContext db)
{
    public async Task Delete(int id, Guid revision)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        await db.Database.ExecuteSqlRawAsync(TeamService.LockSql);
        var student=await db.Students.SingleOrDefaultAsync(s=>s.Id==id)
            ?? throw new ValidationException("Lojtari nuk u gjet.");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Students\" WHERE \"Id\"={id} FOR UPDATE");
        await db.Entry(student).ReloadAsync();
        if(student.Revision!=revision)throw new ValidationException("Lojtari ndryshoi. Rihapeni formularin.");
        var allowed=new HashSet<Type>{typeof(StudentFeeAssignment),typeof(MonthlyFeePeriod),typeof(StudentTransaction),typeof(StudentTeamChange),typeof(StudentChange),typeof(StudentPhoto),typeof(FirstMonthFeeChange),typeof(PendingBankPayment)};
        // Fail closed for every other relationship, including future modules.
        static string Quote(string name)=>"\""+name.Replace("\"","\"\"")+"\"";
        foreach(var fk in db.Model.FindEntityType(typeof(Student))!.GetReferencingForeignKeys()){
            if(allowed.Contains(fk.DeclaringEntityType.ClrType))continue;
            var entity=fk.DeclaringEntityType;
            var table=StoreObjectIdentifier.Table(entity.GetTableName()!,entity.GetSchema());
            var tableName=(table.Schema==null?"":Quote(table.Schema)+".")+Quote(table.Name);
            var column=Quote(fk.Properties.Single().GetColumnName(table)!);
            // Identifiers come only from EF metadata and are quoted; id is a parameter.
#pragma warning disable EF1002
            if(await db.Database.SqlQueryRaw<bool>($"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE {column}={{0}}) AS \"Value\"",id).SingleAsync())
#pragma warning restore EF1002
                throw new ValidationException("Lojtari ka pagesa ose veprimtari të lidhur. Fshirja përfundimtare lejohet vetëm për regjistrime të gabuara pa veprimtari; përdorni çaktivizimin.");
        }
        if(await db.StudentTransactions.AnyAsync(t=>t.StudentId==id && t.TransactionType!="MonthlyFee" && t.TransactionType!="MonthlyFeeAdjustment") ||
            await db.PendingBankPayments.AnyAsync(p=>p.StudentId==id && (!p.IsAutomatic || p.ReviewedAt!=null || p.PaymentId!=null || p.ResultId!=null)))
            throw new ValidationException("Lojtari ka veprime financiare. Përdorni çaktivizimin për të ruajtur historikun.");
        await db.PendingBankPayments.Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        await db.FirstMonthFeeChanges.Where(x=>x.Period!.StudentId==id).ExecuteDeleteAsync();
        await db.StudentTransactions.Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        await db.MonthlyFeePeriods.Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        await db.StudentFeeAssignments.Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        await db.StudentTeamChanges.Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        await db.StudentChanges.Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        await db.Set<StudentPhoto>().Where(x=>x.StudentId==id).ExecuteDeleteAsync();
        db.Students.Remove(student);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
