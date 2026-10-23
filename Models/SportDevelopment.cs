using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class DevelopmentMetric
{
    public int Id { get; set; }
    [MaxLength(30)] public string Sport { get; set; } = "Football";
    [MaxLength(20)] public string Section { get; set; } = "Measurements";
    [MaxLength(80)] public string Key { get; set; } = "";
    [MaxLength(150)] public string Label { get; set; } = "";
    public string Type { get; set; } = "Number";
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string Step { get; set; } = "1";
    public string Choices { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
}
public class MetricInput
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    public string Sport { get; set; } = "Football";
    public string Section { get; set; } = "Measurements";
    public string Key { get; set; } = "";
    [Required, StringLength(150)] public string Label { get; set; } = "";
    [Required] public string Type { get; set; } = "Number";
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    [Required, StringLength(20)] public string Step { get; set; } = "1";
    [StringLength(4000)] public string? Choices { get; set; }
    public bool Enabled { get; set; } = true;
    [Range(0,999)] public int SortOrder { get; set; }
}
public record SportCatalog(string Sport, List<DevelopmentSection> Sections)
{
    public DevelopmentSection? Find(string key)=>Sections.SingleOrDefault(x=>x.Key==key);
    public string[] Roles=>Find("Profile")?.Fields.FirstOrDefault(x=>x.Key=="PlayerRole")?.Choices??["Lojtar"];
    public string[] Positions=>Find("Profile")?.Fields.FirstOrDefault(x=>x.Key=="PrimaryPosition")?.Choices?.Where(x=>x!="Pa caktuar").ToArray()??[];
}
public record MetricManagement(string Sport,List<DevelopmentMetric> Fields);
