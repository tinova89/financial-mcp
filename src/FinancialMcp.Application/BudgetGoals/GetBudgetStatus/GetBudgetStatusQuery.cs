using MediatR;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

/// <summary>
/// Corresponds to the MCP tool `get_budget_status`. Calculates ActualSpend, RemainingBudget and
/// PercentUsed by category/month, per CLAUDE.md > Business Rules > Budget
/// goals. Year/Month are required; without them the query would be ambiguous. Per category,
/// the applicable goal for that Year/Month is resolved via BudgetGoal.ResolveEffective.
/// </summary>
public sealed record GetBudgetStatusQuery(int Year, int Month) : IRequest<IReadOnlyList<BudgetStatusDto>>;

public sealed record BudgetStatusDto(
    string RawCategory,
    decimal GoalAmount,
    decimal ActualSpend,
    decimal RemainingBudget,
    decimal? PercentUsed,
    IReadOnlyList<SubcategoryBreakdownDto> SubcategoryBreakdown);

/// <summary>Secondary breakdown by subcategory — does not create its own goal (see CLAUDE.md).</summary>
public sealed record SubcategoryBreakdownDto(string? Subcategory, decimal ActualSpend);
