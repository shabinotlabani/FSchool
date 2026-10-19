using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2Korriku.Models;
public class PendingBankPayment
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public bool IsAutomatic { get; set; }
    public DateOnly? AutomaticMonth { get; set; }
    public int? StudentId { get; set; }
    public int? TargetId { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public int? ResultId { get; set; }
    public Student? Student { get; set; }
    [StringLength(20)] public string Kind { get; set; }="Monthly";
    [Column(TypeName="numeric(18,2)")] public decimal Amount { get; set; }
    [Column(TypeName="numeric(18,2)")] public decimal? ConfirmedAmount { get; set; }
    public string Payload { get; set; }="";
    public string CreatedById { get; set; }="";
    public DateTime CreatedAt { get; set; }=DateTime.UtcNow;
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }
    [StringLength(250)] public string? RejectionReason { get; set; }
}
public class BankReviewModel
{
    public bool AllocateAll { get; set; }
    public string? ExpectedClientState { get; set; }
    public int Id { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? ReceivedAmount { get; set; }
    [Required] public DateOnly? ReceivedOn { get; set; }=_2Korriku.Services.BillingClock.Today;
    [StringLength(250)] public string? Reason { get; set; }
}
