using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class StudentTransaction
{
    public int Id { get; set; }

    [Required]
    public int StudentId { get; set; }

    public Student? Student { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(50)]
    public string TransactionType { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Credit { get; set; }

    [StringLength(50)]
    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
}
