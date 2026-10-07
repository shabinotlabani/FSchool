using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class MonthlyInvoiceView
{
    public MonthlyFeePeriod Period { get; set; } = new();
    public Student Student { get; set; } = new();
    public decimal Amount { get; set; }
    public decimal SeasonalDiscount { get; set; }
    public bool IsSeasonal { get; set; }
    public decimal Exempted { get; set; }
    public decimal Paid { get; set; }
    public bool CanEditFirstMonth => Student.FirstMonthUsesWeeks && Period.FirstMonthWeeks.HasValue && !IsSeasonal && !Period.IsFeeWaived && Paid==0 && Exempted==0
        && Period.Year==TariffService.CurrentMonth.Year && Period.Month==TariffService.CurrentMonth.Month
        && BillingClock.LocalDate(Student.RegistrationDate).Year==Period.Year && BillingClock.LocalDate(Student.RegistrationDate).Month==Period.Month;
    public bool CanExempt => !IsSeasonal && !Period.IsFeeWaived && Paid == 0 && Due > 0 && Exempted == 0;
    public decimal Due => Math.Max(0, Amount - Exempted - Paid);
    public string Number => $"2K-{Period.Year}{Period.Month:00}-{Period.StudentId:00000}";
    public string MonthLabel => new DateTime(Period.Year, Period.Month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("sq-AL"));
    public string Status => SeasonalDiscount > 0 ? "Zbritje sezonale" : IsSeasonal && Due == 0 ? "E paguar · sezonale" : Period.IsFeeWaived ? "E liruar" : Amount == 0 ? "Pa tarifë" : Exempted >= Amount && Exempted > 0 ? "E liruar" : Due == 0 ? "E paguar" : Paid > 0 ? "Pjesërisht e paguar" : "E papaguar";
    public string StatusClass => Due == 0 ? "status-active" : Paid > 0 ? "billing-partial" : "billing-unpaid";
}

public class BillingIndexModel
{
    public List<FamilyInvoiceGroup> Families { get; set; } = [];
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Search { get; set; }
    public bool UnpaidOnly { get; set; }
    public List<MonthlyInvoiceView> Invoices { get; set; } = [];
}

public class PlayerBillingModel
{
    public int? CurrentFamilyId { get; set; }
    public List<PersonalTraining> PersonalTrainings { get; set; } = [];
    public decimal PersonalDue => PersonalTrainings.SelectMany(x=>x.Charges).Sum(x=>x.Due);
    public List<FirstMonthFeeChange> FirstMonthChanges { get; set; } = [];
    public string CurrentFeeName { get; set; } = "";
    public List<StudentFeeAssignment> FeeAssignments { get; set; } = [];
    public List<Sale> Sales { get; set; } = [];
    public decimal SeasonalReserve => BillingService.ReservedSeasonalFunds(Payments, Invoices);
    public decimal AvailableBalance => PersonalDue > 0 ? PersonalDue + Math.Max(0, Balance + SeasonalReserve - PersonalDue) : Balance + SeasonalReserve;
    public decimal MonthlyDue => Invoices.Sum(i => i.Due);
    public decimal MonthlyBalance => History.Where(t => !t.IsCancelled && !t.TransactionType.StartsWith("Sale") && !t.TransactionType.StartsWith("Training")).Sum(t => t.Debit - t.Credit);
    public Student Student { get; set; } = new();
    public List<MonthlyInvoiceView> Invoices { get; set; } = [];
    public List<StudentTransaction> History { get; set; } = [];
    public List<StudentMonthlyExemption> Exemptions { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public decimal Balance => History.Where(t => !t.IsCancelled).Sum(t => t.Debit - t.Credit);
    public bool IsExemptionReversed(int id) => History.Any(t => !t.IsCancelled && t.TransactionType == "ExemptionReversal" && t.ReferenceId == id);
}

public class InvoiceDetailModel
{
    public MonthlyInvoiceView Invoice { get; set; } = new();
    public List<StudentTransaction> History { get; set; } = [];
}

public class RegisterPaymentModel : IValidatableObject
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal Balance { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Required(ErrorMessage = "Shkruani shumën.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Shuma duhet të jetë më e madhe se zero.")]
    public decimal? Amount { get; set; }
    [Required(ErrorMessage = "Zgjidhni datën e pagesës."), DataType(DataType.Date)]
    public DateOnly? PaymentDate { get; set; } = BillingClock.Today;
    [Required]
    public string PaymentMethod { get; set; } = "Cash";
    [StringLength(250)]
    public string? Notes { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty) yield return new ValidationResult("Rihapni formularin e pagesës.", [nameof(RequestId)]);
        if (PaymentDate.HasValue && (PaymentDate.Value == DateOnly.MinValue || PaymentDate.Value > BillingClock.Today))
            yield return new ValidationResult("Data e pagesës nuk mund të jetë në të ardhmen.", [nameof(PaymentDate)]);
        if (Amount.HasValue && decimal.Round(Amount.Value, 2) != Amount.Value)
            yield return new ValidationResult("Shuma lejon deri në dy shifra dhjetore.", [nameof(Amount)]);
        if (!new[] { "Cash", "Bank", "Other" }.Contains(PaymentMethod))
            yield return new ValidationResult("Zgjidhni mënyrën e pagesës.", [nameof(PaymentMethod)]);
    }
}

public class ExemptInvoiceModel
{
    public int PeriodId { get; set; }
    public MonthlyInvoiceView? Invoice { get; set; }
    [Required]
    public string ExemptionType { get; set; } = "Sickness";
    [Required(ErrorMessage = "Shkruani shumën e lirimit.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Shuma duhet të jetë pozitive.")]
    public decimal? Amount { get; set; }
    [Required(ErrorMessage = "Arsyeja është e detyrueshme."), StringLength(200)]
    public string Reason { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class ReverseBillingModel
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public int StudentId { get; set; }
    [Required(ErrorMessage = "Shkruani arsyen e korrigjimit."), StringLength(200)]
    public string Reason { get; set; } = string.Empty;
}
