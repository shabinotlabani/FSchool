using _2Korriku.Models;
namespace _2Korriku.Services;
public static class DevelopmentCharts
{
    public static List<DevelopmentChart> Build(List<DevelopmentRecord> records)
    {
        var charts=new List<DevelopmentChart>();
        var multipleSports=records.Select(x=>x.Sport).Distinct().Skip(1).Any();
        var fields=records.SelectMany(r=>r.Schema.Fields.Where(f=>f.Min.HasValue&&r.Values.ContainsKey(f.Key)).Select(f=>new{Record=r,Field=f}));
        foreach(var group in fields.GroupBy(x=>new{x.Record.Sport,x.Record.Kind,x.Field.Key,x.Field.Label,x.Field.Min,x.Field.Max,x.Field.Step,x.Field.Slider})){
            var k=group.Key;var rating=k.Slider&&k.Max==5;
            var unit=rating?"/5":k.Label.Contains("(cm)")?"cm":k.Label.Contains("(kg)")?"kg":k.Label.Contains("(m)")?"m":k.Label.Contains("(s)")||k.Label.Contains("(sekonda)")?"s":k.Label.Contains("(min)")?"min":"";
            var label=k.Label.Replace(" / 5","");
            foreach(var suffix in new[]{" (cm)"," (kg)"," (m)"," (s)"," (sekonda)"," (min)"})label=label.Replace(suffix,"");
            var title=(multipleSports?SportDevelopmentService.Label(k.Sport)+" · ":"")+label;
            var chart=DevelopmentChart.From(group.Select(x=>x.Record).ToList(),k.Kind,k.Key,title,unit,rating);
            if(chart.Points.Count>0)charts.Add(chart);
        }
        return charts;
    }
}
