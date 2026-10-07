namespace _2Korriku.Services;

public static class MonthlyProration
{
    // Calendar days, inclusive of the registration date. Round once, per player.
    public static decimal Calculate(decimal monthlyAmount, DateOnly registrationDate)
    {
        if (monthlyAmount < 0) throw new ArgumentOutOfRangeException(nameof(monthlyAmount));
        var days = DateTime.DaysInMonth(registrationDate.Year, registrationDate.Month);
        return decimal.Round(monthlyAmount * (days - registrationDate.Day + 1) / days, 2, MidpointRounding.AwayFromZero);
    }
}
