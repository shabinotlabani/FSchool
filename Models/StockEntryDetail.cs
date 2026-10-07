using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class StockEntryDetail
{
    public int Id { get; set; }
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    [StringLength(20)] public string? UnitName { get; set; }

    [Required]
    public int StockEntryId { get; set; }
    public StockEntry? StockEntry { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [StringLength(200)]
    public string? ProductName { get; set; }
    [StringLength(50)]
    public string? ProductCode { get; set; }
    [StringLength(20)]
    public string? ProductSize { get; set; }
    [NotMapped]
    public string DisplayName => ProductName ?? Product?.Name ?? "Rekuizitë";
    [NotMapped]
    public string? DisplaySize => ProductName != null ? ProductSize : Product?.Size;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
}
