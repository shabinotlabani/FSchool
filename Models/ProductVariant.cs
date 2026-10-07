using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;

public class SizeGroup : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(2000)] public string Sizes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public static string[] Parse(string? sizes) => (sizes ?? "").Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var values = Parse(Sizes);
        if (values.Length is < 1 or > 60 || values.Any(v => v.Length > 20))
            yield return new("Shkruani 1–60 madhësi, deri në 20 karaktere secila, të ndara me presje.");
        if (values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length)
            yield return new("Madhësitë nuk duhet të përsëriten brenda grupit.");
    }
}

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, StringLength(20)] public string Size { get; set; } = "";
    public bool IsActive { get; set; } = true;
    [Column(TypeName = "numeric(18,2)")] public decimal CurrentStock { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal PurchasePrice { get; set; }
}

public class ProductSizesModel
{
    public int ProductId { get; set; }
    [StringLength(2000)] public string? NewSizes { get; set; }
    public List<int> ActiveIds { get; set; } = [];
    public Product? Product { get; set; }
    public List<SizeGroup> Groups { get; set; } = [];
}
