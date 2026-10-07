using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class CreateProductModel : IValidatableObject
{
    [Required(ErrorMessage = "Shkruani emrin e rekuizitës."), StringLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Shkruani kategorinë."), StringLength(50)]
    public string Category { get; set; } = "Pajisje sportive";
    [Range(1, int.MaxValue, ErrorMessage = "Zgjidhni grupin e madhësive.")] public int SizeGroupId { get; set; }
    public List<SizeGroup> SizeGroups { get; set; } = [];
    [Required, RegularExpression("Copë|Palë")] public string UnitName { get; set; } = "Copë";
    [StringLength(50)] public string? Barcode { get; set; }
    [Range(typeof(decimal), "0", "999999.99", ErrorMessage = "Çmimi duhet të jetë zero ose pozitiv.")]
    public decimal SalePrice { get; set; }
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Stoku minimal duhet të jetë zero ose pozitiv.")]
    public decimal MinimumStock { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (decimal.Round(SalePrice, 2) != SalePrice || decimal.Round(MinimumStock, 2) != MinimumStock)
            yield return new ValidationResult("Lejohen deri në dy shifra dhjetore.");
    }
}

public class StockEntryLineModel
{
    [Required(ErrorMessage = "Zgjidhni madhësinë."), Range(1, int.MaxValue)] public int? ProductVariantId { get; set; }
    [Required(ErrorMessage = "Zgjidhni rekuizitën."), Range(1, int.MaxValue)]
    public int? ProductId { get; set; }
    [Required(ErrorMessage = "Shkruani sasinë."), Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Sasia duhet të jetë pozitive.")]
    public decimal? Quantity { get; set; }
    [Required(ErrorMessage = "Shkruani çmimin e blerjes."), Range(typeof(decimal), "0", "999999.99", ErrorMessage = "Çmimi nuk mund të jetë negativ.")]
    public decimal? UnitPrice { get; set; }
}

public class CreateStockEntryModel : IValidatableObject
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Required(ErrorMessage = "Zgjidhni datën."), DataType(DataType.Date)]
    public DateOnly? Date { get; set; } = BillingClock.Today;
    [StringLength(200)] public string? SupplierName { get; set; }
    [StringLength(100)] public string? SupplierInvoiceNo { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    public List<StockEntryLineModel> Lines { get; set; } = [new()];
    public List<Product> Products { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty) yield return new ValidationResult("Rihapni formularin e hyrjes.");
        if (Date.HasValue && (Date.Value == DateOnly.MinValue || Date.Value > BillingClock.Today)) yield return new ValidationResult("Data e hyrjes nuk mund të jetë në të ardhmen.", [nameof(Date)]);
        if (Lines == null || Lines.Count is < 1 or > 50) { yield return new ValidationResult("Hyrja duhet të ketë nga 1 deri në 50 rreshta."); yield break; }
        for (var i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            if ((line.Quantity.HasValue && decimal.Round(line.Quantity.Value, 2) != line.Quantity) || (line.UnitPrice.HasValue && decimal.Round(line.UnitPrice.Value, 2) != line.UnitPrice))
                yield return new ValidationResult($"Rreshti {i + 1}: lejohen deri në dy shifra dhjetore.");
        }
    }
}

public class StockEntryListModel
{
    public string? Search { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public List<StockEntry> Entries { get; set; } = [];
}

public class EditProductModel : IValidatableObject
{
    public int Id { get; set; }
    [Required] public string ExpectedState { get; set; } = "";
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, StringLength(50)] public string Category { get; set; } = "";
    [Required, RegularExpression("Copë|Palë")] public string UnitName { get; set; } = "Copë";
    [StringLength(50)] public string? Barcode { get; set; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal SalePrice { get; set; }
    [Range(typeof(decimal), "0", "1000000")] public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public Product? Product { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (decimal.Round(SalePrice, 2) != SalePrice || decimal.Round(MinimumStock, 2) != MinimumStock)
            yield return new("Lejohen deri në dy shifra dhjetore.");
    }
}
