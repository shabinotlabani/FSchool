using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class StockEntry
{
    public int Id { get; set; }
    public Guid? RequestId { get; set; }

    [Required, StringLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [StringLength(200)]
    public string? SupplierName { get; set; }

    [StringLength(100)]
    public string? SupplierInvoiceNo { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalAmount { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StockEntryDetail> Details { get; set; } = new List<StockEntryDetail>();
}
