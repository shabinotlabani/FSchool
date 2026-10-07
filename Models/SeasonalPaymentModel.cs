using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class SeasonalPaymentModel : IValidatableObject
{
    public int StudentId { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public string StartMonth { get; set; } = TariffService.CurrentMonth.ToString("yyyy-MM");
    public int Months { get; set; } = 6;
    public decimal ExpectedRate { get; set; }
    [Required, DataType(DataType.Date)] public DateOnly? PaymentDate { get; set; } = BillingClock.Today;
    [Required] public string PaymentMethod { get; set; } = "Cash";
    [StringLength(250)] public string? Notes { get; set; }
    public string? StudentName { get; set; }
    public SeasonalQuote? Quote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty) yield return new("Rihapni formularin e pagesës.");
        if (Months is not (6 or 12)) yield return new("Zgjidhni sezonin 6 ose 12 muaj.");
        if (!TariffService.TryMonth(StartMonth, out var start) || start < TariffService.CurrentMonth || start > TariffService.CurrentMonth.AddMonths(24))
            yield return new("Sezoni duhet të fillojë nga muaji aktual deri në 24 muaj më vonë.");
        if (!PaymentDate.HasValue || PaymentDate < new DateOnly(2000,1,1) || PaymentDate > BillingClock.Today)
            yield return new("Zgjidhni një datë të vlefshme pagese, jo në të ardhmen.");
        if (PaymentMethod is not ("Cash" or "Bank" or "Other")) yield return new("Zgjidhni mënyrën e pagesës.");
    }
}

public record SeasonalQuote(DateOnly Start, int Months, decimal Rate)
{
    public int FreeMonths => Months / 6;
    public int PaidMonths => Months - FreeMonths;
    public DateOnly End => Start.AddMonths(Months - 1);
    public decimal Gross => Rate * Months;
    public decimal Discount => Rate * FreeMonths;
    public decimal Total => Rate * PaidMonths;
}
