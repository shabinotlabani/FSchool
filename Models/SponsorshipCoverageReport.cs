namespace _2Korriku.Models;

public class SponsorshipSponsorReport
{
    public int? Id { get; set; }
    public List<SponsorshipContract> Contracts { get; set; } = [];
    public SponsorshipContractDetails? Report { get; set; }
}

public class SponsorshipCoverageReport
{
    public int Year { get; set; }
    public List<int> Years { get; set; } = [];
    public List<SponsorshipCoverageLine> Lines { get; set; } = [];
    public decimal Declared => Lines.Sum(x=>x.Declared);
    public decimal Covered => Lines.Sum(x=>x.Covered);
    public decimal Needed => Lines.Sum(x=>x.Needed);
    public decimal Percentage => Declared>0?Covered/Declared*100:0;
}

public class SponsorshipCoverageLine
{
    public string Category { get; set; } = "";
    public decimal Declared { get; set; }
    public decimal Covered { get; set; }
    public decimal Needed => Math.Max(0,Declared-Covered);
    public decimal Percentage => Declared>0?Covered/Declared*100:0;
}
