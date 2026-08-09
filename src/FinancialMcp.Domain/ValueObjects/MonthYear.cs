namespace FinancialMcp.Domain.ValueObjects;

/// <summary>Monthly aggregation key ("Mês_Ano") used by get_budget_status and get_balance_projection.</summary>
public readonly record struct MonthYear(int Year, int Month)
{
    public static MonthYear FromDate(DateOnly date) => new(date.Year, date.Month);

    public MonthYear AddMonths(int months)
    {
        var date = new DateOnly(Year, Month, 1).AddMonths(months);
        return new MonthYear(date.Year, date.Month);
    }

    public override string ToString() => $"{Month:D2}/{Year:D4}";
}
