namespace _2Korriku.Models;

public record PlayerAttendanceMonth(int Year, int Month, int Present, int Total)
{
    private static readonly string[] Names = ["Janar", "Shkurt", "Mars", "Prill", "Maj", "Qershor", "Korrik", "Gusht", "Shtator", "Tetor", "Nëntor", "Dhjetor"];
    public string Label => $"{Names[Month - 1]} {Year}";
    public int Percent => (int)new MonthlyAttendanceSummary(Present, Total).Percent;
}
