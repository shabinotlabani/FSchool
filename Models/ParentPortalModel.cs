namespace _2Korriku.Models;

public class ParentPortalModel
{
    public int StudentId { get; set; }
    public string? FamilyName { get; set; }
    public decimal StudentDebt { get; set; }
    public List<ParentFamilyMember> Members { get; set; } = [];
    public DateOnly Today { get; set; }
    public bool NeedsAcknowledgement { get; set; }
    public string? ParkingTicketId { get; set; }
    public List<ParentSchedule> TodaySessions { get; set; } = [];
    public decimal Debt { get; set; }
    public decimal Advance { get; set; }
    public string? Name { get; set; }
    public string? TeamName { get; set; }
    public string? CoachName { get; set; }
    public string? AssistantCoachName { get; set; }
    public bool Finance { get; set; }
    public DateOnly Month { get; set; }
    public List<ParentSchedule> Schedule { get; set; } = [];
    public List<ParentAttendance> Attendance { get; set; } = [];
    public List<ParentMovement> Movements { get; set; } = [];
}
public record ParentFamilyMember(int Id,string Name,decimal Debt,decimal Advance);
public class ParentLinkView
{
    public Student Student { get; set; } = null!;
    public PlayerFamily? Family { get; set; }
    public string? Token => Family==null?Student.ParentLinkToken:Family.ParentLinkToken;
    public string Name => Family?.Name??Student.FullName;
    public List<Student> Members { get; set; } = [];
}
public record ParentSchedule(int Day, TimeOnly Start, TimeOnly End);
public record ParentAttendance(DateOnly Date, bool Present);
public record ParentMovement(DateTime Date, decimal Debit, decimal Credit);
