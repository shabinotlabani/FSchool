namespace _2Korriku.Models;

public class ReasonReportModel
{
    public MoneyDateFilter Filter { get; set; }=new();
    public string? Category { get; set; }
    public string? Action { get; set; }
    public string? Search { get; set; }
    public List<ReasonReportRow> Rows { get; set; }=[];
    public static readonly string[] Categories=["Pagesat","Lirimet mujore","Rekuizitat","Trajnimet personale","Shpenzimet","Arkat","Terminet","Tarifat","Banka"];
    public static readonly string[] Actions=["Anulim","Lirim","Kthim lirimi","Ndryshim","Refuzim","Mbyllje automatike"];
}
public class ReasonReportRow
{
    public DateTime Date { get; set; }
    public string Category { get; set; }="";
    public string Action { get; set; }="Ndryshim";
    public string Subject { get; set; }="";
    public string Document { get; set; }="";
    public string Reason { get; set; }="";
    public decimal? Amount { get; set; }
    public string? ActorId { get; set; }
    public string Actor { get; set; }="";
}
