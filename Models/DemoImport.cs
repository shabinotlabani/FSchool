using System.ComponentModel.DataAnnotations;
namespace _2Korriku.Models;
public class DemoImport
{
    [Key,StringLength(100)] public string Id { get; set; }="";
    public DateTime CompletedAt { get; set; }
    public int Students { get; set; }
    public int Teams { get; set; }
}
