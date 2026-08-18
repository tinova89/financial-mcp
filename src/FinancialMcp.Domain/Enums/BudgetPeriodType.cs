namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Whether a BudgetGoal repeats automatically every month (Monthly) or applies to a single
/// explicit month referenced by PeriodReference, with no automatic repetition (OneTime).
/// </summary>
public enum BudgetPeriodType
{
    Monthly = 1,
    OneTime = 2
}
