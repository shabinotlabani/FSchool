using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class Sale
{
    public int Id { get; set; }
    public Guid? RequestId { get; set; }
    [StringLength(200)] public string? WaiverReason { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
    [StringLength(201)] public string? PlayerName { get; set; }
    [StringLength(200)] public string? ParentName { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    [NotMapped] public decimal Due => Math.Max(0, GrandTotal - AmountPaid);

    [Required, StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int? StudentId { get; set; }
    public Student? Student { get; set; }

    [StringLength(200)]
    public string? CustomerName { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal GrandTotal { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal AmountPaid { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Completed";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}
