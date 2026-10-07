using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class StudentMonthlyExemption
{
    public int Id { get; set; }

    [Required]
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    [Required, StringLength(50)]
    public string ExemptionType { get; set; } = "Other";

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
