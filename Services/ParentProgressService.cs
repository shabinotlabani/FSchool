using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;
public class ParentProgressService(ApplicationDbContext db)
{
    // Numeric charts only: private notes, psychological assessments and talent labels are never returned.
    public async Task<List<DevelopmentChart>> Load(int id,DateOnly today)
    {
        var records=await db.DevelopmentRecords.AsNoTracking().Where(x=>x.StudentId==id&&x.Date<=today&&
            (x.Kind=="Measurements"||x.Kind=="Skills"||x.Kind=="Testing"||x.Kind=="Goalkeeping"))
            .Select(x=>new DevelopmentRecord{Id=x.Id,Kind=x.Kind,Date=x.Date,Data=x.Data,Sport=x.Sport,SchemaData=x.SchemaData}).ToListAsync();
        var charts=DevelopmentCharts.Build(records);
        var sport=await new SportDevelopmentService(db).Sport();
        var evaluations=await db.PlayerEvaluations.AsNoTracking().Where(x=>x.StudentId==id&&x.Sport==sport&&x.Date<=today).OrderBy(x=>x.Date).ThenBy(x=>x.Id)
            .Select(x=>new{x.Date,x.Technique,x.Fitness}).ToListAsync();
        var latest= evaluations.GroupBy(x=>x.Date).Select(g=>g.Last()).ToList();
        if(latest.Count>0){charts.Add(new("Teknika","/5",latest.Select(x=>new DevelopmentPoint(x.Date,x.Technique)).ToList(),true));charts.Add(new("Përgatitja fizike","/5",latest.Select(x=>new DevelopmentPoint(x.Date,x.Fitness)).ToList(),true));}
        return charts;
    }
}
