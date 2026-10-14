namespace _2Korriku.Services;

public static class FieldTime
{
    public static bool Overlaps(DateOnly firstDate, TimeOnly firstStart, TimeOnly firstEnd,
        DateOnly secondDate, TimeOnly secondStart, TimeOnly secondEnd)
    {
        var a = firstDate.ToDateTime(firstStart);
        var b = firstDate.ToDateTime(firstEnd).AddDays(firstEnd <= firstStart ? 1 : 0);
        var c = secondDate.ToDateTime(secondStart);
        var d = secondDate.ToDateTime(secondEnd).AddDays(secondEnd <= secondStart ? 1 : 0);
        return a < d && c < b;
    }

    public static bool OverlapsWeekly(DateOnly date, TimeOnly start, TimeOnly end, int weekday, TimeOnly sessionStart, TimeOnly sessionEnd)
    {
        for (var offset = -1; offset <= 1; offset++)
        {
            var sessionDate = date.AddDays(offset);
            if (((int)sessionDate.DayOfWeek + 6) % 7 + 1 == weekday && Overlaps(date, start, end, sessionDate, sessionStart, sessionEnd)) return true;
        }
        return false;
    }
}
