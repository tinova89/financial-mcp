namespace FinancialMcp.Domain.ValueObjects;

/// <summary>Monthly aggregation key (reference month/year) used by get_budget_status.</summary>
public readonly record struct MonthYear(int Year, int Month)
{
    public static MonthYear FromDate(DateOnly date) => new(date.Year, date.Month);

    public override string ToString() => $"{Month:D2}/{Year:D4}";
}
