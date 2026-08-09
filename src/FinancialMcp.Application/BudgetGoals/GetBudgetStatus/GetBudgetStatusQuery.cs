using MediatR;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

/// <summary>
/// Corresponds to the MCP tool `get_budget_status`. Calculates Gasto_Real, Saldo_Meta and
/// % Utilizado by category/month, per CLAUDE.md > Business Rules > Budget
/// goals. Year/Month are required; without them the query would be ambiguous.
/// </summary>
public sealed record GetBudgetStatusQuery(int Year, int Month) : IRequest<IReadOnlyList<BudgetStatusDto>>;

public sealed record BudgetStatusDto(
    string RawCategory,
    decimal BudgetAmount,
    decimal ActualSpent,
    decimal RemainingBudget,
    decimal? UtilizationPercentage,
    IReadOnlyList<SubcategoryBreakdownDto> SubcategoryBreakdown);

/// <summary>Secondary breakdown by subcategory — does not create its own goal (see CLAUDE.md).</summary>
public sealed record SubcategoryBreakdownDto(string? Subcategory, decimal ActualSpent);
