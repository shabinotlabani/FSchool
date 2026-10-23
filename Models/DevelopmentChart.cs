using System.Globalization;

namespace _2Korriku.Models;

public record DevelopmentPoint(DateOnly Date, decimal Value);
public record DevelopmentChart(string Title,string Unit,List<DevelopmentPoint> Points,bool Rating=false)
{
    public static DevelopmentChart From(List<DevelopmentRecord> records,string kind,string key,string title,string unit,bool rating=false) =>
        new(title,unit,records.Where(x=>x.Kind==kind)
            .Select(x=>new{Row=x,Value=x.Values.GetValueOrDefault(key)})
            .Where(x=>decimal.TryParse(x.Value,NumberStyles.Number,CultureInfo.InvariantCulture,out _))
            .GroupBy(x=>x.Row.Date).OrderBy(x=>x.Key)
            .Select(g=>new DevelopmentPoint(g.Key,decimal.Parse(g.OrderByDescending(x=>x.Row.Id).First().Value!,CultureInfo.InvariantCulture))).ToList(),rating);
}
