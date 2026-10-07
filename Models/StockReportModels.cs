using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class StockFilter : IValidatableObject
{
    [Range(1, int.MaxValue)] public int? VariantId { get; set; }
    [StringLength(200)] public string? Search { get; set; }
    [StringLength(50)] public string? Category { get; set; }
    [RegularExpression("all|active|inactive")] public string Status { get; set; } = "all";
    [RegularExpression("all|low|empty")] public string Stock { get; set; } = "all";
    [RegularExpression("list|categories|cards")] public string Display { get; set; } = "list";
    public DateOnly From { get; set; } = new(BillingClock.Today.Year,BillingClock.Today.Month,1);
    public DateOnly To { get; set; } = BillingClock.Today;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(From.Year<1900 || To>BillingClock.Today || From>To)
            yield return new("Zgjidhni një periudhë të vlefshme: nga data duhet të jetë para ose e njëjtë me datën përfundimtare, deri sot.");
    }
}
public class StockSummaryRow
{
    public List<VariantStockRow> Sizes { get; set; } = [];
    public decimal UnassignedStock => Closing - Sizes.Sum(s => s.Closing);
    public Product Product { get; set; } = new();
    public decimal Opening { get; set; }
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Closing => Opening + Incoming - Outgoing;
    public decimal UndatedStock { get; set; }
    public bool IsLow => Closing <= Product.MinimumStock || Sizes.Any(s => s.Variant.IsActive && s.Closing <= Product.MinimumStock);
    public string Status => Closing<0 ? "Gjendje negative" : Closing==0 ? "Pa stok" : IsLow ? "Stok i ulët" : "Në stok";
    public string StatusClass => Closing<=0 ? "stock-empty" : IsLow ? "stock-low" : "stock-good";
}
public class StockOverviewModel
{
    public StockFilter Filter { get; set; } = new();
    public List<string> Categories { get; set; } = [];
    public List<StockSummaryRow> Rows { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool HasUndatedStock => Rows.Any(r=>r.UndatedStock!=0);
}
public class StockCardRow
{
    public StockMovement Movement { get; set; } = new();
    public string Document { get; set; } = "";
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? DocumentId { get; set; }
    public string Party { get; set; } = "—";
    public decimal Incoming => Math.Max(0,Movement.Quantity);
    public decimal Outgoing => Math.Max(0,-Movement.Quantity);
    public decimal Balance { get; set; }
}
public class VariantStockRow
{
    public ProductVariant Variant { get; set; } = new();
    public decimal Opening { get; set; }
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Closing => Opening + Incoming - Outgoing;
}
public class StockCardModel
{
    public List<VariantStockRow> Sizes { get; set; } = [];
    public string SelectedSize => Filter.VariantId.HasValue ? Product.Variants.FirstOrDefault(v=>v.Id==Filter.VariantId)?.Size ?? "—" : "Të gjitha";
    public Product Product { get; set; } = new();
    public StockFilter Filter { get; set; } = new();
    public decimal Opening { get; set; }
    public decimal UndatedStock { get; set; }
    public List<StockCardRow> Rows { get; set; } = [];
    public decimal Incoming => Rows.Sum(r=>r.Incoming);
    public decimal Outgoing => Rows.Sum(r=>r.Outgoing);
    public decimal Closing => Opening+Incoming-Outgoing;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
