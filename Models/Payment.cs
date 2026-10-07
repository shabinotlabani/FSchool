using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class Payment
{
    public int? FamilyPaymentId { get; set; }
    public FamilyPayment? FamilyPayment { get; set; }
    public int Id { get; set; }
    public Guid? RequestId { get; set; }
    public DateOnly? SeasonalStartMonth { get; set; }
    public int? SeasonalMonths { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal? SeasonalRate { get; set; }
    [NotMapped] public DateOnly? SeasonalEndMonth => SeasonalStartMonth?.AddMonths(SeasonalMonths!.Value - 1);
    [NotMapped] public string SeasonalLabel => SeasonalStartMonth.HasValue ? $"Sezonale {SeasonalMonths} muaj · {SeasonalStartMonth:MM.yyyy}–{SeasonalEndMonth:MM.yyyy} · {SeasonalMonths / 6} muaj zbritje" : "";
    public int? PersonalChargeId { get; set; }
    public PersonalCharge? PersonalCharge { get; set; }
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    [Required]
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(250)]
    public string? Notes { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
}
