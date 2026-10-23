namespace _2Korriku.Models;

public class CoachActivityReport
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<CoachActivityRow> Rows { get; set; } = [];
}
public record CoachActivityRow(string Name, int Attendance, int Development, int Referrals, int Days, DateTime? Last)
{
    public int Points => Attendance + Development + Referrals;
}
