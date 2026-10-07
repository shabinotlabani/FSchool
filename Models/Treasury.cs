using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using _2Korriku.Services;

namespace _2Korriku.Models;

public class TreasuryEntry
{
    public const string Opening = "Opening";
    public const string Withdrawal = "Withdrawal";
    public const string OpeningGroup = "Gjendja fillestare";
    public const string WithdrawalGroup = "Tërheqje nga pronari";
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    [StringLength(20)] public string Kind { get; set; } = Opening;
    [StringLength(20)] public string Method { get; set; } = "Cash";
    public DateOnly Date { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
    [StringLength(150)] public string? Owner { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public string CreatedById { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancelledById { get; set; }
    public ApplicationUser? CancelledBy { get; set; }
    [StringLength(200)] public string? CancellationReason { get; set; }
    [NotMapped] public string Label => Kind == Opening ? OpeningGroup : WithdrawalGroup;
    [NotMapped] public string Number => $"{(Kind == Opening ? "GJF" : "TPR")}-{Date.Year}-{Id:000000}";
}

public class TreasuryCreateModel : IValidatableObject
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Required, RegularExpression("Opening|Withdrawal")] public string Kind { get; set; } = TreasuryEntry.Opening;
    [Required, RegularExpression("Cash|Bank")] public string Method { get; set; } = "Cash";
    [Required] public DateOnly? Date { get; set; } = BillingClock.Today;
    [Range(typeof(decimal), "0", "999999999.99")] public decimal Amount { get; set; }
    [StringLength(150)] public string? Owner { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    [ValidateNever] public List<MoneyMethodBalance> Balances { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty) yield return new("Rihapni formularin.");
        if (Date.HasValue && (Date < new DateOnly(1900, 1, 1) || Date > BillingClock.Today)) yield return new("Zgjidhni datë nga viti 1900 deri sot.");
        if (decimal.Round(Amount, 2) != Amount) yield return new("Shuma lejon deri në dy shifra dhjetore.");
        if (Kind == TreasuryEntry.Withdrawal && Amount <= 0) yield return new("Shuma e tërheqjes duhet të jetë më e madhe se zero.");
        if (Kind == TreasuryEntry.Withdrawal && string.IsNullOrWhiteSpace(Owner)) yield return new("Shkruani emrin e pronarit.");
    }
}

public class TreasuryCancelModel
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Reason { get; set; } = "";
    [ValidateNever] public TreasuryEntry? Entry { get; set; }
}

public class TreasuryIndexModel
{
    public MoneyDateFilter Filter { get; set; } = new();
    public List<MoneyMethodBalance> Balances { get; set; } = [];
    public List<TreasuryEntry> Entries { get; set; } = [];
    public List<TreasuryEntry> Openings { get; set; } = [];
}
