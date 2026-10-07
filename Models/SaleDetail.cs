using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class SaleDetail
{
    public int Id { get; set; }
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    [StringLength(20)] public string? UnitName { get; set; }
    [StringLength(200)] public string? ProductName { get; set; }
    [StringLength(20)] public string? ProductSize { get; set; }

    [Required]
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal? ListPrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
}
