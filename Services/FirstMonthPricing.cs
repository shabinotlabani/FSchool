namespace _2Korriku.Services;

public static class FirstMonthPricing
{
    public static int Weeks(DateOnly date)=>Math.Min(4,(DateTime.DaysInMonth(date.Year,date.Month)-date.Day+7)/7);
    public static decimal Calculate(decimal monthlyAmount,DateOnly date)
    {
        if(monthlyAmount<0)throw new ArgumentOutOfRangeException(nameof(monthlyAmount));
        return Math.Min(monthlyAmount,decimal.Ceiling(monthlyAmount*Weeks(date)/4m));
    }
    public static string Explanation(decimal monthlyAmount,DateOnly date)
        =>$"{Weeks(date)}/4 javë · java e nisur llogaritet e plotë · rrumbullakim përpjetë në euro, maksimumi {monthlyAmount:N2} €.";
}
