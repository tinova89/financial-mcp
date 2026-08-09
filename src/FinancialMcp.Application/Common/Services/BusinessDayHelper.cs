namespace FinancialMcp.Application.Common.Services;

/// <summary>
/// Single helper for "next business day", reused by GetBalanceProjection and
/// GetBudgetStatus (see CLAUDE.md > Code Conventions > Dates). Considers only
/// weekends — national/local holidays are out of scope for the MVP.
/// </summary>
public static class BusinessDayHelper
{
    public static DateOnly NextBusinessDay(DateOnly date)
    {
        var result = date;

        while (result.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            result = result.AddDays(1);
        }

        return result;
    }

    public static bool IsBusinessDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
