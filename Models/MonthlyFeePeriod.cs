using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class MonthlyFeePeriod
{
    public int Id { get; set; }
    public int? FirstMonthWeeks { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal? FirstMonthSuggested { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal? FirstMonthFinal { get; set; }
    [StringLength(200)] public string? FirstMonthReason { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal? FullMonthlyAmount { get; set; }
    public DateOnly? ProratedFrom { get; set; }
    [NotMapped] public string? ProrationDescription => FirstMonthFinal.HasValue
        ? $"Muaji i parë: {FirstMonthWeeks?.ToString() ?? "—"}/4 javë · Propozimi {FirstMonthSuggested:N2} € · Shuma {FirstMonthFinal:N2} €" + (FirstMonthReason==null?" · Rrumbullakim përpjetë.":" · Caktuar nga operatori: "+FirstMonthReason)
        : ProratedFrom is DateOnly start && FullMonthlyAmount is decimal amount
        ? $"Muaji i parë: {start:dd.MM.yyyy}–{DateTime.DaysInMonth(start.Year, start.Month):00}.{start:MM.yyyy} · {amount:N2} € × {DateTime.DaysInMonth(start.Year, start.Month) - start.Day + 1}/{DateTime.DaysInMonth(start.Year, start.Month)} ditë = {MonthlyProration.Calculate(amount, start):N2} €"
        : null;
    [StringLength(100)] public string? FeePlanName { get; set; }
    public bool IsFeeWaived { get; set; }
    [StringLength(200)] public string? FeeReason { get; set; }
    [StringLength(400)] public string? FeeNotes { get; set; }

    [Required]
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public bool Generated { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
