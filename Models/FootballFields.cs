using System.ComponentModel.DataAnnotations;
using _2Korriku.Services;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace _2Korriku.Models;

public class FootballField
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Revision { get; set; } = Guid.NewGuid();
}

public class FieldBooking
{
    public Guid? SeriesId { get; set; }
    public int SeriesWeeks { get; set; } = 1;
    [Column(TypeName = "numeric(18,2)")] public decimal Price { get; set; }
    public ICollection<FieldPayment> Payments { get; set; } = new List<FieldPayment>();
    [NotMapped] public decimal Paid => Payments.Where(p => p.CancelledAt == null).Sum(p => p.Amount);
    [NotMapped] public decimal Due => IsCancelled ? 0 : Price - Paid;
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int FootballFieldId { get; set; }
    public FootballField? FootballField { get; set; }
    [Required, StringLength(100)] public string CustomerName { get; set; } = "";
    [StringLength(40)] public string? Phone { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
    [StringLength(200)] public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid Revision { get; set; } = Guid.NewGuid();
    public ICollection<FieldBookingChange> Changes { get; set; } = new List<FieldBookingChange>();
}

public class FieldBookingChange
{
    public int Id { get; set; }
    public int FieldBookingId { get; set; }
    public FieldBooking? FieldBooking { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(1500)] public string Description { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
}

public class FieldEditModel
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required(ErrorMessage = "Shkruani emrin e fushës."), StringLength(100)] public string Name { get; set; } = "";
    [StringLength(200)] public string? Location { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FieldBookingEditModel : IValidatableObject
{
    [Range(1, 52, ErrorMessage = "Zgjidhni nga 1 deri në 52 javë.")] public int Weeks { get; set; } = 1;
    [Range(typeof(decimal), "0", "999999.99")] public decimal Price { get; set; }
    public int Id { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public Guid Revision { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Zgjidhni fushën.")] public int FootballFieldId { get; set; }
    [Required(ErrorMessage = "Shkruani emrin e klientit ose grupit."), StringLength(100)] public string CustomerName { get; set; } = "";
    [StringLength(40)] public string? Phone { get; set; }
    public DateOnly Date { get; set; } = BillingClock.Today;
    [Required(ErrorMessage = "Vendosni orën e fillimit.")] public TimeOnly? StartsAt { get; set; }
    [Required(ErrorMessage = "Vendosni orën e përfundimit.")] public TimeOnly? EndsAt { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    [ValidateNever] public List<FootballField> Fields { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (decimal.Round(Price, 2) != Price) yield return new("Çmimi lejon deri në dy shifra dhjetore.");
        if (Date.Year < 2000 || Date.Year > 2099) yield return new("Zgjidhni një datë nga viti 2000 deri më 2099.", [nameof(Date)]);
        if (Id != 0 && Weeks != 1) yield return new("Ndryshimi vlen vetëm për terminin e zgjedhur.");
        if (Date.Year is >= 2000 and <= 2099 && Weeks is >= 1 and <= 52 && Date.AddDays((Weeks - 1) * 7).Year > 2099)
            yield return new("Data e fundit e rezervimit duhet të jetë deri në vitin 2099.");
        if (StartsAt.HasValue && EndsAt.HasValue && (EndsAt <= StartsAt || StartsAt.Value.Ticks % TimeSpan.TicksPerMinute != 0 || EndsAt.Value.Ticks % TimeSpan.TicksPerMinute != 0))
            yield return new("Përfundimi duhet të jetë pas fillimit, brenda së njëjtës ditë dhe me minuta të plota.");
        if (RequestId == Guid.Empty) yield return new("Hapeni përsëri formularin e rezervimit.");
    }
}

public class FieldBookingCancelModel
{
    [ValidateNever] public FieldBooking? Booking { get; set; }
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(200)] public string Reason { get; set; } = "";
}

public class FieldScheduleItem
{
    public bool CanCancel { get; set; }
    public decimal? Due { get; set; }
    public int FieldId { get; set; }
    public string FieldName { get; set; } = "";
    public DateOnly Date { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string Name { get; set; } = "";
    public int? TeamId { get; set; }
    public int? BookingId { get; set; }
    public bool IsCancelled { get; set; }
}

public class FieldScheduleModel
{
    public DateOnly From { get; set; }
    public DateOnly To => From.AddDays(6);
    public int? FieldId { get; set; }
    public bool IncludeCancelled { get; set; }
    public List<FootballField> Fields { get; set; } = [];
    public List<FieldScheduleItem> Items { get; set; } = [];
    public int UnassignedTeams { get; set; }
}

public class FieldPayment
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int FieldBookingId { get; set; }
    public FieldBooking? FieldBooking { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(20)] public string Method { get; set; } = "Cash";
    [StringLength(500)] public string Description { get; set; } = "";
    public string? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public DateTime? CancelledAt { get; set; }
    [StringLength(200)] public string? CancellationReason { get; set; }
    [NotMapped] public string Number => $"FUS-{BillingClock.LocalDate(CreatedAt).Year}-{Id:000000}";
}

public class FieldPaymentModel : IValidatableObject
{
    public int BookingId { get; set; }
    public Guid Revision { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(typeof(decimal), "0.01", "999999.99")] public decimal Amount { get; set; }
    [Required, RegularExpression("Cash|Bank|Other")] public string Method { get; set; } = "Cash";
    [ValidateNever] public FieldBooking? Booking { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RequestId == Guid.Empty) yield return new("Rihapni formularin e pagesës.");
        if (decimal.Round(Amount, 2) != Amount) yield return new("Shuma lejon deri në dy shifra dhjetore.");
    }
}

public class FieldPaymentCancelModel
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Reason { get; set; } = "";
    [ValidateNever] public FieldPayment? Payment { get; set; }
}
