namespace _2Korriku.Models;

public class PaymentDocumentModel
{
    public string? ServiceDescription { get; set; }
    public bool IsReceipt { get; set; }
    public string? SeasonalDescription { get; set; }
    public decimal SeasonalGross { get; set; }
    public decimal SeasonalDiscount { get; set; }
    public string? SaleNumber { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title => IsReceipt ? "Dëftesë arkëtimi" : ServiceDescription != null ? "Fletëpagesë trajnimi" : "Fletëpagesë mujore";
    public DateOnly Date { get; set; }
    public Student Student { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string? Method { get; set; }
    public string? Notes { get; set; }
    public string PreparedBy { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Exempted { get; set; }
    public decimal Paid { get; set; }
    public decimal Due { get; set; }
    public string CopyLabel { get; set; } = string.Empty;
}
