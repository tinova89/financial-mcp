using FinancialMcp.Domain.Common;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Budget goal for a parent category (never a subcategory — see TransactionCategory.ParentCategoryId),
/// sourced from metas_orcamento.csv. A Monthly goal repeats automatically every month; a OneTime
/// goal applies only to its own PeriodReference, with no automatic repetition.
/// </summary>
public class BudgetGoal : BaseEntity
{
    public Guid RawCategoryId { get; set; }
    public TransactionCategory RawCategory { get; set; } = default!;

    public BudgetPeriodType Period { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public MonthYear PeriodReference => new(Year, Month);

    /// <summary>ISO 4217 currency code, same role as Account.BaseCurrencyCode.</summary>
    public string CurrencyCode { get; set; } = default!;

    public decimal BudgetAmount { get; set; }

    /// <summary>
    /// Picks the goal that applies to (year, month) out of every goal registered for the same
    /// category: an exact OneTime match wins outright; otherwise the most recent Monthly goal
    /// whose own PeriodReference is on or before (year, month) applies (it stays in effect until
    /// a later Monthly row for the same category supersedes it). Null when neither applies.
    /// </summary>
    public static BudgetGoal? ResolveEffective(IEnumerable<BudgetGoal> goalsForCategory, int year, int month)
    {
        var goals = goalsForCategory as ICollection<BudgetGoal> ?? goalsForCategory.ToList();

        var oneTimeMatch = goals.FirstOrDefault(g => g.Period == BudgetPeriodType.OneTime && g.Year == year && g.Month == month);
        if (oneTimeMatch is not null)
        {
            return oneTimeMatch;
        }

        var requested = new DateOnly(year, month, 1);

        return goals
            .Where(g => g.Period == BudgetPeriodType.Monthly && new DateOnly(g.Year, g.Month, 1) <= requested)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .FirstOrDefault();
    }
}
