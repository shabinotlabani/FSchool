namespace _2Korriku.Services;

public static class BillingClock
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");
    public static DateOnly Today => LocalDate(DateTime.UtcNow);
    public static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));
    public static DateTime UtcDate(DateOnly date) => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), Zone);
}
