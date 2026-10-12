using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class OtherDebtService(ApplicationDbContext db)
{
    public async Task<List<OtherDebt>> LoadAsync(string? search=null)
    {
        var students=db.Students.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search))foreach(var term in search.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)){
            var value=term.ToLower();
            students=students.Where(s=>(s.FirstName+" "+s.LastName).ToLower().Contains(value)||(s.ParentPhone!=null&&s.ParentPhone.Contains(value)));
        }
        var ids=students.Select(s=>s.Id);
        var sales=await db.Sales.AsNoTracking().Where(s=>s.StudentId.HasValue&&ids.Contains(s.StudentId.Value)&&s.GrandTotal>s.AmountPaid&&s.Status!="Cancelled")
            .Select(s=>new OtherDebt(s.Id,s.StudentId!.Value,s.Student!.FirstName+" "+s.Student.LastName,"Rekuzita",s.InvoiceNumber,s.GrandTotal-s.AmountPaid,"Sales",s.RequestId.HasValue?"Pay":"Details")).ToListAsync();
        var personal=await db.PersonalCharges.AsNoTracking().Where(c=>!c.IsCancelled&&c.Amount>c.Paid+c.Waived&&ids.Contains(c.PersonalTraining!.StudentId))
            .Select(c=>new OtherDebt(c.Id,c.PersonalTraining!.StudentId,c.PersonalTraining.Student!.FirstName+" "+c.PersonalTraining.Student.LastName,"Trajnim personal",c.Description,c.Amount-c.Paid-c.Waived,"PersonalTraining","Pay")).ToListAsync();
        return sales.Concat(personal).OrderBy(x=>x.StudentName).ThenBy(x=>x.Category).ThenBy(x=>x.Id).ToList();
    }
}
