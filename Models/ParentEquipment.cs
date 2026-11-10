using System.ComponentModel.DataAnnotations;
namespace _2Korriku.Models;

public class EquipmentOrderInput
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(1,int.MaxValue)] public int StudentId { get; set; }
    [Range(1,int.MaxValue)] public int VariantId { get; set; }
    [Range(1,100)] public int Quantity { get; set; } = 1;
    public bool AcceptDebt { get; set; }
    public decimal ExpectedPrice { get; set; }
}
public record EquipmentOption(int Id,int ProductId,string Name,string Size,string Unit,decimal Price,decimal Available,bool HasPhoto);
public record ParentEquipment(string Token,int StudentId,string Name,List<EquipmentOption> Products,List<Sale> Orders);
