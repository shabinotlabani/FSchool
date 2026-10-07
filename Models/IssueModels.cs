using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class IssueLineModel
{
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Çmimi duhet të jetë nga 0 deri në 999999999 €.")] public decimal? UnitPrice { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Zgjidhni madhësinë.")] public int ProductVariantId { get; set; }
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Quantity { get; set; } = 1;
}

public class CreateIssueModel : IValidatableObject
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(1, int.MaxValue, ErrorMessage = "Zgjidhni lojtarin.")] public int StudentId { get; set; }
    public bool WaivePayment { get; set; }
    [StringLength(200)] public string? WaiverReason { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal AmountPaid { get; set; }
    [Required, RegularExpression("Cash|Bank|Other")] public string PaymentMethod { get; set; } = "Cash";
    public List<IssueLineModel> Lines { get; set; } = [new()];
    public List<Student> Students { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty) yield return new("Hapni një formular të ri.");
        if (Lines == null || Lines.Count is < 1 or > 6) yield return new("Lejohen 1–6 rekuizita për fletëdalje.");
        if (decimal.Round(AmountPaid, 2) != AmountPaid) yield return new("Pagesa lejon dy shifra dhjetore.");
        if (WaivePayment && (string.IsNullOrWhiteSpace(WaiverReason) || string.IsNullOrWhiteSpace(Notes))) yield return new("Për lirim nga pagesa kërkohen arsyeja dhe shënimi.");
        if (WaivePayment && AmountPaid != 0) yield return new("Dalja e liruar duhet të ketë pagesë zero.");
    }
}

public class IssuePaymentModel : IValidatableObject
{
    public int SaleId { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Amount { get; set; }
    [Required, RegularExpression("Cash|Bank|Other")] public string PaymentMethod { get; set; } = "Cash";
    [StringLength(250)] public string? Notes { get; set; }
    public Sale? Sale { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty || decimal.Round(Amount, 2) != Amount) yield return new("Kontrolloni formularin dhe shumën me dy shifra dhjetore.");
    }
}
