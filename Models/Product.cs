using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(20)] public string UnitName { get; set; } = "Copë";
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Barcode { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string CategoryId { get; set; } = "Other";

    [StringLength(20)]
    public string? Size { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal SalePrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
