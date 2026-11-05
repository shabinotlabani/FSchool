namespace _2Korriku.Services;

public record RegistrationMonth(DateOnly Month, string Label, int Count);

public record RegistrationTrend(DateOnly AsOf, IReadOnlyList<RegistrationMonth> Months)
{
    public int Total => Months.Sum(x => x.Count);
    public int CurrentMonth => Months.LastOrDefault()?.Count ?? 0;
    public static RegistrationTrend Build(IEnumerable<DateTime> registrations, DateOnly today)
    {
        var current = new DateOnly(today.Year, today.Month, 1);
        var first = current.AddMonths(-5);
        var counts = registrations.Select(BillingClock.LocalDate)
            .Where(d => d >= first && d <= today)
            .GroupBy(d => new DateOnly(d.Year, d.Month, 1))
            .ToDictionary(g => g.Key, g => g.Count());
        string[] labels = ["Jan", "Shk", "Mar", "Pri", "Maj", "Qer", "Kor", "Gus", "Sht", "Tet", "Nën", "Dhj"];
        return new(today, Enumerable.Range(0, 6).Select(i => {
            var month = first.AddMonths(i);
            return new RegistrationMonth(month, labels[month.Month - 1], counts.GetValueOrDefault(month));
        }).ToList());
    }
}
