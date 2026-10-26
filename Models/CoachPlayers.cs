namespace _2Korriku.Models;

public class CoachPlayers
{
    public string? Search { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<CoachPlayerRow> Players { get; set; } = [];
}
public record CoachPlayerRow(int Id, string Name, string Team, int Age, int Present, int Total)
{
    public int? Percent => Total == 0 ? null : (int)Math.Round(100d * Present / Total);
    public decimal? Rating { get; init; }
    public DateOnly? RatingDate { get; init; }
}
