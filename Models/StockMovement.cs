using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    [StringLength(20)] public string? ProductSize { get; set; }
    [StringLength(20)] public string? UnitName { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required, StringLength(50)]
    public string MovementType { get; set; } = string.Empty;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal PurchasePrice { get; set; }

    [StringLength(50)]
    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
