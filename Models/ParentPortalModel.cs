namespace _2Korriku.Models;

public class ParentPortalModel
{
    public decimal Debt { get; set; }
    public decimal Advance { get; set; }
    public string? Name { get; set; }
    public bool Finance { get; set; }
    public DateOnly Month { get; set; }
    public List<ParentSchedule> Schedule { get; set; } = [];
    public List<ParentAttendance> Attendance { get; set; } = [];
    public List<ParentMovement> Movements { get; set; } = [];
}
public record ParentSchedule(int Day, TimeOnly Start, TimeOnly End);
public record ParentAttendance(DateOnly Date, bool Present);
public record ParentMovement(DateTime Date, decimal Debit, decimal Credit);
