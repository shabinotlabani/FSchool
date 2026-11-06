using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace _2Korriku.Models;

public class EventCategory
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [RegularExpression("Sport|Social")] public string Kind { get; set; } = "Sport";
    public bool IsActive { get; set; } = true;
}

public class ClubEvent
{
    public int Id { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public int Version { get; set; } = 1;
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, StringLength(4000)] public string Description { get; set; } = "";
    public int CategoryId { get; set; }
    public EventCategory Category { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public DateTime Deadline { get; set; }
    [Required, StringLength(300)] public string Location { get; set; } = "";
    public int Capacity { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Price { get; set; }
    public bool RequiresConsent { get; set; } = true;
    [StringLength(4000)] public string Terms { get; set; } = "";
    [StringLength(1500)] public string Transport { get; set; } = "";
    [StringLength(1500)] public string Instructions { get; set; } = "";
    [StringLength(150)] public string Organizer { get; set; } = "";
    [StringLength(200)] public string Contact { get; set; } = "";
    // Refund releases the fee on cancellation; Retain preserves an authorized fee.
    [RegularExpression("Refund|Retain")] public string CancellationPolicy { get; set; } = "Refund";
    [StringLength(20)] public string Status { get; set; } = "Draft";
    public byte[]? Photo { get; set; }
    [StringLength(30)] public string? PhotoType { get; set; }
    [StringLength(450)] public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<EventInvitation> Invitations { get; set; } = [];
    public string StatusLabel => Status switch { "Published"=>"Publikuar", "Cancelled"=>"Anuluar", "Completed"=>"Përfunduar", _=>"Draft" };
}

public class EventInvitation
{
    public int Id { get; set; }
    public int ClubEventId { get; set; }
    public ClubEvent Event { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [StringLength(20)] public string Status { get; set; } = "Pending";
    public int AcceptedVersion { get; set; }
    public bool? Attended { get; set; }
    public bool WithdrawalRequested { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Waived { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal Paid { get; set; }
    public decimal Due => Math.Max(0, Amount-Waived-Paid);
    public decimal Refundable => Math.Max(0, Paid-(Amount-Waived));
    public string StatusLabel => Status switch { "Accepted"=>"Konfirmuar", "Declined"=>"Refuzuar", "Cancelled"=>"Anuluar", _=>"Në pritje" };
    public List<EventConsent> Consents { get; set; } = [];
}

public class EventConsent
{
    public int Id { get; set; }
    public int EventInvitationId { get; set; }
    public EventInvitation Invitation { get; set; } = null!;
    public int Version { get; set; }
    [StringLength(30)] public string Decision { get; set; } = "";
    [StringLength(150)] public string Guardian { get; set; } = "";
    [StringLength(64)] public string TokenHash { get; set; } = "";
    public bool TermsAccepted { get; set; }
    public string Snapshot { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EventAudit
{
    public int Id { get; set; }
    public int ClubEventId { get; set; }
    public ClubEvent Event { get; set; } = null!;
    [StringLength(450)] public string Actor { get; set; } = "";
    [StringLength(200)] public string Action { get; set; } = "";
    public string Snapshot { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EventExpenseLink
{
    public int Id { get; set; }
    public int ClubEventId { get; set; }
    public ClubEvent Event { get; set; } = null!;
    public int ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;
}

public class EventSponsorLink
{
    public int Id { get; set; }
    public int ClubEventId { get; set; }
    public ClubEvent Event { get; set; } = null!;
    public int SponsorshipReceiptId { get; set; }
    public SponsorshipReceipt Receipt { get; set; } = null!;
}

public class EventSettlement
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int EventInvitationId { get; set; }
    public EventInvitation Invitation { get; set; } = null!;
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [StringLength(10)] public string Kind { get; set; } = "Refund";
    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }
    [StringLength(450)] public string Actor { get; set; } = "";
    [StringLength(200)] public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EventEdit : IValidatableObject
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(150)] public string Name { get; set; } = "";
    [Required,StringLength(4000)] public string Description { get; set; } = "";
    [Range(1,int.MaxValue)] public int CategoryId { get; set; }
    public DateTime StartsAt { get; set; } = _2Korriku.Services.BillingClock.Today.ToDateTime(TimeOnly.MinValue).AddDays(7).AddHours(9);
    public DateTime EndsAt { get; set; } = _2Korriku.Services.BillingClock.Today.ToDateTime(TimeOnly.MinValue).AddDays(7).AddHours(17);
    public DateTime Deadline { get; set; } = _2Korriku.Services.BillingClock.Today.ToDateTime(TimeOnly.MinValue).AddDays(5).AddHours(18);
    [Required,StringLength(300)] public string Location { get; set; } = "";
    [Range(1,10000)] public int Capacity { get; set; } = 30;
    public bool IsPaid { get; set; }
    [Range(typeof(decimal),"0","999999.99")] public decimal Price { get; set; }
    public bool RequiresConsent { get; set; } = true;
    [StringLength(4000)] public string? Terms { get; set; }
    [StringLength(1500)] public string? Transport { get; set; }
    [StringLength(1500)] public string? Instructions { get; set; }
    [StringLength(150)] public string? Organizer { get; set; }
    [StringLength(200)] public string? Contact { get; set; }
    [RegularExpression("Refund|Retain")] public string CancellationPolicy { get; set; } = "Refund";
    public List<int> TeamIds { get; set; } = [];
    public List<int> StudentIds { get; set; } = [];
    [Range(0,100)] public int? MinAge { get; set; }
    [Range(0,100)] public int? MaxAge { get; set; }
    public bool AllPlayers { get; set; }
    public bool RemovePhoto { get; set; }
    [ValidateNever] public List<EventCategory> Categories { get; set; } = [];
    [ValidateNever] public List<TrainingTeam> Teams { get; set; } = [];
    [ValidateNever] public List<Student> Students { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(StartsAt.Year<2000||EndsAt<=StartsAt||Deadline>StartsAt||Deadline.Year<2000)yield return new("Kontrolloni datat dhe afatin e konfirmimit.");
        if(IsPaid&&Price<=0||decimal.Round(Price,2)!=Price)yield return new("Shkruani çmim të vlefshëm me deri në dy shifra dhjetore.");
        if(RequiresConsent&&string.IsNullOrWhiteSpace(Terms))yield return new("Shkruani kushtet e autorizimit.");
        if(MinAge>MaxAge)yield return new("Grupmosha nuk është e vlefshme.");
        if(Id==0&&!AllPlayers&&TeamIds.Count==0&&StudentIds.Count==0&&!MinAge.HasValue&&!MaxAge.HasValue)yield return new("Zgjidhni lojtarët, ekipet ose grupmoshën e ftuar.");
    }
}

public class EventDetails
{
    public ClubEvent Event { get; set; } = null!;
    public List<EventExpenseLink> Expenses { get; set; } = [];
    public List<EventSponsorLink> Sponsors { get; set; } = [];
    public List<EventSettlement> Settlements { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<EventAudit> Audits { get; set; } = [];
    public List<Expense> AvailableExpenses { get; set; } = [];
    public List<SponsorshipReceipt> AvailableSponsors { get; set; } = [];
    public List<ExpenseCategory> ExpenseCategories { get; set; } = [];
    public decimal Revenue => Event.Invitations.Sum(x=>x.Amount-x.Waived);
    public decimal Collected => Event.Invitations.Sum(x=>x.Paid);
    public decimal Debt => Event.Invitations.Sum(x=>x.Due);
    public decimal Sponsorship => Sponsors.Where(x=>x.Receipt.CancelledAt==null).Sum(x=>x.Receipt.Amount);
    public decimal Costs => Expenses.Where(x=>x.Expense.CancelledAt==null).Sum(x=>x.Expense.Amount);
    public decimal Result => Collected+Sponsorship-Costs;
}

public record ParentEvents(string Token,int StudentId,string Name,List<EventInvitation> Invitations);

public class EventBankInput : IValidatableObject
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Range(1,int.MaxValue)] public int InvitationId { get; set; }
    [Range(typeof(decimal),"0.01","999999.99")] public decimal Amount { get; set; }
    public bool Refund { get; set; }
    [StringLength(200)] public string Reason { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(RequestId==Guid.Empty||decimal.Round(Amount,2)!=Amount)yield return new("Kontrolloni shumën dhe formularin.");
        if(Refund&&string.IsNullOrWhiteSpace(Reason))yield return new("Shkruani arsyen e rimbursimit.");
    }
}
