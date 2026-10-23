using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using _2Korriku.Services;

namespace _2Korriku.Models;

[Index(nameof(StaffId),nameof(StudentId),nameof(Kind),IsUnique=true)]
public class CareAssignment
{
    public int Id { get; set; }
    public string StaffId { get; set; } = "";
    public ApplicationUser Staff { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [MaxLength(20)] public string Kind { get; set; } = "";
    public bool Active { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
    [MaxLength(450)] public string AssignedBy { get; set; } = "";
}
[Index(nameof(RequestId),IsUnique=true)]
public class CareEntry
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public CareAssignment Assignment { get; set; } = null!;
    public Guid RequestId { get; set; }
    public DateOnly Date { get; set; }
    [MaxLength(150)] public string Subject { get; set; } = "";
    [MaxLength(8000)] public string? PrivateNotes { get; set; }
    [MaxLength(20)] public string? TrainingStatus { get; set; }
    [MaxLength(2000)] public string? CoachInstructions { get; set; }
    public DateOnly? ReviewDate { get; set; }
    public bool ShareWithParent { get; set; }
    [MaxLength(4000)] public string? ParentInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class CareInput
{
    public int StudentId { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; } = BillingClock.Today;
    [Required,MaxLength(150)] public string Subject { get; set; } = "";
    [MaxLength(8000)] public string? PrivateNotes { get; set; }
    [RegularExpression("Ready|Limited|Unavailable")] public string? TrainingStatus { get; set; }
    [MaxLength(2000)] public string? CoachInstructions { get; set; }
    public DateOnly? ReviewDate { get; set; }
    public bool ShareWithParent { get; set; }
    [MaxLength(4000)] public string? ParentInstructions { get; set; }
    [Range(10,250)] public decimal? Weight { get; set; }
    [Range(70,250)] public decimal? Height { get; set; }
}
public record CareRosterRow(Student Player,CareEntry? Latest);
public record CareStaffRow(ApplicationUser User,string Kind,int Players);
public record CareRoutingRow(int StudentId,string Name,string Kind);
public record CarePage(Student Player,string Kind,List<CareEntry> Entries,List<DevelopmentRecord> Measurements);
public record CareNotice(int StudentId,string Status,string? Instructions,DateOnly Date,DateOnly? ReviewDate);
public record ParentCareNotice(string Kind,string Instructions,DateOnly Date);
public static class CareLabels
{
    public static string Status(string? value)=>value switch {"Ready"=>"I gatshëm","Limited"=>"Me kufizime","Unavailable"=>"Nuk stërvitet",_=>"Pa vlerësim"};
}
